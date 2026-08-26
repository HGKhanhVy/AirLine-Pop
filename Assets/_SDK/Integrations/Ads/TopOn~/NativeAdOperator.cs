//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.SceneManagement;
//using AnyThinkAds.Api;
//using UnityEngine.UI;

//using AnyThinkAds.ThirdParty.LitJson;


//public class NativeAdOperator : BaseAdOperator
//{
//    public static ATNativeAdView nativeAdView;

//    private static readonly NativeAdOperator instance = new NativeAdOperator();

//    private NativeAdOperator() 
//	{
        
//	}

//	public static NativeAdOperator Instance 
//	{
//        get 
//		{
//			return instance;
//		}
//	}

//    // Use this for initialization
//    public override void initializeAd()
//    {
//        Debug.Log("NativeAdOperator: start() >>> called");
//        ATNativeAd.Instance.client.onAdLoadEvent += onAdLoad;
//        ATNativeAd.Instance.client.onAdLoadFailureEvent += onAdLoadFail;
//        ATNativeAd.Instance.client.onAdImpressEvent += onAdImpressed;
//        ATNativeAd.Instance.client.onAdClickEvent += onAdClick;
//        ATNativeAd.Instance.client.onAdCloseEvent += onAdClose;
//        ATNativeAd.Instance.client.onAdVideoStartEvent += onAdVideoStart;
//        ATNativeAd.Instance.client.onAdVideoEndEvent += onAdVideoEnd;
//        ATNativeAd.Instance.client.onAdVideoProgressEvent += onAdVideoProgress;
//        ATNativeAd.Instance.client.onAdSourceAttemptEvent += startLoadingADSource;
//        ATNativeAd.Instance.client.onAdSourceFilledEvent += finishLoadingADSource;
//        ATNativeAd.Instance.client.onAdSourceLoadFailureEvent += failToLoadADSource;
//        ATNativeAd.Instance.client.onAdSourceBiddingAttemptEvent += startBiddingADSource;
//        ATNativeAd.Instance.client.onAdSourceBiddingFilledEvent += finishBiddingADSource;
//        ATNativeAd.Instance.client.onAdSourceBiddingFailureEvent += failBiddingADSource;
//    }

//    public override void destroyAd()
//    {
//        ATNativeAd.Instance.client.onAdLoadEvent -= onAdLoad;
//        ATNativeAd.Instance.client.onAdLoadFailureEvent -= onAdLoadFail;
//        ATNativeAd.Instance.client.onAdImpressEvent -= onAdImpressed;
//        ATNativeAd.Instance.client.onAdClickEvent -= onAdClick;
//        ATNativeAd.Instance.client.onAdCloseEvent -= onAdClose;
//        ATNativeAd.Instance.client.onAdVideoStartEvent -= onAdVideoStart;
//        ATNativeAd.Instance.client.onAdVideoEndEvent -= onAdVideoEnd;
//        ATNativeAd.Instance.client.onAdVideoProgressEvent -= onAdVideoProgress;
//        ATNativeAd.Instance.client.onAdSourceAttemptEvent -= startLoadingADSource;
//        ATNativeAd.Instance.client.onAdSourceFilledEvent -= finishLoadingADSource;
//        ATNativeAd.Instance.client.onAdSourceLoadFailureEvent -= failToLoadADSource;
//        ATNativeAd.Instance.client.onAdSourceBiddingAttemptEvent -= startBiddingADSource;
//        ATNativeAd.Instance.client.onAdSourceBiddingFilledEvent -= finishBiddingADSource;
//        ATNativeAd.Instance.client.onAdSourceBiddingFailureEvent -= failBiddingADSource;

//        ATNativeAd.Instance.cleanAdView(mPlacementId_native_all, NativeAdOperator.nativeAdView);
//    }

//    public override void showAd() 
//    {
//        bool isAdReady = ATNativeAd.Instance.hasAdReady(mPlacementId_native_all);
//        if (isAdReady) {
//            showNativeAd();
//        } else {
//            loadAd();
//        }
//    }

//    public override void loadAd()
//    {
//        Debug.Log("Developer load native, placementId = " + mPlacementId_native_all);
//        //new in v5.6.6
//        Dictionary<string, object> jsonmap = new Dictionary<string, object>();

//#if UNITY_ANDROID
//            ATSize nativeSize = new ATSize(900, 600);

//            //support gdt pangle template-rendering
//            jsonmap.Add(AnyThinkAds.Api.ATConst.ADAPTIVE_HEIGHT, AnyThinkAds.Api.ATConst.ADAPTIVE_HEIGHT_YES);;//Adaptive height, only for template-rendering

//            jsonmap.Add(ATNativeAdLoadingExtra.kATNativeAdLoadingExtraNativeAdSizeStruct, nativeSize);
//#elif UNITY_IOS || UNITY_IPHONE
//            ATSize nativeSize = new ATSize(320, 250, false);
//            jsonmap.Add(ATNativeAdLoadingExtra.kATNativeAdLoadingExtraNativeAdSizeStruct, nativeSize);
        
//#endif
//        setLoading();
//        ATNativeAd.Instance.loadNativeAd(mPlacementId_native_all, jsonmap);
//    }

//    public int ColorToInt(Color c)
//    {
//        int retVal = 0;
//        retVal |= Mathf.RoundToInt(c.r * 255f) << 24;
//        retVal |= Mathf.RoundToInt(c.g * 255f) << 16;
//        retVal |= Mathf.RoundToInt(c.b * 255f) << 8;
//        retVal |= Mathf.RoundToInt(c.a * 255f);
//        return retVal;
//    }

//    public void showNativeAd()
//    {
//        Debug.Log("Developer is native ready....");
//        string adStatus = ATNativeAd.Instance.checkAdStatus(mPlacementId_native_all);
//        Debug.Log("Developer checkAdStatus native...." + adStatus);


//        Debug.Log("Developer show native....");
//        ATNativeConfig conifg = new ATNativeConfig();

//        string bgcolor = "#ffffff";
//        string textcolor = "#000000";
//#if UNITY_ANDROID

//		int rootbasex = 100, rootbasey = 100;
//		//父框架
//		int x = rootbasex,y = rootbasey,width = 300*3,height = 200*3,textsize = 17;
//		conifg.parentProperty = new ATNativeItemProperty(x,y,width,height,bgcolor,textcolor,textsize);

//		//adlogo 
//		x = 0*3;y = 0*3;width = 30*3;height = 20*3;textsize = 17;
//		conifg.adLogoProperty  = new ATNativeItemProperty(x,y,width,height,bgcolor,textcolor,textsize);


//		//adicon
//		x = 0*3;y = 50*3-50;width = 60*3;height = 50*3;textsize = 17;
//		conifg.appIconProperty  = new ATNativeItemProperty(x,y,width,height,bgcolor,textcolor,textsize);

//		//ad cta 
//		x = 0*3;y = 150*3;width = 300*3;height = 50*3;textsize = 17;
//		conifg.ctaButtonProperty  = new ATNativeItemProperty(x,y,width,height,"#ff21bcab","#ffffff",textsize);

//		//ad desc
//		x = 60*3;y = 100*3;width = 240*3-20;height = 50*3-10;textsize = 10;
//		conifg.descProperty  = new ATNativeItemProperty(x,y,width,height,bgcolor,"#777777",textsize);


//		//ad image
//		x = 60*3;y = 0*3+20;width = 240*3-20;height = 100*3-10;textsize = 17;
//		conifg.mainImageProperty  = new ATNativeItemProperty(x,y,width,height,bgcolor,textcolor,textsize);

//		//ad title 
//		x = 0*3;y = 100*3;width = 60*3;height = 50*3;textsize = 12;
//		conifg.titleProperty  = new ATNativeItemProperty(x,y,width,height,bgcolor,textcolor,textsize);

//		//ad dislike button (close button)
//        x = 300*3 - 75;y = 0;width = 75;height = 75;textsize = 17;
//        conifg.dislikeButtonProperty  = new ATNativeItemProperty(x,y,width,height,"#00000000",textcolor,textsize, true);


//#elif UNITY_IOS || UNITY_IPHONE

//		int rootbasex = 30, rootbasey = 90, totalWidth = Screen.width - 60, totalHeight = 350;
//		//ad frame
//		int x = rootbasex,y = rootbasey,width = totalWidth,height = totalHeight,textsize = 17;
//		conifg.parentProperty = new ATNativeItemProperty(x,y,width,height,"clearColor",textcolor,textsize, true);

//		//adlogo
//		x = totalWidth - 30 - 15 - 15;y = totalHeight - 30 - 15;width = 30;height = 30;textsize = 17;
//		conifg.adLogoProperty  = new ATNativeItemProperty(x,y,width,height,"nil",textcolor,textsize, true);

//		//adicon
//		x = 0;y = 0;width = 90;height = 90;textsize = 34;
//		conifg.appIconProperty  = new ATNativeItemProperty(x,y,width,height,bgcolor,textcolor,textsize, true);

//		//ad title
//		x = width + 5;y = 0;width = totalWidth - 30 - x;height = 15 + 3;textsize = 18;
//		conifg.titleProperty  = new ATNativeItemProperty(x,y,width,height,"clearColor",textcolor,textsize, true);

//		//ad desc
//		x = x;y = y + height + 5;width = width;height = 13;textsize = 10;
//		conifg.descProperty  = new ATNativeItemProperty(x,y,width,height,bgcolor,"#777777",textsize, true);

//		//ad cta
//		x = x;y = y + height + 5;width = width;height = 15 + 3;textsize = 15;
//		conifg.ctaButtonProperty  = new ATNativeItemProperty(x,y,width,height,"#ff21bcab","#ffffff",textsize, true);

//		//ad image
//		x = 0;y = y + height + 5;width = totalWidth - 30;height = totalHeight - y;textsize = 17;
//		conifg.mainImageProperty  = new ATNativeItemProperty(x,y,width,height,bgcolor,textcolor,textsize, true);

//		//ad dislike button 
//        x = totalWidth - 75;y = 0;width = 75;height = 75;textsize = 17;
//        conifg.dislikeButtonProperty  = new ATNativeItemProperty(x,y,width,height,"#00000000",textcolor,textsize, true);
//#endif

//        ATNativeAdView nativeAdView = new ATNativeAdView(conifg);
//        NativeAdOperator.nativeAdView = nativeAdView;

//        //Debug.Log("Developer renderAdToScene--->");
//        //ATNativeAd.Instance.renderAdToScene(mPlacementId_native_all, nativeAdView);

//        // show with scenario
//        // Debug.Log("Developer renderAdToScene with scenariio--->");
//        //Dictionary<string, string> jsonmap = new Dictionary<string, string>();
//        //jsonmap.Add(AnyThinkAds.Api.ATConst.SCENARIO, showingScenario);
//        //ATNativeAd.Instance.renderAdToScene(mPlacementId_native_all, nativeAdView, jsonmap);



//        // show in adaptive for template-rendering ad
//        Debug.Log("Developer renderAdToScene with adatpive height--->");
//        Dictionary<string, string> jsonmap = new Dictionary<string, string>();
//        jsonmap.Add(AnyThinkAds.Api.ATConst.ADAPTIVE_HEIGHT, AnyThinkAds.Api.ATConst.ADAPTIVE_HEIGHT_YES);
//        //jsonmap.Add(AnyThinkAds.Api.ATConst.POSITION, AnyThinkAds.Api.ATConst.POSITION_BOTTOM);
//        jsonmap.Add(AnyThinkAds.Api.ATConst.POSITION, AnyThinkAds.Api.ATConst.POSITION_TOP);
//        ATNativeAd.Instance.renderAdToScene(mPlacementId_native_all, nativeAdView, jsonmap);
//    }

//    public void cleanAdView() 
//    {
//        ATNativeAd.Instance.cleanAdView(mPlacementId_native_all, NativeAdOperator.nativeAdView);
//    }


//    public void onAdImpressed(object sender, ATAdEventArgs erg)
//    {
//        Debug.Log("Developer callback onAdImpressed :" + erg.placementId + "->" + JsonMapper.ToJson(erg.callbackInfo.toDictionary()));
//    }

//    public void onAdClick(object sender, ATAdEventArgs erg)
//    {
//        Debug.Log("Developer callback onAdClick :" + erg.placementId + "->" + JsonMapper.ToJson(erg.callbackInfo.toDictionary()));
//    }

//    public void onAdClose(object sender, ATAdEventArgs erg)
//    {
//        Debug.Log("Developer callback onAdClose :" + erg.placementId);
//        cleanAdView();
//    }

//    public void onAdLoad(object sender, ATAdEventArgs erg)
//    {
//        Debug.Log("Developer callback onAdLoad :" + erg.placementId);
//        setLoadSuccess();
//    }

//    public void onAdLoadFail(object sender, ATAdErrorEventArgs erg)
//    {
//        Debug.Log("Developer callback onAdLoadFail : : " + erg.placementId + "--erg.errorCode:" + erg.errorCode + "--msg:" + erg.errorMessage);
//        setLoadFailed(erg);
//        // Native ad failed to load. We recommend retrying with exponentially higher delays up to a maximum delay (in this case 64 seconds).
//        retryAdAttempt();
//    }

//    public void onAdVideoStart(object sender, ATAdEventArgs erg)
//    {
//        Debug.Log("Developer onAdVideoStart------:" + erg.placementId);
//    }
//    public void onAdVideoEnd(object sender, ATAdEventArgs erg)
//    {
//        Debug.Log("Developer onAdVideoEnd------:" + erg.placementId);
//    }
//    public void onAdVideoProgress(object sender, ATAdProgressEventArgs erg)
//    {
//        Debug.Log("Developer onAdVideoProgress------:" + erg.placementId);
//    }

//    public void startLoadingADSource(object sender, ATAdEventArgs erg)
//    {
//        Debug.Log("Developer startLoadingADSource------" + "->" + JsonMapper.ToJson(erg.callbackInfo.toAdsourceDictionary()));

//    }

//    public void finishLoadingADSource(object sender, ATAdEventArgs erg)
//    {
//        Debug.Log("Developer finishLoadingADSource------" + "->" + JsonMapper.ToJson(erg.callbackInfo.toAdsourceDictionary()));

//    }

//    public void failToLoadADSource(object sender, ATAdErrorEventArgs erg)
//    {
//        Debug.Log("Developer failToLoadADSource------erg.errorCode:" + erg.errorCode + "---erg.errorMessage:" + erg.errorMessage + "->" + JsonMapper.ToJson(erg.callbackInfo.toAdsourceDictionary()));

//    }

//    public void startBiddingADSource(object sender, ATAdEventArgs erg)
//    {
//        Debug.Log("Developer startBiddingADSource------" + "->" + JsonMapper.ToJson(erg.callbackInfo.toAdsourceDictionary()));

//    }

//    public void finishBiddingADSource(object sender, ATAdEventArgs erg)
//    {
//        Debug.Log("Developer finishBiddingADSource------" + "->" + JsonMapper.ToJson(erg.callbackInfo.toAdsourceDictionary()));

//    }

//    public void failBiddingADSource(object sender, ATAdErrorEventArgs erg)
//    {
//        Debug.Log("Developer failBiddingADSource------erg.errorCode:" + erg.errorCode + "---erg.errorMessage:" + erg.errorMessage + "->" + JsonMapper.ToJson(erg.callbackInfo.toAdsourceDictionary()));

//    }
//}
