using DG.Tweening;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The star on the square the path finishes on. It pops in with a little spin the
    /// moment the level is won and keeps twinkling until the square is reused.
    /// </summary>
    public sealed class GoalStarView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer star;

        [Tooltip("Star size as a share of the square, once it has settled.")]
        [SerializeField, Range(0.3f, 1.2f)] private float size = 0.72f;
        [SerializeField, Min(0.05f)] private float popSeconds = 0.4f;
        [SerializeField, Range(0f, 90f)] private float spinDegrees = 72f;
        [SerializeField, Range(1f, 1.3f)] private float twinkleScale = 1.08f;
        [SerializeField, Min(0.1f)] private float twinkleSeconds = 0.6f;

        private Sequence reveal;

        public void Reveal()
        {
            Kill();
            Transform mark = star.transform;
            mark.localScale = Vector3.zero;
            mark.localRotation = Quaternion.Euler(0f, 0f, -spinDegrees);
            star.gameObject.SetActive(true);

            reveal = DOTween.Sequence()
                .Append(mark.DOScale(size, popSeconds).SetEase(Ease.OutBack, 2.2f))
                .Join(mark.DOLocalRotate(Vector3.zero, popSeconds).SetEase(Ease.OutCubic))
                .Append(mark.DOScale(size * twinkleScale, twinkleSeconds).SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo));
        }

        public void Hide()
        {
            Kill();
            star.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            Kill();
        }

        private void Kill()
        {
            reveal?.Kill();
            reveal = null;
        }

#if UNITY_EDITOR
        public void EditorLink(SpriteRenderer linkedStar)
        {
            star = linkedStar;
        }
#endif
    }
}
