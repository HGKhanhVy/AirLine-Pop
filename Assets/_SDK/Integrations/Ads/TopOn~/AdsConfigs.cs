using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ASTeams.Base.Ads
{
    public enum AdBannerType
    {
        Standard,
        SmartBanner
    }

    public enum AdPositionType
    {
        Top,
        Bottom,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Center
    }

    [CreateAssetMenu(menuName = "ASTeams/AdsConfigs", fileName = "AdsConfigs")]
    public class AdsConfigs : ScriptableObject
    {
        public List<string> testDeviceIds = new List<string> { "96e23e80653bb28980d3f40beb58915c" };

        [Space(10)]
        [Header("App")]
#if UNITY_ANDROID
        public string SdkAppId = "a5aa1f9deda26d";
        public string SdkKey = "4f7b9ac17decb9babec83aac078742c7";
#elif UNITY_IOS
        public string SdkAppId = "a5b0e8491845b3";
        public string SdkKey = "7eae0567827cfe2b22874061763f30c9";
#else
        public string SdkAppId = "";
        public string SdkKey = "";
#endif

        //open ad
        [Space(10)]
        [Header("Open Ad")]
        public bool isOpenAd;
#if UNITY_ANDROID
        public string openAdUnitId = "b5bea7cc9a4497";
#elif UNITY_IPHONE
        public string openAdUnitId = "b5c22f0e5cc7a0";
#else
        public string openAdUnitId = "unused";
#endif
        public float timeToTriggerOpenAd = 180f; // thời gian ngoài app đủ lâu mới trigger open ad: 3 mins

        //banner
        [Space(10)]
        [Header("Banner")]
        public bool isBanner;
#if UNITY_ANDROID
        public string bannerAdUnitId = "b5baca4f74c3d8";
#elif UNITY_IPHONE
        public string bannerAdUnitId = "b5bacaccb61c29";
#else
        public string bannerAdUnitId = "unused";
#endif
        public AdBannerType bannerAdsize;
        public AdPositionType bannerPosition;

        //interstitial
        [Space(10)]
        [Header("Interstitial")]
#if UNITY_ANDROID
        public string interstitialAdUnitId = "b5baca53984692";
#elif UNITY_IPHONE
        public string interstitialAdUnitId = "b5bacad26a752a";
#else
        public string interstitialAdUnitId = "unused";
#endif

        //rewarded
        [Space(10)]
        [Header("Reward")]
#if UNITY_ANDROID
        public string rewardedAdUnitId = "b5b449fb3d89d7";
#elif UNITY_IPHONE
        public string rewardedAdUnitId = "b5b44a0f115321";
#else
        public string rewardedAdUnitId = "unused";
#endif

        public float reloadInterval = 10f; // Interval for checking ad load status
        public float delayBetweenAds = 60f; //giữa các inter ads delay 60s
    }
}

