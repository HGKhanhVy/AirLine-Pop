using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// A hand that plays a drag over the real board: fades in on the first square,
    /// presses, slides through the given squares, lifts and fades, then does it again.
    ///
    /// It lives in world space beside the board, so it follows every reframing of the
    /// board camera for free. It only knows how to perform; which squares and when is
    /// the caller's business.
    /// </summary>
    public sealed class HandGuideView : MonoBehaviour
    {
        [SerializeField] private Transform hand;
        [SerializeField] private SpriteRenderer handSprite;

        [Tooltip("Where the fingertip sits in the sprite, 0..1 from the bottom-left. " +
                 "That point is the one laid on each square.")]
        [SerializeField] private Vector2 fingertip = new Vector2(0.1f, 0.9f);

        [SerializeField, Min(0.1f)] private float heightInCells = 1.1f;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float fadeDuration = 0.25f;
        [SerializeField, Min(0f)] private float pressDuration = 0.15f;
        [SerializeField, Min(0.05f)] private float stepDuration = 0.3f;
        [SerializeField, Min(0f)] private float holdDuration = 0.3f;
        [SerializeField, Min(0f)] private float restDuration = 0.6f;
        [SerializeField, Range(0.5f, 1f)] private float pressScale = 0.88f;

        private Sequence loop;

        private void Awake()
        {
            handSprite.sortingOrder = BoardSortingOrder.Guide;
            PlaceFingertipOnPivot();
            handSprite.enabled = false;
        }

        public void Play(IReadOnlyList<Vector3> path, float cellPitch)
        {
            Stop();

            if (path == null || path.Count < 2 || handSprite.sprite == null)
            {
                return;
            }

            var points = new Vector3[path.Count];

            for (int i = 0; i < points.Length; i++)
            {
                points[i] = path[i];
            }

            float scale = heightInCells * cellPitch / handSprite.sprite.bounds.size.y;
            Vector3 restScale = Vector3.one * scale;
            Vector3 pressedScale = restScale * pressScale;

            SetAlpha(0f);
            handSprite.enabled = true;

            loop = DOTween.Sequence()
                .AppendCallback(() =>
                {
                    hand.position = points[0];
                    hand.localScale = restScale;
                })
                .Append(handSprite.DOFade(1f, fadeDuration))
                .Append(hand.DOScale(pressedScale, pressDuration));

            // Linear legs: a finger crossing a board does not ease into every square.
            for (int i = 1; i < points.Length; i++)
            {
                loop.Append(hand.DOMove(points[i], stepDuration).SetEase(Ease.Linear));
            }

            loop.AppendInterval(holdDuration)
                .Append(hand.DOScale(restScale, pressDuration))
                .Append(handSprite.DOFade(0f, fadeDuration))
                .AppendInterval(restDuration)
                .SetLoops(-1, LoopType.Restart);
        }

        public void Stop()
        {
            loop?.Kill();
            loop = null;
            handSprite.enabled = false;
        }

        private void PlaceFingertipOnPivot()
        {
            if (handSprite.sprite == null)
            {
                return;
            }

            Vector3 size = handSprite.sprite.bounds.size;
            handSprite.transform.localPosition = new Vector3(
                (0.5f - fingertip.x) * size.x,
                (0.5f - fingertip.y) * size.y,
                0f);
        }

        private void SetAlpha(float alpha)
        {
            Color color = handSprite.color;
            color.a = alpha;
            handSprite.color = color;
        }

        private void OnDestroy()
        {
            loop?.Kill();
        }
    }
}
