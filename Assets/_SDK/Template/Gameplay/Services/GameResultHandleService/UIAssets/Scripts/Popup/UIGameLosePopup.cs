using System;
using AirlinePop.Base.Ads;
using AirlinePop.Base.Gameplay;
using AirlinePop.Base.Data;
using AirlinePop.Base.Level;
using AirlinePop.Base.UI;
using AirlinePop.Base.UI.Template;
using TMPro;
using UnityEngine;

namespace AirlinePop.Base
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
