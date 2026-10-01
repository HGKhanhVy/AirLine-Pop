using System;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The pause card (GDD 9): Resume first and largest, then Restart and Home, then the
    /// sound and vibration switches. Only reports what was pressed.
    /// </summary>
    public sealed class PausePanelView : MonoBehaviour
    {
        [SerializeField] private ModalPanel modal;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button homeButton;
        [SerializeField] private SettingToggleView musicToggle;
        [SerializeField] private SettingToggleView soundToggle;
        [SerializeField] private SettingToggleView hapticToggle;

        public event Action OnResume;
        public event Action OnRestart;
        public event Action OnHome;
        public event Action<bool> OnMusicChanged;
        public event Action<bool> OnSoundChanged;
        public event Action<bool> OnHapticChanged;

        public bool IsShown => modal.IsShown;

        private void OnEnable()
        {
            resumeButton.onClick.AddListener(HandleResume);
            restartButton.onClick.AddListener(HandleRestart);
            homeButton.onClick.AddListener(HandleHome);
            musicToggle.OnChanged += HandleMusic;
            soundToggle.OnChanged += HandleSound;
            hapticToggle.OnChanged += HandleHaptic;
        }

        private void OnDisable()
        {
            resumeButton.onClick.RemoveListener(HandleResume);
            restartButton.onClick.RemoveListener(HandleRestart);
            homeButton.onClick.RemoveListener(HandleHome);
            musicToggle.OnChanged -= HandleMusic;
            soundToggle.OnChanged -= HandleSound;
            hapticToggle.OnChanged -= HandleHaptic;
        }

        public void Show(bool isMusicOn, bool isSoundOn, bool isHapticOn)
        {
            modal.Show();
            musicToggle.SetIsOn(isMusicOn);
            soundToggle.SetIsOn(isSoundOn);
            hapticToggle.SetIsOn(isHapticOn);
        }

        public void Hide()
        {
            modal.Hide();
        }

        private void HandleResume()
        {
            OnResume?.Invoke();
        }

        private void HandleRestart()
        {
            OnRestart?.Invoke();
        }

        private void HandleHome()
        {
            OnHome?.Invoke();
        }

        private void HandleMusic(bool isOn)
        {
            OnMusicChanged?.Invoke(isOn);
        }

        private void HandleSound(bool isOn)
        {
            OnSoundChanged?.Invoke(isOn);
        }

        private void HandleHaptic(bool isOn)
        {
            OnHapticChanged?.Invoke(isOn);
        }

#if UNITY_EDITOR
        public void EditorLink(ModalPanel linkedModal, Button resume, Button restart, Button home,
            SettingToggleView music, SettingToggleView sound, SettingToggleView haptic)
        {
            modal = linkedModal;
            resumeButton = resume;
            restartButton = restart;
            homeButton = home;
            musicToggle = music;
            soundToggle = sound;
            hapticToggle = haptic;
        }
#endif
    }
}
