using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Turns a tap on a cat in the lounge into "this cat was chosen".
    ///
    /// A tap is a press and release close together in place and time, so dragging across
    /// the screen never opens a cat. Taps that land on UI are left to the UI. Only runs a
    /// cheap pointer check per frame; the physics ray is cast once, on release.
    /// </summary>
    public sealed class CatTapInput : MonoBehaviour
    {
        private const float MaxTapTravel = 24f;
        private const float MaxTapSeconds = 0.4f;

        [SerializeField] private Camera viewCamera;
        [SerializeField] private CatRoster roster;
        [SerializeField] private HomeTabController tabs;

        [SerializeField, Min(1f)] private float maxRayDistance = 80f;

        private bool isPressed;
        private Vector2 pressPosition;
        private float pressTime;

        public event Action<CatBrain> OnCatTapped;

        private void Update()
        {
            Pointer pointer = Pointer.current;

            if (pointer == null || tabs.Current != HomeTab.Cats)
            {
                isPressed = false;
                return;
            }

            bool pressedNow = pointer.press.isPressed;

            if (pressedNow && !isPressed)
            {
                isPressed = true;
                pressPosition = pointer.position.ReadValue();
                pressTime = Time.unscaledTime;
                return;
            }

            if (!pressedNow && isPressed)
            {
                isPressed = false;
                Vector2 releasePosition = pointer.position.ReadValue();

                bool isTap = (releasePosition - pressPosition).sqrMagnitude <= MaxTapTravel * MaxTapTravel
                    && Time.unscaledTime - pressTime <= MaxTapSeconds;

                if (isTap && !IsOverUi())
                {
                    TryPick(releasePosition);
                }
            }
        }

        private void TryPick(Vector2 screenPosition)
        {
            Ray ray = viewCamera.ScreenPointToRay(screenPosition);

            if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance))
            {
                return;
            }

            CatBrain brain = roster.FindByCollider(hit.collider);

            if (brain != null)
            {
                OnCatTapped?.Invoke(brain);
            }
        }

        private static bool IsOverUi()
        {
            EventSystem events = EventSystem.current;
            return events != null && events.IsPointerOverGameObject();
        }

#if UNITY_EDITOR
        public void EditorLink(Camera linkedCamera, CatRoster linkedRoster, HomeTabController linkedTabs)
        {
            viewCamera = linkedCamera;
            roster = linkedRoster;
            tabs = linkedTabs;
        }
#endif
    }
}
