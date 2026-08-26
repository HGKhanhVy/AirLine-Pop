using System;
using ASTeams.Base.Ads;
using ASTeams.Base.Gameplay;
using ASTeams.Base.Data;
using ASTeams.Base.Level;
using ASTeams.Base.UI;
using ASTeams.Base.UI.Template;
using TMPro;
using UnityEngine;

namespace ASTeams.Base
{
    public class UIGameLosePopup : UIBasePopup
    {
        [SerializeField] private TextMeshProUGUI levelTxt;
        [SerializeField] private UIBaseButton retryBtn;

        private LosePopupData _data;
        public Action OnRetry;

        private void OnEnable()
        {
            retryBtn?.onClick?.AddListener(OnClickRetry);
        }

        private void OnDisable()
        {
            retryBtn?.onClick?.RemoveListener(OnClickRetry);
        }

        public override void Show()
        {
            base.Show();
            levelTxt.text = $"Level {UserProfileController.Instance.LEVEL}";
        }

        public void SetData(LosePopupData data)
        {
            _data = data;
            levelTxt.text = $"Level {data.level}";
        }

        protected override void HideCompleted()
        {
            base.HideCompleted();
            OnRetry?.Invoke();
            OnRetry = null;
        }

        private void OnClickRetry()
        {
            UIPopupController.Instance.HidePopup<UIGameLosePopup>();
        }
    }
}
