using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using ASTeams.Base.UI;
using UnityEngine.Events;
using ASTeams.Base.Ads;
using ASTeams.Base.Data;

public class UIGameRestartPopup : UIBasePopup
{
    [Header("Lose")]
    [SerializeField] private UIBaseButton loseBtn;

    private void OnEnable()
    {
        loseBtn.onClick.AddListener(OnClickLose);
    }

    private void OnDisable()
    {
        loseBtn?.onClick.RemoveListener(OnClickLose);
    }

    private void OnClickLose()
    {
        UserProfileController.Instance.ResetWinStreak();
        UserProfileController.Instance.UseLife();
        UISceneController.Instance.ChangeScene(SceneName.Gameplay);
        UIPopupController.Instance.HidePopup<UIGameRestartPopup>();
    }
}
