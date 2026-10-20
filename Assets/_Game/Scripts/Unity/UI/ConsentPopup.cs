using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace AirlinePop.SingleLine.Unity
{
    /// <summary>
    /// Privacy popup (GDD 12.3), asked once before the first launch continues. The game
    /// shows no ads, so the only answer is agreeing to the policy, which can be read
    /// from here first.
    /// </summary>
    public sealed class ConsentPopup : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform panel;
        [SerializeField] private Button privacyPolicyButton;

        [Tooltip("Agrees to the policy and lets the game open.")]
        [SerializeField] private Button saveButton;

        [SerializeField] private SettingsConfigSO config;
        [SerializeField] private UiSoundConfigSO sounds;

        [SerializeField, Min(0f)] private float animDuration = 0.3f;
        [SerializeField, Range(0f, 1f)] private float hiddenScale = 0.7f;

        private Action onAnswered;
        private Sequence motion;

        /// <summary>Opens the popup and calls back once the player has agreed.</summary>
        public void Ask(Action answered)
        {
            onAnswered = answered;
            Show();
        }

        private void OnEnable()
        {
            privacyPolicyButton.onClick.AddListener(OpenPrivacyPolicy);
            saveButton.onClick.AddListener(Accept);
        }

        private void OnDisable()
        {
            privacyPolicyButton.onClick.RemoveListener(OpenPrivacyPolicy);
            saveButton.onClick.RemoveListener(Accept);
        }

        private void OnDestroy()
        {
            motion?.Kill();
        }

        private void Show()
        {
            motion?.Kill();
            gameObject.SetActive(true);
            SetInteractable(false);
            canvasGroup.alpha = 0f;
            panel.localScale = Vector3.one * hiddenScale;

            if (sounds != null)
            {
                GameAudio.Instance?.PlaySound(sounds.PopupOpen, sounds.PopupOpenVolume);
            }

            motion = DOTween.Sequence()
                .Join(panel.DOScale(Vector3.one, animDuration).SetEase(Ease.OutBack))
                .Join(canvasGroup.DOFade(1f, animDuration).SetEase(Ease.OutQuad))
                .SetUpdate(true)
                .OnComplete(() => SetInteractable(true));
        }

        private void Accept()
        {
            FirstRunProfile.AcceptConsent();
            SetInteractable(false);
            motion?.Kill();
            motion = DOTween.Sequence()
                .Join(panel.DOScale(Vector3.one * hiddenScale, animDuration).SetEase(Ease.InBack))
                .Join(canvasGroup.DOFade(0f, animDuration * 0.7f))
                .SetUpdate(true)
                .OnComplete(Finish);
        }

        private void Finish()
        {
            gameObject.SetActive(false);
            Action answered = onAnswered;
            onAnswered = null;
            answered?.Invoke();
        }

        private void OpenPrivacyPolicy()
        {
            if (config == null || string.IsNullOrEmpty(config.PrivacyPolicyUrl))
            {
                Debug.LogWarning("Privacy policy URL is not set in SettingsConfig.");
                return;
            }

            Application.OpenURL(config.PrivacyPolicyUrl);
        }

        private void SetInteractable(bool isInteractable)
        {
            canvasGroup.interactable = isInteractable;
            canvasGroup.blocksRaycasts = isInteractable;
        }

#if UNITY_EDITOR
        public void EditorLink(CanvasGroup linkedGroup, RectTransform linkedCard, Button linkedPolicy, Button linkedSave,
            SettingsConfigSO linkedConfig, UiSoundConfigSO linkedSounds)
        {
            canvasGroup = linkedGroup;
            panel = linkedCard;
            privacyPolicyButton = linkedPolicy;
            saveButton = linkedSave;
            config = linkedConfig;
            sounds = linkedSounds;
        }
#endif
    }
}
