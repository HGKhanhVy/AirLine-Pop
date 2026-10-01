using DG.Tweening;
using TMPro;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The "cats aboard" pill on the gameplay HUD. Reads nothing but the passenger event,
    /// and gives a little bounce each time another cat boards.
    /// </summary>
    public sealed class PassengerCounterView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text label;
        [SerializeField] private RectTransform bounceTarget;

        [SerializeField, Min(0f)] private float bounceStrength = 0.28f;
        [SerializeField, Min(0.05f)] private float bounceDuration = 0.35f;

        private int shownAboard = -1;

        private void OnEnable()
        {
            GameplayEvents.OnPassengersChanged += HandlePassengersChanged;
            GameplayEvents.RequestSnapshot();
        }

        private void OnDisable()
        {
            GameplayEvents.OnPassengersChanged -= HandlePassengersChanged;
            bounceTarget.DOKill();
        }

        private void HandlePassengersChanged(int aboard, int total)
        {
            group.alpha = total > 0 ? 1f : 0f;
            label.SetText("{0}/{1}", aboard, total);

            if (shownAboard >= 0 && aboard > shownAboard)
            {
                bounceTarget.DOKill();
                bounceTarget.localScale = Vector3.one;
                bounceTarget.DOPunchScale(Vector3.one * bounceStrength, bounceDuration, 6, 0.6f);
            }

            shownAboard = aboard;
        }

#if UNITY_EDITOR
        public void EditorLink(CanvasGroup linkedGroup, TMP_Text linkedLabel, RectTransform linkedBounce)
        {
            group = linkedGroup;
            label = linkedLabel;
            bounceTarget = linkedBounce;
        }
#endif
    }
}
