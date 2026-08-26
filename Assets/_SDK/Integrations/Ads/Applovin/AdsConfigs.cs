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
    }

    [CreateAssetMenu(menuName = "ASTeams/AdsConfigs", fileName = "AdsConfigs")]
    public class AdsConfigs : ScriptableObject
    {
        public List<string> testDeviceIds = new List<string> { "96e23e80653bb28980d3f40beb58915c" };

        [Space(10)]
        [Header("App")]
//#if UNITY_IOS
//        public string SdkKey = "";
//#else 
//        public string SdkKey = "";
//#endif

        //banner
        [Space(10)]
        [Header("Banner")]
        public bool isBanner;
#if UNITY_IOS
        public string bannerAdUnitId = "";
#else 
        public string bannerAdUnitId = "";
#endif
        public AdBannerType bannerAdsize;
        public AdPositionType bannerPosition;

        //interstitial
        [Space(10)]
        [Header("Interstitial")]
#if UNITY_IOS
        public string interstitialAdUnitId = "";
#else
        public string interstitialAdUnitId = "";
#endif

        //rewarded
        [Space(10)]
        [Header("Reward")]
#if UNITY_IOS
        public string rewardedAdUnitId = "";
#else
        public string rewardedAdUnitId = "";
#endif
    }
}

