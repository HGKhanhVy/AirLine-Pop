using DG.Tweening;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Shows and hides a full-screen panel: a dimmer fades while the card springs up from
    /// below. The dimmer blocks raycasts, which is also what keeps the board from taking
    /// a drag while the panel is open. Unscaled time, so it runs while the game is paused.
    /// </summary>
    public sealed class ModalPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform card;
        [SerializeField, Min(0.01f)] private float duration = 0.28f;
        [SerializeField] private float travel = 220f;

        private Sequence motion;
        private Vector2 restPosition;

        public bool IsShown { get; private set; }

        private void Awake()
        {
            restPosition = card.anchoredPosition;
            SetHiddenInstant();
        }

        private void OnDestroy()
        {
            motion?.Kill();
        }

        public void Show()
        {
            IsShown = true;
            gameObject.SetActive(true);
            group.blocksRaycasts = true;
            group.interactable = true;
            motion?.Kill();
            card.anchoredPosition = restPosition - new Vector2(0f, travel);
            card.localScale = Vector3.one * 0.92f;
            motion = DOTween.Sequence()
                .Join(group.DOFade(1f, duration * 0.7f))
                .Join(card.DOAnchorPos(restPosition, duration).SetEase(Ease.OutBack))
                .Join(card.DOScale(1f, duration).SetEase(Ease.OutBack))
                .SetUpdate(true);
        }

        public void Hide()
        {
            IsShown = false;
            group.blocksRaycasts = false;
            group.interactable = false;
            motion?.Kill();
            motion = DOTween.Sequence()
                .Join(group.DOFade(0f, duration * 0.6f))
                .Join(card.DOAnchorPos(restPosition - new Vector2(0f, travel * 0.5f), duration * 0.6f).SetEase(Ease.InQuad))
                .SetUpdate(true)
                .OnComplete(Deactivate);
        }

        public void SetHiddenInstant()
        {
            IsShown = false;
            motion?.Kill();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            gameObject.SetActive(false);
        }

        private void Deactivate()
        {
            gameObject.SetActive(false);
        }

#if UNITY_EDITOR
        public void EditorLink(CanvasGroup linkedGroup, RectTransform linkedCard)
        {
            group = linkedGroup;
            card = linkedCard;
        }
#endif
    }
}
