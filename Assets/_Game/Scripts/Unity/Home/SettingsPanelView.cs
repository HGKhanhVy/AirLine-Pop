using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The settings popup on Home: music, sound and vibration, saved the moment they change
    /// through the same service the pause screen uses. Opened from the gear, which stays in
    /// the corner on every tab.
    /// </summary>
    public sealed class SettingsPanelView : MonoBehaviour
    {
        [SerializeField] private ModalPanel modal;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button backdropButton;
        [SerializeField] private SettingToggleView musicToggle;
        [SerializeField] private SettingToggleView soundToggle;
        [SerializeField] private SettingToggleView hapticToggle;

        private ISettingsService settings;

        public void Initialize(ISettingsService settingsService)
        {
            settings = settingsService;
        }

        private void OnEnable()
        {
            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(Close);
            backdropButton.onClick.AddListener(Close);
            musicToggle.OnChanged += HandleMusic;
            soundToggle.OnChanged += HandleSound;
            hapticToggle.OnChanged += HandleHaptic;
        }

        private void OnDisable()
        {
            openButton.onClick.RemoveListener(Open);
            closeButton.onClick.RemoveListener(Close);
            backdropButton.onClick.RemoveListener(Close);
            musicToggle.OnChanged -= HandleMusic;
            soundToggle.OnChanged -= HandleSound;
            hapticToggle.OnChanged -= HandleHaptic;
        }

        private void Open()
        {
            if (settings == null || modal.IsShown)
            {
                return;
            }

            musicToggle.SetIsOn(settings.IsMusicOn);
            soundToggle.SetIsOn(settings.IsSoundOn);
            hapticToggle.SetIsOn(settings.IsHapticOn);
            modal.Show();
        }

        private void Close()
        {
            modal.Hide();
        }

        private void HandleMusic(bool isOn)
        {
            settings.SetMusicOn(isOn);
        }

        private void HandleSound(bool isOn)
        {
            settings.SetSoundOn(isOn);
        }

        private void HandleHaptic(bool isOn)
        {
            settings.SetHapticOn(isOn);
        }

#if UNITY_EDITOR
        public void EditorLink(ModalPanel linkedModal, Button linkedOpen, Button linkedClose, Button linkedBackdrop,
            SettingToggleView linkedMusic, SettingToggleView linkedSound, SettingToggleView linkedHaptic)
        {
            modal = linkedModal;
            openButton = linkedOpen;
            closeButton = linkedClose;
            backdropButton = linkedBackdrop;
            musicToggle = linkedMusic;
            soundToggle = linkedSound;
            hapticToggle = linkedHaptic;
        }
#endif
    }
}
