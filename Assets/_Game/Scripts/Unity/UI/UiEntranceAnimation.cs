using DG.Tweening;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Brings a UI element in from an offset each time it is enabled, optionally fading it
    /// up on the way. Delays on neighbouring elements turn a screen opening into a
    /// sequence rather than a single pop.
    ///
    /// Moves position only, so it never fights a component that owns the scale, such as
    /// the button press or pulse.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class UiEntranceAnimation : MonoBehaviour
    {
        [Tooltip("Where it starts, relative to where it rests, in canvas units.")]
        [SerializeField] private Vector2 fromOffset = new Vector2(0f, -200f);

        [SerializeField, Min(0f)] private float delay;
        [SerializeField, Min(0.05f)] private float duration = 0.5f;
        [SerializeField] private AnimationCurve curve = EntranceCurves.CreateDrop();

        [Tooltip("Optional. When set, the element fades up during the first part of the move.")]
        [SerializeField] private CanvasGroup canvasGroup;

        private RectTransform rect;
        private Vector2 restPosition;
        private Sequence motion;

        private void Awake()
        {
            rect = (RectTransform)transform;
            restPosition = rect.anchoredPosition;
        }

        private void OnEnable()
        {
            motion?.Kill();
            rect.anchoredPosition = restPosition + fromOffset;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            // Unscaled: a screen opening should not wait on the game's time scale.
            motion = DOTween.Sequence()
                .AppendInterval(delay)
                .Append(rect.DOAnchorPos(restPosition, duration).SetEase(curve))
                .SetUpdate(true);

            if (canvasGroup != null)
            {
                motion.Insert(delay, canvasGroup.DOFade(1f, duration * 0.6f));
            }
        }

        private void OnDisable()
        {
            motion?.Kill();
            rect.anchoredPosition = restPosition;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
        }
    }
}
