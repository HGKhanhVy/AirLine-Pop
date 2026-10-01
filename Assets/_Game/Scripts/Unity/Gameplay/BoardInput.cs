using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// UnityEngine ships a legacy TouchPhase of its own, so the Input System one has to
// be named explicitly.
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Turns a finger or a mouse into a stream of cell indices.
    ///
    /// A drag is sampled along the segment travelled since the previous frame rather than
    /// only at the current point. On a fast swipe the pointer can cross a whole cell
    /// between frames, and MOV-05 requires that cell to be entered rather than skipped.
    ///
    /// The pointer being lifted is reported but never resets anything: MOV-04 keeps the
    /// path alive so the player can carry on from the head.
    /// </summary>
    public sealed class BoardInput : MonoBehaviour
    {
        [SerializeField] private BoardView boardView;
        [SerializeField] private Camera boardCamera;

        /// <summary>Samples per cell along a drag. Three leaves room for a diagonal cut.</summary>
        [SerializeField, Min(1)] private int samplesPerCell = 3;

        [Tooltip("Movement before a hold counts as a drag. GDD 4 asks for 8 px at a 1080p reference.")]
        [SerializeField, Min(0f)] private float dragThreshold = 8f;

        /// <summary>Used when the camera cannot be measured, such as before the first frame.</summary>
        private const float FallbackPixelsPerCell = 64f;

        /// <summary>Width the drag threshold is quoted against, per GDD 4.</summary>
        private const float ReferenceWidth = 1080f;

        private const int NoTouch = -1;

        private readonly List<RaycastResult> uiHits = new List<RaycastResult>();

        private bool isPointerPreviouslyPressed;
        private bool isPressOnUi;
        private Vector2 lastScreenPosition;
        private int activeTouchId = NoTouch;
        private PointerEventData uiPointer;
        private EventSystem uiPointerOwner;

        /// <summary>Raised for every cell the pointer passes over, in the order it entered them.</summary>
        public event Action<int> OnCellEntered;

        public event Action OnPressReleased;

        /// <summary>
        /// True while <see cref="OnCellEntered"/> reports the cell a press has just landed
        /// on, false for the cells a drag passes over. Lets a listener tell a tap on a
        /// square from a finger sliding across it.
        /// </summary>
        public bool IsReportingPress { get; private set; }

        /// <summary>Set false while a level is resolving so stray input cannot disturb it.</summary>
        public bool AcceptsInput { get; set; } = true;

        /// <summary>True while a press that belongs to the board is held down.</summary>
        public bool IsSteering { get; private set; }

        /// <summary>Where the steering pointer is on the board plane. Valid while <see cref="IsSteering"/>.</summary>
        public Vector3 PointerWorldPosition { get; private set; }

        private void Update()
        {
            if (!TryReadOwningPointer(out bool pressed, out Vector2 screen))
            {
                IsSteering = false;
                return;
            }

            if (pressed && !isPointerPreviouslyPressed)
            {
                isPressOnUi = IsOverUi(screen);
            }

            IsSteering = pressed && AcceptsInput && !isPressOnUi && boardCamera != null;

            if (IsSteering)
            {
                PointerWorldPosition = ScreenToWorld(screen);
            }

            if (!AcceptsInput || isPressOnUi)
            {
                isPointerPreviouslyPressed = pressed;
                lastScreenPosition = screen;
                return;
            }

            if (pressed && !isPointerPreviouslyPressed)
            {
                lastScreenPosition = screen;
                SendCellAt(screen);
            }
            else if (pressed)
            {
                // Below the threshold the finger is being held, not dragged. A hand
                // resting on the seam between two cells would otherwise flicker between
                // them. Nothing is discarded: the movement accumulates until it counts.
                if (Vector2.Distance(lastScreenPosition, screen) >= ScaledDragThreshold())
                {
                    SendCellsAlong(lastScreenPosition, screen);
                    lastScreenPosition = screen;
                }
            }
            else if (isPointerPreviouslyPressed)
            {
                OnPressReleased?.Invoke();
            }

            isPointerPreviouslyPressed = pressed;
        }

        /// <summary>
        /// Whether a press lands on UI rather than the board. The board reads the pointer
        /// directly, so without this a popup open over it still lets a drag draw the path
        /// underneath. Asked once per press, and the whole press follows the answer, so a
        /// finger that starts on a button never turns into a stroke.
        /// </summary>
        private bool IsOverUi(Vector2 screen)
        {
            EventSystem eventSystem = EventSystem.current;

            if (eventSystem == null)
            {
                return false;
            }

            // Each scene brings its own EventSystem; the pointer data is bound to one.
            if (uiPointer == null || uiPointerOwner != eventSystem)
            {
                uiPointer = new PointerEventData(eventSystem);
                uiPointerOwner = eventSystem;
            }

            uiPointer.position = screen;
            uiHits.Clear();
            eventSystem.RaycastAll(uiPointer, uiHits);
            return uiHits.Count > 0;
        }

        private float ScaledDragThreshold()
        {
            return dragThreshold * Mathf.Max(1, Screen.width) / ReferenceWidth;
        }

        /// <summary>
        /// Reports the one pointer that owns the board.
        ///
        /// GDD 4 requires the touch that starts a drag to keep control until it lifts.
        /// Reading whichever pointer is newest lets a second finger, or a thumb resting
        /// on the edge of the screen, take the path over mid stroke and jump it across
        /// the board. A mouse still drives it when the device has no touchscreen, which
        /// is what keeps the Editor usable.
        /// </summary>
        private bool TryReadOwningPointer(out bool pressed, out Vector2 screen)
        {
            Touchscreen touchscreen = Touchscreen.current;

            if (touchscreen != null && (activeTouchId != NoTouch || HasTouchDown(touchscreen)))
            {
                return TryReadOwnedTouch(touchscreen, out pressed, out screen);
            }

            activeTouchId = NoTouch;
            Pointer pointer = Pointer.current;

            if (pointer == null)
            {
                pressed = false;
                screen = lastScreenPosition;
                return false;
            }

            pressed = pointer.press.isPressed;
            screen = pointer.position.ReadValue();
            return true;
        }

        private bool TryReadOwnedTouch(Touchscreen touchscreen, out bool pressed, out Vector2 screen)
        {
            pressed = false;
            screen = lastScreenPosition;

            if (activeTouchId != NoTouch)
            {
                foreach (TouchControl touch in touchscreen.touches)
                {
                    if (touch.touchId.ReadValue() != activeTouchId)
                    {
                        continue;
                    }

                    screen = touch.position.ReadValue();
                    pressed = IsDown(touch.phase.ReadValue());

                    if (!pressed)
                    {
                        activeTouchId = NoTouch;
                    }

                    return true;
                }

                // The finger disappeared without ever reporting a release, which is what
                // losing focus mid drag looks like. Treat it as lifted; EC-05 wants the
                // drag cancelled but the path kept, and releasing does exactly that.
                activeTouchId = NoTouch;
                return true;
            }

            foreach (TouchControl touch in touchscreen.touches)
            {
                if (touch.phase.ReadValue() != TouchPhase.Began)
                {
                    continue;
                }

                activeTouchId = touch.touchId.ReadValue();
                screen = touch.position.ReadValue();
                pressed = true;
                return true;
            }

            return true;
        }

        private static bool HasTouchDown(Touchscreen touchscreen)
        {
            foreach (TouchControl touch in touchscreen.touches)
            {
                if (IsDown(touch.phase.ReadValue()))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsDown(TouchPhase phase)
        {
            return phase == TouchPhase.Began
                || phase == TouchPhase.Moved
                || phase == TouchPhase.Stationary;
        }

        /// <summary>
        /// Walks the segment in steps smaller than a cell so no cell along it is missed,
        /// and reports each newly entered cell once.
        /// </summary>
        private void SendCellsAlong(Vector2 from, Vector2 to)
        {
            float pixelsPerCell = EstimatePixelsPerCell();
            float distance = Vector2.Distance(from, to);
            int steps = Mathf.Max(1, Mathf.CeilToInt(distance / (pixelsPerCell / samplesPerCell)));

            int previous = int.MinValue;

            for (int i = 1; i <= steps; i++)
            {
                Vector2 sample = Vector2.Lerp(from, to, i / (float)steps);

                if (!TryGetCell(sample, out int cell) || cell == previous)
                {
                    continue;
                }

                previous = cell;
                OnCellEntered?.Invoke(cell);
            }
        }

        private void SendCellAt(Vector2 screen)
        {
            if (TryGetCell(screen, out int cell))
            {
                IsReportingPress = true;
                OnCellEntered?.Invoke(cell);
                IsReportingPress = false;
            }
        }

        private bool TryGetCell(Vector2 screen, out int cell)
        {
            cell = -1;

            if (boardCamera == null)
            {
                return false;
            }

            return boardView.TryGetCellAt(ScreenToWorld(screen), out cell);
        }

        /// <summary>Where the pointer's line of sight meets the board surface, which lies on z = 0.</summary>
        private Vector3 ScreenToWorld(Vector2 screen)
        {
            Ray ray = boardCamera.ScreenPointToRay(new Vector3(screen.x, screen.y, 0f));

            if (Mathf.Abs(ray.direction.z) < 1e-5f)
            {
                return ray.origin;
            }

            float along = -ray.origin.z / ray.direction.z;
            return ray.origin + ray.direction * along;
        }

        /// <summary>
        /// How many screen pixels one cell spans, so the sampling step adapts to the board
        /// size and the screen rather than being a guess in pixels. A tiny board on a tall
        /// phone and a ten by ten board on a tablet need very different step sizes.
        ///
        /// Under perspective the far rows are smaller than the near ones; measuring across
        /// the middle of the board is close enough for a sampling step.
        /// </summary>
        private float EstimatePixelsPerCell()
        {
            if (boardCamera == null || Screen.height <= 0)
            {
                return FallbackPixelsPerCell;
            }

            Vector3 centre = boardView.transform.position;
            Vector3 left = boardCamera.WorldToScreenPoint(centre);
            Vector3 right = boardCamera.WorldToScreenPoint(centre + Vector3.right * boardView.CellPitch);
            float pixelsPerCell = right.x - left.x;
            return pixelsPerCell < 1f ? FallbackPixelsPerCell : pixelsPerCell;
        }
    }
}
