using UnityEngine;
using ASTeams.Base.UI;
using ASTeams.Template;
using ASTeams.Base.Gameplay; // nếu bạn đang dùng hệ thống UIBasePopup, UIPopupController

namespace ASTeams.Base.UI.Template
{
    public class UISettingButton : MonoBehaviour
    {
        [SerializeField] private UIBaseButton settingBtn;

        private void OnEnable()
        {
            settingBtn.onClick.AddListener(OnClickSetting);
        }

        private void OnDisable()
        {
            settingBtn?.onClick?.RemoveListener(OnClickSetting);
        }

        private void OnClickSetting()
        {
            GameController.Instance.Services.Get<GameStateService>().Pause();
            var popup = UIPopupController.Instance.GetActivePopup<UIPopupSetting>();
            popup.onClosed.RemoveAllListeners();
            popup.onClosed.AddListener(() =>
            {
                GameController.Instance.Services.Get<GameStateService>().Play();
            });
            popup.Show();
        }
    }
}
