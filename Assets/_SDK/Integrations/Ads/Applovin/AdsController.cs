using ASTeams.Base.Analytics;
using ASTeams.Base.Data;
using ASTeams.Base.RemoteConfigs;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
#if APPSFLYER
using AppsFlyerSDK;
#endif
#if APPLOVIN
using static MaxSdkCallbacks;
#endif
using ASTeams.Base.UI;
#if FIREBASE
using Firebase.Analytics;
#endif
using System;


namespace ASTeams.Base.Ads
{
    public class AdsController : MonoSingleton<AdsController>
    {
        [SerializeField] private AdsConfigs configs;

        private float reloadTimer;
        private float lastShowAdsTime = 0f;

        private bool isRewardShowing = false;
        private bool isBannerShowing = false;

        private bool isCallbacksRegistered;

        private UnityAction<bool> CallbackReward;

        // ------------------------------------------------------------
        // CONFIG TỪ REMOTE
        // ------------------------------------------------------------
        private int AdsInterval => RemoteConfigsController.ads_interval;
        private int LevelShowBanner => RemoteConfigsController.level_show_banner;
        private int LevelShowInter => RemoteConfigsController.level_show_inter;
        private bool ShowBannerGameplay => RemoteConfigsController.show_banner_gameplay;
        private bool HienQC => RemoteConfigsController.hien_qc;

        // ------------------------------------------------------------
        // NO ADS FLAG
        // ------------------------------------------------------------
        private bool isNoAds;
        public bool IsNoAds
        {
            get => isNoAds;
            set
            {
                isNoAds = value;
                UserProfileController.Instance.SetParam("isNoAds", value);

                if (isNoAds == true)
                {
                    if (isBannerShowing) HideBannerAds();
                }

                OnAdsRemoved?.Invoke();
            }
        }
        public UnityAction OnAdsRemoved;

        private string adPlacement;

        // ------------------------------------------------------------
        // APPLYER TRACKING HELPERS
        // ------------------------------------------------------------
        private int InterDisplayCount
        {
            get => PlayerPrefs.GetInt("af_inters_displayed_count", 0);
            set => PlayerPrefs.SetInt("af_inters_displayed_count", value);
        }

        private void Track(string key)
        {
#if APPSFLYER
            Debug.Log("[ADS] Track Event: " + key);
            AppsFlyer.sendEvent(key, new Dictionary<string, string>());
            //AnalyticsController.Instance.LogEvent(key);
#endif
        }

        private void TrackInterDisplayedCount()
        {
            InterDisplayCount++;
            int c = InterDisplayCount;
            if (c <= 20)
                Track($"af_inters_displayed_{c}");
        }

        // ============================================================
        // INIT
        // ============================================================
        public void InitAds()
        {
#if ADS
            if (isCallbacksRegistered)
            {
                Debug.Log("[ADS] Callbacks already registered → SKIP");
                return;
            }

            isCallbacksRegistered = true;

            IsNoAds = UserData.Instance.GetParam<bool>("isNoAds");

            MaxSdk.SetHasUserConsent(true);
            MaxSdk.SetDoNotSell(false);
            MaxSdk.InitializeSdk();

            MaxSdkCallbacks.OnSdkInitializedEvent += sdkConfiguration =>
            {
                InitializeBannerAds();
                InitializeInterstitialAds();
                InitializeRewardedAds();
                UpdateShowAdsTime();
            };
#endif
        }

        private void UpdateShowAdsTime()
        {
            lastShowAdsTime = Time.realtimeSinceStartup;
            Debug.Log("[ADS] UpdateShowAdsTime() = " + lastShowAdsTime);
        }

        private bool IsJustShowAds()
        {
            bool result = Time.realtimeSinceStartup - lastShowAdsTime < AdsInterval;
            Debug.Log("[ADS] IsJustShowAds() = " + result);
            return result;
        }

        // ============================================================
        #region INTERSTITIAL
        // ============================================================

        private void InitializeInterstitialAds()
        {
#if ADS
            Debug.Log("[ADS] Init Interstitial");

            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += (adUnit, info) =>
            {
                Debug.Log("[ADS] Interstitial Loaded");
                Track("af_inters_api_called");
            };

            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += (adUnit, err) =>
            {
                Debug.Log("[ADS] Interstitial Load FAILED: " + err.Message);
                Invoke(nameof(LoadInterstitial), 1.5f); //load fail load lại sau 1.5s
            };

            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += (adUnit, err, info) =>
            {
                Debug.Log("[ADS] Interstitial Display FAILED: " + err.Message);
                Invoke(nameof(LoadInterstitial), 1.5f); //displayed fail load lại sau 1.5s
            };

            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += (adUnit, info) =>
            {
                Debug.Log("[ADS] Interstitial Displayed");
                Track("af_inters_displayed");
                TrackInterDisplayedCount();
                UpdateShowAdsTime();
            };

            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += (adUnit, info) =>
            {
                Debug.Log("[ADS] Interstitial Hidden");
                UpdateShowAdsTime();
                Invoke(nameof(LoadInterstitial), 1.5f); //coi xong load lại sau 1.5s
            };

            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += (adUnit, info) =>
            {
                Debug.Log("[ADS] Interstitial Revenue: " + info.Revenue);
                TrackAdRevenue(info, "interstitial");
                UpdateShowAdsTime();
            };

            LoadInterstitial();
#endif
        }

        private void LoadInterstitial()
        {
#if ADS
            Debug.Log("[ADS] Load Interstitial");
            MaxSdk.LoadInterstitial(configs.interstitialAdUnitId);
#endif
        }

        public void ShowInterstitialAds(string from)
        {
#if ADS
            Debug.Log("[ADS] Try Show Interstitial from: " + from);

            if (!HienQC) return;
            if (IsNoAds) return;

            int level = UserData.Instance.GetParam<int>("currentLevel");
            if (level <= LevelShowInter)
            {
                Debug.Log("[ADS] Level too low → skip interstitial");
                return;
            }

            if (IsJustShowAds())
            {
                Debug.Log("[ADS] Just showed ad → skip");
                return;
            }

            Track("af_inters_ad_eligible");

            if (MaxSdk.IsInterstitialReady(configs.interstitialAdUnitId))
            {
                Debug.Log("[ADS] Interstitial READY → SHOW");                
                MaxSdk.ShowInterstitial(configs.interstitialAdUnitId);
                AnalyticsController.Instance.LogEvent($"show_ads_interstitial_{from}");
                UpdateShowAdsTime();
            }
            else
            {
                Debug.Log("[ADS] Interstitial NOT READY");
                AnalyticsController.Instance.LogEvent($"fail_ads_interstitial_{from}");
            }
#endif
        }

        #endregion

        // ============================================================
        #region REWARDED
        // ============================================================

        private void InitializeRewardedAds()
        {
#if ADS
            Debug.Log("[ADS] Init Rewarded");

            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += (adUnit, info) =>
            {
                Debug.Log("[ADS] Rewarded Loaded");
                Track("af_rewarded_api_called");
            };

            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += (adUnit, err) =>
            {
                Debug.Log("[ADS] Rewarded Load FAILED: " + err.Message);
                Invoke(nameof(LoadRewardedAd), 1.5f); //load fail load lại sau 1.5s
            };

            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += (adUnit, err, info) =>
            {
                Debug.Log("[ADS] Rewarded Display FAILED: " + err.Message);
                RunCallBackRewardOnMainThread(false);
                Invoke(nameof(LoadRewardedAd), 1.5f); //display fail load lại sau 1.5s
            };

            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += (adUnit, info) =>
            {
                Debug.Log("[ADS] Rewarded Displayed");
                Track("af_rewarded_ad_displayed");
            };

            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += (adUnit, info) =>
            {
                Debug.Log("[ADS] Rewarded Hidden");
                RunCallBackRewardOnMainThread(false);
                Invoke(nameof(LoadRewardedAd), 1.5f); //coi xong load lại sau 1.5s
            };

            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += (adUnit, reward, info) =>
            {
                Debug.Log("[ADS] Rewarded → GIVE REWARD");
                RunCallBackRewardOnMainThread(true);
                UpdateShowAdsTime();
            };

            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += (adUnit, info) =>
            {
                Debug.Log("[ADS] Rewarded Revenue: " + info.Revenue);
                TrackAdRevenue(info, "rewarded");
                UpdateShowAdsTime();
            };

            LoadRewardedAd();
#endif
        }

        private void LoadRewardedAd()
        {
#if ADS
            Debug.Log("[ADS] Load Rewarded");
            MaxSdk.LoadRewardedAd(configs.rewardedAdUnitId);
#endif
        }

        public void ShowRewardedAds(string from, UnityAction<bool> callback)
        {
#if ADS
            CallbackReward = callback;
            Debug.Log("[ADS] Try Show Rewarded: " + from);

            if (!HienQC)
            {
                callback?.Invoke(false);
                CallbackReward = null;
                return;
            }

            Track("af_rewarded_ad_eligible");
            if (MaxSdk.IsRewardedAdReady(configs.rewardedAdUnitId))
            {
                Debug.Log("[ADS] Rewarded READY → SHOW");

                MaxSdk.ShowRewardedAd(configs.rewardedAdUnitId);

                AnalyticsController.Instance.LogEvent($"show_ads_rewarded_{from}");
                isRewardShowing = true;

                UpdateShowAdsTime();
            }
            else
            {
                Debug.Log("[ADS] Rewarded NOT READY");
                AnalyticsController.Instance.LogEvent($"fail_ads_rewarded_{from}");

                callback?.Invoke(false);
                CallbackReward = null;

                UIMessageManager.Instance.ShowNoti("No ads available", "top");
            }
#endif
        }

        private void RunCallBackRewardOnMainThread(bool isSuccess)
        {
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
            {
                try
                {
                    if (CallbackReward!= null)
                    {
                        CallbackReward?.Invoke(isSuccess);
                        CallbackReward = null;
                        isRewardShowing = false;
                    } 
                }
                catch (Exception e)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogError("[ADS] RunOnMainThread exception:\n" + e);
#endif
                }
            });
        }


        #endregion

        // ============================================================
        #region BANNER
        // ============================================================

        private void InitializeBannerAds()
        {
#if ADS
            Debug.Log("[ADS] Init Banner");

            MaxSdkCallbacks.Banner.OnAdLoadedEvent += (adUnit, info) =>
            {
                Debug.Log("[ADS] Banner Loaded");
            };

            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += (adUnit, err) =>
            {
                Debug.Log("[ADS] Banner Load FAILED: " + err.Message);
            };

            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += (adUnit, info) =>
            {
                Debug.Log("[ADS] Banner Revenue: " + info.Revenue);
                TrackAdRevenue(info, "banner");
            };

            var position = configs.bannerPosition == AdPositionType.Top
                ? MaxSdkBase.BannerPosition.TopCenter
                : MaxSdkBase.BannerPosition.BottomCenter;

            MaxSdk.CreateBanner(configs.bannerAdUnitId, position);
            Debug.Log("[ADS] Banner Created at: " + position);
#endif
        }

        public bool ShowBannerAds()
        {
#if ADS
            Debug.Log("[ADS] ShowBannerAds()");

            if (!HienQC) return false;

            if (configs.isBanner && !isBannerShowing)
            {
                int level = UserData.Instance.GetParam<int>("currentLevel");
                if (ShowBannerGameplay && level >= LevelShowBanner && !IsNoAds)
                {
                    Debug.Log("[ADS] Banner meets conditions → SHOW");

                    isBannerShowing = true;
                    MaxSdk.SetBannerBackgroundColor(configs.bannerAdUnitId, Color.black);
                    MaxSdk.ShowBanner(configs.bannerAdUnitId);
                    return true;
                }

                Debug.Log($"[ADS] Banner conditions not met. ShowBannerGameplay {ShowBannerGameplay} Level {level}, needed {LevelShowBanner}");
            }
#endif

            return false;
        }

        public void HideBannerAds()
        {
#if ADS
            Debug.Log("[ADS] HideBannerAds()");
            isBannerShowing = false;
            MaxSdk.HideBanner(configs.bannerAdUnitId);
#endif
        }

        #endregion

        // ============================================================
        #region APPSFLYER AD REVENUE
        // ============================================================

#if ADS
        private void TrackAdRevenue(MaxSdkBase.AdInfo adInfo, string adsType)
        {
            Debug.Log($"[ADS] TrackAdRevenue() type={adsType}, value={adInfo.Revenue}");

            if (adInfo == null || adInfo.Revenue <= 0) return;

            if (adInfo.RevenuePrecision != "exact") //tránh bị trùng lặp
            {
                Debug.Log("[ADS] Skip non-exact revenue: " + adInfo.RevenuePrecision);
                return;
            }

            double revenueValue = adInfo.Revenue;
            string currency = "USD";
            string network = adInfo.NetworkName;
            string adUnit = adInfo.AdUnitIdentifier;
            string placement = string.IsNullOrEmpty(adInfo.Placement) ? "default" : adInfo.Placement;
            string countryCode = MaxSdk.GetSdkConfiguration().CountryCode;

            var additionalParams = new Dictionary<string, string>()
            {
                { AFAdRevenueEvent.COUNTRY,  countryCode },
                { AFAdRevenueEvent.AD_UNIT,  adUnit },
                { AFAdRevenueEvent.AD_TYPE,  adsType },
                { AFAdRevenueEvent.PLACEMENT, placement },
                { "network_name", network },
                { "ad_format", adsType },
            };

            //APPSFLYER
            AppsFlyerAdRevenue.logAdRevenue(
                monetizationNetwork: network,
                mediationNetwork: AppsFlyerAdRevenueMediationNetworkType.AppsFlyerAdRevenueMediationNetworkTypeApplovinMax,
                eventRevenue: revenueValue,
                revenueCurrency: currency,
                additionalParameters: additionalParams
            );


            //FIREBASE
            AnalyticsController.Instance.LogEvent(
                "ad_impression",
                ("ad_platform", "applovin_max"),
                ("ad_source", adInfo.NetworkName),
                ("ad_unit_name", adInfo.AdUnitIdentifier),
                ("ad_format", adsType),
                ("value", revenueValue),
                ("currency", "USD")
            );
        }
#endif

        #endregion
    }
}
