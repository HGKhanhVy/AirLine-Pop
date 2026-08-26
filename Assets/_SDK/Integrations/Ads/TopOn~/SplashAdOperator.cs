#if TOPON
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using AnyThinkAds.Api;
using UnityEngine.UI;
using AnyThinkAds.ThirdParty.LitJson;
using System.Security;
using ASTeams.Base.Ads;

public class SplashAdOperator : BaseAdOperator
{
//#if UNITY_ANDROID
//    private const string SPLASH_PLACEMENT_ID = "b5bea7cc9a4497";   
//#elif UNITY_IOS || UNITY_IPHONE
//	private static string SPLASH_PLACEMENT_ID = "b5c22f0e5cc7a0";
//#endif

    private static readonly SplashAdOperator instance = new SplashAdOperator();

    private SplashAdOperator() 
	{
        
	}

	public static SplashAdOperator Instance 
	{
        get 
		{
			return instance;
		}
	}
        
    public override void initializeAd(string id)
    {
        this.adsId = id;

        ATSplashAd.Instance.client.onAdLoadEvent += onSplashAdLoad;
        ATSplashAd.Instance.client.onAdCloseEvent += onSplashAdClose;
        ATSplashAd.Instance.client.onAdShowEvent += onSplashAdShow;
        ATSplashAd.Instance.client.onAdLoadTimeoutEvent += onSplashAdLoadTimeout;
        ATSplashAd.Instance.client.onAdLoadFailureEvent += onSplashAdLoadFailed;
    }

    public override void destroyAd()
    {
        ATSplashAd.Instance.client.onAdLoadEvent -= onSplashAdLoad;
        ATSplashAd.Instance.client.onAdCloseEvent -= onSplashAdClose;
        ATSplashAd.Instance.client.onAdShowEvent -= onSplashAdShow;
        ATSplashAd.Instance.client.onAdLoadTimeoutEvent -= onSplashAdLoadTimeout;
        ATSplashAd.Instance.client.onAdLoadFailureEvent -= onSplashAdLoadFailed;
    }

    public override void showAd() 
    {
        if (isAdReady())
        {
            ATSplashAd.Instance.showSplashAd(this.adsId, new Dictionary<string, object>());
        }  
    }
    public override bool isAdReady()
    {
        bool isAdReady = ATSplashAd.Instance.hasSplashAdReady(this.adsId);
        Debug.Log($"Splash {this.adsId} ready {isAdReady}");
        //AdsManager.Instance.MsgLog($"Splash {this.adsId} ready {isAdReady}");
        return isAdReady;
    }

    public override void loadAd()
    {
        ATSplashAd.Instance.loadSplashAd(this.adsId, new Dictionary<string, object>());
    }

    public void onSplashAdLoad(object sender, ATAdEventArgs arg)
    {
        Debug.Log("Splash::onSplashAdLoad() >>> " + arg.placementId);
    }

    public void onSplashAdClose(object sender, ATAdEventArgs arg) 
    {
        Debug.Log("Splash::onSplashAdClose() >>> " + arg.placementId);
    }

    public void onSplashAdShow(object sender, ATAdEventArgs arg) 
    {
        Debug.Log("Splash::onSplashAdShow() >>> " + arg.placementId);
    }

    public void onSplashAdLoadTimeout(object sender, ATAdEventArgs arg)
    {
         Debug.Log("Splash::onSplashAdLoadTimeout() >>> " + arg.placementId);
    }

    public void onSplashAdLoadFailed(object sender, ATAdErrorEventArgs args)
    {
        Debug.Log("Splash::onSplashAdLoadFailed() >>> " + args.placementId);
    }
}
#endif