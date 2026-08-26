using UnityEngine;
using ASTeams.Base.UI;
using TMPro;

namespace ASTeams.Base.UI.Template
{
    public class UIPopupSetting : UIBasePopup
    {
        [Header("Sound UI")]
        [SerializeField] private UIBaseButton soundBtn;
        [SerializeField] private GameObject soundOnGo;
        [SerializeField] private GameObject soundOffGo;

        [Header("Music UI")]
        [SerializeField] private UIBaseButton musicBtn;
        [SerializeField] private GameObject musicOnGo;
        [SerializeField] private GameObject musicOffGo;

        [Header("Vibration UI")]
        [SerializeField] private UIBaseButton vibrationBtn;
        [SerializeField] private GameObject vibrationOnGo;
        [SerializeField] private GameObject vibrationOffGo;

        [Header("Version")]
        [SerializeField] private TextMeshProUGUI versionText;

        private void OnEnable()
        {
            soundBtn.onClick.AddListener(OnClickSound);
            musicBtn.onClick.AddListener(OnClickMusic);
            vibrationBtn.onClick.AddListener(OnClickVibration);

            RefreshUI();
        }

        private void OnDisable()
        {
            soundBtn?.onClick?.RemoveListener(OnClickSound);
            musicBtn?.onClick?.RemoveListener(OnClickMusic);
            vibrationBtn?.onClick?.RemoveListener(OnClickVibration);
        }

        public override void Show()
        {
            base.Show();
            RefreshUI();
        }

        private void RefreshUI()
        {
            // Sound
            bool soundOn = !AudioController.Instance.IsMuteSound;
            soundOnGo.SetActive(soundOn);
            soundOffGo.SetActive(!soundOn);

            // Music
            bool musicOn = !AudioController.Instance.IsMuteMusic;
            musicOnGo.SetActive(musicOn);
            musicOffGo.SetActive(!musicOn);

            // Vibration
            bool vibOn = !VibrationController.Instance.IsMuteVibration;
            vibrationOnGo.SetActive(vibOn);
            vibrationOffGo.SetActive(!vibOn);

            versionText.text = $"version {Application.version}";
        }


        private void OnClickSound()
        {
            AudioController.Instance.ToggleSound();
            RefreshUI();
        }

        private void OnClickMusic()
        {
            AudioController.Instance.ToggleMusic();
            RefreshUI();
        }

        private void OnClickVibration()
        {
            VibrationController.Instance.ToggleVibration();
            RefreshUI();
        }
    }
}
