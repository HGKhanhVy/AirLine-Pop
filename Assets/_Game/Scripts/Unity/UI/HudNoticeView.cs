using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// A short notice that slides down under the top bar and leaves by itself, for news that
    /// should not stop play, such as a storm ahead. It never takes a tap.
    /// </summary>
    public sealed class HudNoticeView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform panel;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text label;
        [SerializeField, Min(0.5f)] private float holdSeconds = 2.4f;
        [SerializeField, Min(0.05f)] private float moveSeconds = 0.3f;
        [SerializeField] private float slideDistance = 60f;

        private Vector2 restPosition;
        private Sequence showing;

        private void Awake()
        {
            restPosition = panel.anchoredPosition;
            group.alpha = 0f;
        }

        private void OnDisable()
        {
            showing?.Kill();
            group.alpha = 0f;
        }

        public void Show(Sprite sprite, string text)
        {
            icon.sprite = sprite;
            label.text = text;

            showing?.Kill();
            panel.anchoredPosition = restPosition + Vector2.up * slideDistance;
            group.alpha = 0f;
            showing = DOTween.Sequence().SetUpdate(true)
                .Append(group.DOFade(1f, moveSeconds))
                .Join(panel.DOAnchorPos(restPosition, moveSeconds).SetEase(Ease.OutBack))
                .AppendInterval(holdSeconds)
                .Append(group.DOFade(0f, moveSeconds));
        }

#if UNITY_EDITOR
        public void EditorLink(CanvasGroup linkedGroup, RectTransform linkedPanel, Image linkedIcon, TMP_Text linkedLabel)
        {
            group = linkedGroup;
            panel = linkedPanel;
            icon = linkedIcon;
            label = linkedLabel;
        }
#endif
    }
}
