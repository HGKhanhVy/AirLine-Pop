using System;
using UnityEngine;
using UnityEngine.InputSystem;

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

        /// <summary>Used when the camera cannot be measured, such as before the first frame.</summary>
        private const float FallbackPixelsPerCell = 64f;

        private bool wasPressed;
        private Vector2 lastScreenPosition;

        /// <summary>Raised for every cell the pointer passes over, in the order it entered them.</summary>
        public event Action<int> OnCellEntered;

        public event Action OnPressReleased;

        /// <summary>Set false while a level is resolving so stray input cannot disturb it.</summary>
        public bool AcceptsInput { get; set; } = true;

        private void Awake()
        {
            if (boardCamera == null)
            {
                boardCamera = Camera.main;
            }
        }

        private void Update()
        {
            Pointer pointer = Pointer.current;

            if (pointer == null)
            {
                return;
            }

            bool pressed = pointer.press.isPressed;
            Vector2 screen = pointer.position.ReadValue();

            if (!AcceptsInput)
            {
                wasPressed = pressed;
                lastScreenPosition = screen;
                return;
            }

            if (pressed && !wasPressed)
            {
                lastScreenPosition = screen;
                SendCellAt(screen);
            }
            else if (pressed)
            {
                SendCellsAlong(lastScreenPosition, screen);
                lastScreenPosition = screen;
            }
            else if (wasPressed)
            {
                OnPressReleased?.Invoke();
            }

            wasPressed = pressed;
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
                OnCellEntered?.Invoke(cell);
            }
        }

        private bool TryGetCell(Vector2 screen, out int cell)
        {
            cell = -1;

            if (boardCamera == null)
            {
                return false;
            }

            Vector3 world = boardCamera.ScreenToWorldPoint(
                new Vector3(screen.x, screen.y, -boardCamera.transform.position.z));

            return boardView.TryGetCellAt(world, out cell);
        }

        /// <summary>
        /// How many screen pixels one cell spans, so the sampling step adapts to the board
        /// size and the screen rather than being a guess in pixels. A tiny board on a tall
        /// phone and a ten by ten board on a tablet need very different step sizes.
        /// </summary>
        private float EstimatePixelsPerCell()
        {
            if (boardCamera == null || !boardCamera.orthographic || Screen.height <= 0)
            {
                return FallbackPixelsPerCell;
            }

            float worldPerPixel = boardCamera.orthographicSize * 2f / Screen.height;

            if (worldPerPixel <= 0f)
            {
                return FallbackPixelsPerCell;
            }

            float pixelsPerCell = boardView.CellPitch / worldPerPixel;
            return pixelsPerCell < 1f ? FallbackPixelsPerCell : pixelsPerCell;
        }
    }
}
