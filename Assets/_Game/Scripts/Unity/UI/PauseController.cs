using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Pause flow (GDD 2, 9): freezes the game clock, keeps the path as it is, and returns
    /// to exactly the same state on Resume. Restart and Home go through the same channel
    /// and navigator the rest of the game uses. The app losing focus pauses too.
    /// </summary>
    public sealed class PauseController : MonoBehaviour
    {
        [SerializeField] private Button pauseButton;
        [SerializeField] private PausePanelView panel;

        private ISettingsService settings;
        private ISceneNavigator navigator;
        private bool isLeaving;
        private bool isLocked;

        public void Initialize(ISettingsService settingsService, ISceneNavigator sceneNavigator)
        {
            settings = settingsService;
            navigator = sceneNavigator;
        }

        private void OnEnable()
        {
            pauseButton.onClick.AddListener(Open);
            panel.OnResume += Close;
            panel.OnRestart += HandleRestart;
            panel.OnHome += HandleHome;
            panel.OnMusicChanged += HandleMusic;
            panel.OnSoundChanged += HandleSound;
            panel.OnHapticChanged += HandleHaptic;
        }

        private void OnDisable()
        {
            pauseButton.onClick.RemoveListener(Open);
            panel.OnResume -= Close;
            panel.OnRestart -= HandleRestart;
            panel.OnHome -= HandleHome;
            panel.OnMusicChanged -= HandleMusic;
            panel.OnSoundChanged -= HandleSound;
            panel.OnHapticChanged -= HandleHaptic;
            Time.timeScale = 1f;
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (isPaused)
            {
                Open();
            }
        }

        /// <summary>No pausing once a level is won; the win screen owns that moment.</summary>
        public void SetLocked(bool locked)
        {
            isLocked = locked;
            pauseButton.interactable = !locked;
        }

        public void Open()
        {
            if (isLocked || isLeaving || panel.IsShown || settings == null)
            {
                return;
            }

            Time.timeScale = 0f;
            panel.Show(settings.IsMusicOn, settings.IsSoundOn, settings.IsHapticOn);
        }

        private void Close()
        {
            Time.timeScale = 1f;
            panel.Hide();
        }

        private void HandleRestart()
        {
            Close();
            GameplayEvents.RequestRestart();
        }

        private void HandleHome()
        {
            if (isLeaving)
            {
                return;
            }

            isLeaving = true;
            Time.timeScale = 1f;
            navigator.GoHome();
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
        public void EditorLink(Button linkedPause, PausePanelView linkedPanel)
        {
            pauseButton = linkedPause;
            panel = linkedPanel;
        }
#endif
    }
}
