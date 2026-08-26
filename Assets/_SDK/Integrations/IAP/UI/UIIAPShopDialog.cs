#if IAP
using UnityEngine;
using ASTeams.Base.UI;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using ASTeams.Base.IAP;
using ASTeams.Base.Ads;
using ASTeams.Base.Data;

public class UIIAPShopDialog : UIBaseDialog
{
    public IAPShopUI ShopUI;

    public override void Show()
    {
        UIDialogManager.Instance.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        base.Show();

        AdsController.Instance.HideBannerAds();
    }

    protected override void HideCompleted()
    {
        base.HideCompleted();
        UIDialogManager.Instance.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        if (!UIDialogManager.Instance.IsActiveDialog<UILoseDialog>() 
        && !UIDialogManager.Instance.IsActiveDialog<UIWinDialog>()
        && !UIDialogManager.Instance.IsActiveDialog<UIReviveDialog>())
        {
            ResourceManager.Instance.HideUIResource();
            LevelManager.Instance.PauseTime(false);

            AdsController.Instance.ShowBannerAds();
        }
    }
}
#endif