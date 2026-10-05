using DG.Tweening;
using TMPro;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The "+20" that rises from a regular when it hands over its daily gift: a coin and the
    /// amount on a little pill, over the cat's head, floating up and fading. One pill, reused.
    /// </summary>
    public sealed class CatGiftPopView : MonoBehaviour
    {
        [SerializeField] private RectTransform pop;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text amount;
        [SerializeField] private Camera worldCamera;

        [SerializeField] private float rise = 120f;
        [SerializeField, Min(0.2f)] private float seconds = 1.4f;
        [SerializeField] private float edgeRoom = 16f;

        private Sequence motion;

        private void Awake()
        {
            group.alpha = 0f;
            pop.gameObject.SetActive(false);
        }

        public void Play(Vector3 worldPoint, int coins)
        {
            motion?.Kill();
            Vector2 screen = worldCamera.WorldToScreenPoint(worldPoint);
            var parent = (RectTransform)pop.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, null, out Vector2 local);
            local = InsideScreen(local, parent.rect);
            amount.SetText("+{0}", coins);
            pop.anchoredPosition = local;
            pop.localScale = Vector3.one * 0.5f;
            pop.gameObject.SetActive(true);
            group.alpha = 1f;

            motion = DOTween.Sequence()
                .Join(pop.DOScale(1f, 0.25f).SetEase(Ease.OutBack))
                .Join(pop.DOAnchorPosY(local.y + rise, seconds).SetEase(Ease.OutSine))
                .Insert(seconds * 0.6f, group.DOFade(0f, seconds * 0.4f))
                .OnComplete(() => pop.gameObject.SetActive(false));
        }

        /// <summary>Keeps the pill on screen for its whole rise, however close to an edge the cat stands.</summary>
        private Vector2 InsideScreen(Vector2 start, Rect area)
        {
            Rect box = pop.rect;
            var below = new Vector2(box.xMin, box.yMin);
            var above = new Vector2(box.xMax, box.yMax + rise);
            return ScreenEdgeClamp.Inside(start, below, above, area, edgeRoom);
        }

#if UNITY_EDITOR
        public void EditorLink(RectTransform linkedPop, CanvasGroup linkedGroup, TMP_Text linkedAmount, Camera linkedCamera)
        {
            pop = linkedPop;
            group = linkedGroup;
            amount = linkedAmount;
            worldCamera = linkedCamera;
        }
#endif
    }
}
