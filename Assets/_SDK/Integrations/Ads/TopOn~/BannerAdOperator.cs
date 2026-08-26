#if TOPON
using System.Collections.Generic;
using UnityEngine;
using AnyThinkAds.Api;

using AnyThinkAds.ThirdParty.LitJson;


public class BannerAdOperator : BaseAdOperator
{
//#if UNITY_ANDROID
//    static string mPlacementId_banner_all = "b5baca4f74c3d8";
//    static string showingScenario = "f600e6039e152c";

//#elif UNITY_IOS || UNITY_IPHONE
//	static string mPlacementId_banner_all = "b5bacaccb61c29";
//    //static string mPlacementId_banner_all = "b5bacaccb61c29";
//    static string showingScenario = "";
//#endif

    private int screenWidth;
    private string position;

    private static readonly BannerAdOperator instance = new BannerAdOperator();

    private BannerAdOperator() 
	{
        
	}

	public static BannerAdOperator Instance 
	{
        get 
		{
			return instance;
		}
	}

    // Use this for initialization
    public override void initializeAd(string id)
    {
        this.adsId = id;
        this.screenWidth = Screen.width;

        ATBannerAd.Instance.client.onAdAutoRefreshEvent += onAdAutoRefresh;
        ATBannerAd.Instance.client.onAdAutoRefreshFailureEvent += onAdAutoRefreshFail;
        ATBannerAd.Instance.client.onAdClickEvent += onAdClick;
        ATBannerAd.Instance.client.onAdCloseEvent += onAdClose;
        ATBannerAd.Instance.client.onAdCloseButtonTappedEvent += onAdCloseButtonTapped;
        ATBannerAd.Instance.client.onAdImpressEvent += onAdImpress;
        ATBannerAd.Instance.client.onAdLoadEvent += onAdLoad;
        ATBannerAd.Instance.client.onAdLoadFailureEvent += onAdLoadFail;
        ATBannerAd.Instance.client.onAdSourceAttemptEvent += startLoadingADSource;
        ATBannerAd.Instance.client.onAdSourceFilledEvent += finishLoadingADSource;
        ATBannerAd.Instance.client.onAdSourceLoadFailureEvent += failToLoadADSource;
        ATBannerAd.Instance.client.onAdSourceBiddingAttemptEvent += startBiddingADSource;
        ATBannerAd.Instance.client.onAdSourceBiddingFilledEvent += finishBiddingADSource;
        ATBannerAd.Instance.client.onAdSourceBiddingFailureEvent += failBiddingADSource;

    }

    public void initPosition(string position)
    {
        this.position = position.ToString();
    }

    public override void destroyAd()
    {
        ATBannerAd.Instance.client.onAdAutoRefreshEvent -= onAdAutoRefresh;
        ATBannerAd.Instance.client.onAdAutoRefreshFailureEvent -= onAdAutoRefreshFail;
        ATBannerAd.Instance.client.onAdClickEvent -= onAdClick;
        ATBannerAd.Instance.client.onAdCloseEvent -= onAdClose;
        ATBannerAd.Instance.client.onAdCloseButtonTappedEvent -= onAdCloseButtonTapped;
        ATBannerAd.Instance.client.onAdImpressEvent -= onAdImpress;
        ATBannerAd.Instance.client.onAdLoadEvent -= onAdLoad;
        ATBannerAd.Instance.client.onAdLoadFailureEvent -= onAdLoadFail;
        ATBannerAd.Instance.client.onAdSourceAttemptEvent -= startLoadingADSource;
        ATBannerAd.Instance.client.onAdSourceFilledEvent -= finishLoadingADSource;
        ATBannerAd.Instance.client.onAdSourceLoadFailureEvent -= failToLoadADSource;
        ATBannerAd.Instance.client.onAdSourceBiddingAttemptEvent -= startBiddingADSource;
        ATBannerAd.Instance.client.onAdSourceBiddingFilledEvent -= finishBiddingADSource;
        ATBannerAd.Instance.client.onAdSourceBiddingFailureEvent -= failBiddingADSource;

        ATBannerAd.Instance.cleanBannerAd(this.adsId);
    }

    public override void showAd() 
    {
        string adStatusJsonStr = ATBannerAd.Instance.checkAdStatus(this.adsId);
        JsonData jsonData = JsonMapper.ToObject(adStatusJsonStr);
        bool isAdReady = bool.Parse(jsonData.ContainsKey("isReady") ? jsonData["isReady"].ToString() : "false");
        
        if (isAdReady) {
            //var arpuRect = new ATRect(0,50, this.screenWidth, 300, true);
            //ATBannerAd.Instance.showBannerAd(mPlacementId_banner_all, arpuRect);
            // ATBannerAd.Instance.showBannerAd(mPlacementId_banner_all, ATBannerAdLoadingExtra.kATBannerAdShowingPisitionBottom);
            ATBannerAd.Instance.showBannerAd(this.adsId, this.position);
            //show with scenario
            //        Dictionary<string, string> jsonmap = new Dictionary<string, string>();
            //        jsonmap.Add(AnyThinkAds.Api.ATConst.SCENARIO, showingScenario);
            //        //ATBannerAd.Instance.showBannerAd(mPlacementId_banner_all, arpuRect, jsonmap);
            //        ATBannerAd.Instance.showBannerAd(mPlacementId_banner_all, ATBannerAdLoadingExtra.kATBannerAdShowingPisitionTop, jsonmap);
        }   
    }

    public override bool isAdReady()
    {
        string adStatusJsonStr = ATBannerAd.Instance.checkAdStatus(this.adsId);
        JsonData jsonData = JsonMapper.ToObject(adStatusJsonStr);
        bool isAdReady = bool.Parse(jsonData.ContainsKey("isReady") ? jsonData["isReady"].ToString() : "false");

        Debug.Log($"Banner {this.adsId} ready {isAdReady}");
        //AdsManager.Instance.MsgLog($"Banner {this.adsId} ready {isAdReady}");
        return isAdReady;
    }

    // static BannerCallback bannerCallback ;

    public override void loadAd()
    {

        // if(bannerCallback == null){
        //     bannerCallback = new BannerCallback();
        //     ATBannerAd.Instance.setListener(bannerCallback);
        // }

        Dictionary<string, object> jsonmap = new Dictionary<string, object>();


#if UNITY_ANDROID
            ATSize bannerSize = new ATSize(960, 150, true);
            jsonmap.Add(ATBannerAdLoadingExtra.kATBannerAdLoadingExtraBannerAdSizeStruct, bannerSize);
            jsonmap.Add(ATBannerAdLoadingExtra.kATBannerAdLoadingExtraAdaptiveWidth, bannerSize.width);
            jsonmap.Add(ATBannerAdLoadingExtra.kATBannerAdLoadingExtraAdaptiveOrientation, ATBannerAdLoadingExtra.kATBannerAdLoadingExtraAdaptiveOrientationPortrait);
#elif UNITY_IOS || UNITY_IPHONE
            ATSize bannerSize = new ATSize(320, 50, false);
            jsonmap.Add(ATBannerAdLoadingExtra.kATBannerAdLoadingExtraBannerAdSizeStruct, bannerSize);
            jsonmap.Add(ATBannerAdLoadingExtra.kATBannerAdLoadingExtraAdaptiveWidth, bannerSize.width);
            jsonmap.Add(ATBannerAdLoadingExtra.kATBannerAdLoadingExtraAdaptiveOrientation, ATBannerAdLoadingExtra.kATBannerAdLoadingExtraAdaptiveOrientationPortrait);
        
#endif
        ATBannerAd.Instance.loadBannerAd(this.adsId, jsonmap);
    }

    public void onAdAutoRefresh(object sender, ATAdEventArgs erg)
    {
        Debug.Log("Developer callback onAdAutoRefresh :" + erg.placementId + "->" + JsonMapper.ToJson(erg.callbackInfo.toDictionary()));
    }

    public void onAdAutoRefreshFail(object sender, ATAdErrorEventArgs erg)
    {
        Debug.Log("Developer callback onAdAutoRefreshFail : " + erg.placementId + "--erg.errorCode:" + erg.errorCode + "--msg:" + erg.errorMessage);
    }

    public void onAdClick(object sender, ATAdEventArgs erg)
    {
        Debug.Log("Developer callback onAdClick :" + erg.placementId + "->" + JsonMapper.ToJson(erg.callbackInfo.toDictionary()));
    }

    public void onAdClose(object sender, ATAdEventArgs erg)
    {
        Debug.Log("Developer callback onAdClose :" + erg.placementId);
    }

    public void onAdCloseButtonTapped(object sender, ATAdEventArgs erg)
    {
        Debug.Log("Developer callback onAdCloseButtonTapped :" + erg.placementId + "->" + JsonMapper.ToJson(erg.callbackInfo.toDictionary()));
    }

    public void onAdImpress(object sender, ATAdEventArgs erg)
    {
        Debug.Log("Developer callback onAdImpress :" + erg.placementId + "->" + JsonMapper.ToJson(erg.callbackInfo.toDictionary()));
    }

    public void onAdLoad(object sender, ATAdEventArgs erg)
    {
        // Banner ad is ready to be shown.
        Debug.Log("Developer callback onAdLoad :" + erg.placementId);
    }

    public void onAdLoadFail(object sender, ATAdErrorEventArgs erg)
    {
        // Banner ad failed to load. SDK will automatically try loading a new ad internally.
        Debug.Log("Developer callback onAdLoadFail : : " + erg.placementId + "--erg.errorCode:" + erg.errorCode + "--msg:" + erg.errorMessage);
    }

    public void startLoadingADSource(object sender, ATAdEventArgs erg)
    {
        Debug.Log("Developer startLoadingADSource------" + "->" + JsonMapper.ToJson(erg.callbackInfo.toAdsourceDictionary()));

    }

    public void finishLoadingADSource(object sender, ATAdEventArgs erg)
    {
        Debug.Log("Developer finishLoadingADSource------" + "->" + JsonMapper.ToJson(erg.callbackInfo.toAdsourceDictionary()));

    }

    public void failToLoadADSource(object sender, ATAdErrorEventArgs erg)
    {
        Debug.Log("Developer failToLoadADSource------erg.errorCode:" + erg.errorCode + "---erg.errorMessage:" + erg.errorMessage + "->" + JsonMapper.ToJson(erg.callbackInfo.toAdsourceDictionary()));

    }

    public void startBiddingADSource(object sender, ATAdEventArgs erg)
    {
        Debug.Log("Developer startBiddingADSource------" + "->" + JsonMapper.ToJson(erg.callbackInfo.toAdsourceDictionary()));

    }

    public void finishBiddingADSource(object sender, ATAdEventArgs erg)
    {
        Debug.Log("Developer finishBiddingADSource------" + "->" + JsonMapper.ToJson(erg.callbackInfo.toAdsourceDictionary()));

    }

    public void failBiddingADSource(object sender, ATAdErrorEventArgs erg)
    {
        Debug.Log("Developer failBiddingADSource------erg.errorCode:" + erg.errorCode + "---erg.errorMessage:" + erg.errorMessage + "->" + JsonMapper.ToJson(erg.callbackInfo.toAdsourceDictionary()));

    }

    public void hideBannerAd()
    {
        ATBannerAd.Instance.hideBannerAd(this.adsId);
    }

    /*
     * Use this method when you want to reshow a banner that is previously hidden(by calling hideBannerAd)   
    */
    public void reshowBannerAd()
    {
        ATBannerAd.Instance.showBannerAd(this.adsId);
    }
}
#endif