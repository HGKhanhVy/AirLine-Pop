using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Now and then one of the airline's planes crosses the sky far off, a cat at its
    /// window, then another follows a while later in a different livery and the other way.
    /// It flies over the map itself, not on it, so it is seen wherever the player has
    /// scrolled to.
    /// </summary>
    public sealed class AirlinerFlyby : MonoBehaviour
    {
        [SerializeField] private RectTransform area;
        [SerializeField] private RectTransform plane;
        [SerializeField] private Image planeImage;
        [SerializeField] private Sprite[] liveries = new Sprite[0];

        [Tooltip("The planes are drawn nose to the left; a flight to the right mirrors them.")]
        [SerializeField] private bool isDrawnFacingLeft = true;

        [SerializeField, Min(1f)] private float crossSeconds = 9f;
        [SerializeField, Min(0f)] private float bob = 14f;
        [SerializeField] private Vector2 pauseSeconds = new Vector2(6f, 12f);
        [SerializeField, Min(0f)] private float firstDelay = 2f;
        [SerializeField] private Vector2 heightRange = new Vector2(0.15f, 0.4f);

        private Sequence trip;
        private int turn;

        public void Play()
        {
            Stop();
            trip = DOTween.Sequence().SetUpdate(true).AppendInterval(firstDelay).AppendCallback(Cross);
        }

        public void Stop()
        {
            trip?.Kill();
            trip = null;
            plane.DOKill();
            plane.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            Stop();
        }

        private void Cross()
        {
            turn++;
            planeImage.sprite = liveries[turn % liveries.Length];
            bool goesRight = turn % 2 == 1;
            float facing = goesRight == isDrawnFacingLeft ? -1f : 1f;
            plane.localScale = new Vector3(facing * Mathf.Abs(plane.localScale.x), plane.localScale.y, 1f);

            Vector2 size = area.rect.size;
            float half = plane.rect.width * Mathf.Abs(plane.localScale.x) * 0.6f;
            float startX = (goesRight ? -1f : 1f) * (size.x / 2f + half);
            float y = size.y * (0.5f - Mathf.Lerp(heightRange.x, heightRange.y, (turn * 37 % 10) / 10f));
            plane.anchoredPosition = new Vector2(startX, y);
            plane.gameObject.SetActive(true);

            trip = DOTween.Sequence().SetUpdate(true)
                .Append(plane.DOAnchorPosX(-startX, crossSeconds).SetEase(Ease.Linear))
                .Join(plane.DOAnchorPosY(y + bob, crossSeconds / 4f).SetEase(Ease.InOutSine).SetLoops(4, LoopType.Yoyo))
                .AppendCallback(() => plane.gameObject.SetActive(false))
                .AppendInterval(Mathf.Lerp(pauseSeconds.x, pauseSeconds.y, (turn * 53 % 10) / 10f))
                .AppendCallback(Cross);
        }

#if UNITY_EDITOR
        public void EditorLink(RectTransform linkedArea, RectTransform linkedPlane, Image linkedImage, Sprite[] linkedLiveries)
        {
            area = linkedArea;
            plane = linkedPlane;
            planeImage = linkedImage;
            liveries = linkedLiveries;
        }
#endif
    }
}
