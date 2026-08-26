#if TOPON
using System.Collections.Generic;
using UnityEngine;
using AnyThinkAds.Api;
using AnyThinkAds.ThirdParty.LitJson;
using UnityEngine.Events;
using ASTeams.Base.Ads;

public class RewardVideoAdOperator : BaseAdOperator
{
    private static readonly RewardVideoAdOperator instance = new RewardVideoAdOperator();
    public static RewardVideoAdOperator Instance => instance;

    private bool hasRewarded = false;
    private bool hasInvoked = false;
    public UnityAction<bool> OnRewared;

    private RewardVideoAdOperator() { }

    public override void initializeAd(string id)
    {
        this.adsId = id;

        ATRewardedVideo.Instance.client.onAdLoadEvent += onAdLoad;
        ATRewardedVideo.Instance.client.onAdLoadFailureEvent += onAdLoadFail;
        ATRewardedVideo.Instance.client.onRewardEvent += onReward;
        ATRewardedVideo.Instance.client.onAdVideoCloseEvent += onAdVideoClosedEvent;
        ATRewardedVideo.Instance.client.onAdVideoEndEvent += onAdVideoEndEvent;
        ATRewardedVideo.Instance.client.onAdVideoStartEvent += onAdVideoStartEvent;
        ATRewardedVideo.Instance.client.onAdVideoFailureEvent += onAdVideoPlayFailure;
        ATRewardedVideo.Instance.client.onAdClickEvent += onAdClick;
    }

    public override void destroyAd()
    {
        ATRewardedVideo.Instance.client.onAdLoadEvent -= onAdLoad;
        ATRewardedVideo.Instance.client.onAdLoadFailureEvent -= onAdLoadFail;
        ATRewardedVideo.Instance.client.onRewardEvent -= onReward;
        ATRewardedVideo.Instance.client.onAdVideoCloseEvent -= onAdVideoClosedEvent;
        ATRewardedVideo.Instance.client.onAdVideoEndEvent -= onAdVideoEndEvent;
        ATRewardedVideo.Instance.client.onAdVideoStartEvent -= onAdVideoStartEvent;
        ATRewardedVideo.Instance.client.onAdVideoFailureEvent -= onAdVideoPlayFailure;
        ATRewardedVideo.Instance.client.onAdClickEvent -= onAdClick;
    }

    public override void loadAd()
    {
        Debug.Log("🔄 Loading rewarded ad...");
        ATRewardedVideo.Instance.loadVideoAd(this.adsId, new Dictionary<string, string>());
    }

    public override bool isAdReady()
    {
        bool ready = ATRewardedVideo.Instance.hasAdReady(this.adsId);
        Debug.Log($"Rewarded {this.adsId} ready: {ready}");
        return ready;
    }

    public override void showAd()
    {
        if (!isAdReady())
        {
            Debug.LogWarning("Rewarded ad not ready!");
            return;
        }

        Debug.Log("🎬 Showing rewarded ad...");
        hasRewarded = false;
        hasInvoked = false;

        ATRewardedVideo.Instance.showAd(this.adsId);
    }

    // ============================= CALLBACKS =============================

    private void SafeInvoke(bool success)
    {
        if (hasInvoked) return;
        hasInvoked = true;
        OnRewared?.Invoke(success);
    }

    public void onReward(object sender, ATAdEventArgs erg)
    {
        Debug.Log("✅ onReward -> user earned reward");
        hasRewarded = true;
        SafeInvoke(true);
    }

    public void onAdVideoClosedEvent(object sender, ATAdEventArgs erg)
    {
        Debug.Log("📴 onAdVideoClosedEvent -> ad closed");
        loadAd();

        if (!hasRewarded)
        {
            Debug.Log("⚠️ User closed before reward");
            SafeInvoke(false);
        }
    }

    public void onAdVideoPlayFailure(object sender, ATAdErrorEventArgs erg)
    {
        Debug.LogError($"❌ onAdVideoPlayFailure: {erg.errorMessage}");
        loadAd();
        SafeInvoke(false);
    }

    public void onAdLoad(object sender, ATAdEventArgs erg)
    {
        Debug.Log($"✅ onAdLoad: {erg.placementId}");
    }

    public void onAdLoadFail(object sender, ATAdErrorEventArgs erg)
    {
        Debug.LogError($"❌ onAdLoadFail: {erg.errorCode} - {erg.errorMessage}");
    }

    public void onAdVideoStartEvent(object sender, ATAdEventArgs erg)
    {
        Debug.Log("▶️ onAdVideoStartEvent");
    }

    public void onAdVideoEndEvent(object sender, ATAdEventArgs erg)
    {
        Debug.Log("⏹ onAdVideoEndEvent");
    }

    public void onAdClick(object sender, ATAdEventArgs erg)
    {
        Debug.Log("🖱 onAdClick");
    }
}
#endif
