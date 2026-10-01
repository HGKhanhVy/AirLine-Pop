using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// One bottom-bar tab: a small icon in its own colours over its name. Idle, the icon
    /// fades back and the name is a soft brown; selected, the icon shows in full, the name
    /// turns sky blue, the tab lifts slightly and a small marker shows under the name.
    /// Nothing framed sits inside the bar's own frame.
    /// </summary>
    public sealed class NavTabButton : MonoBehaviour
    {
        [SerializeField] private HomeTab tab;
        [SerializeField] private Button button;
        [SerializeField] private Image plate;
        [SerializeField] private RectTransform content;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image icon;
        [SerializeField] private Color iconSelected = Color.white;
        [SerializeField] private Color iconIdle = Color.white;
        [SerializeField] private Color labelSelected = Color.white;
        [SerializeField] private Color labelIdle = Color.white;

        [SerializeField] private float selectedLift = 4f;
        [SerializeField] private float selectedScale = 1.04f;
        [SerializeField, Min(0.01f)] private float duration = 0.18f;

        public HomeTab Tab => tab;

        public event Action<HomeTab> OnClicked;

        private void OnEnable()
        {
            button.onClick.AddListener(HandleClick);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(HandleClick);
        }

        private void OnDestroy()
        {
            content.DOKill();
        }

        public void SetSelected(bool isSelected, bool isInstant)
        {
            plate.enabled = isSelected;
            icon.color = isSelected ? iconSelected : iconIdle;
            label.color = isSelected ? labelSelected : labelIdle;
            float lift = isSelected ? selectedLift : 0f;
            float scale = isSelected ? selectedScale : 1f;
            content.DOKill();

            if (isInstant)
            {
                content.anchoredPosition = new Vector2(0f, lift);
                content.localScale = Vector3.one * scale;
                return;
            }

            content.DOAnchorPosY(lift, duration).SetEase(Ease.OutBack).SetUpdate(true);
            content.DOScale(scale, duration).SetEase(Ease.OutBack).SetUpdate(true);
        }

        private void HandleClick()
        {
            OnClicked?.Invoke(tab);
        }

#if UNITY_EDITOR
        public void EditorLink(HomeTab linkedTab, Button linkedButton, Image linkedPlate, RectTransform linkedContent, TMP_Text linkedLabel,
            Image linkedIcon, Color selectedIcon, Color idleIcon, Color selectedLabel, Color idleLabel)
        {
            icon = linkedIcon;
            iconSelected = selectedIcon;
            iconIdle = idleIcon;
            labelSelected = selectedLabel;
            labelIdle = idleLabel;
            tab = linkedTab;
            button = linkedButton;
            plate = linkedPlate;
            content = linkedContent;
            label = linkedLabel;
        }
#endif
    }
}
