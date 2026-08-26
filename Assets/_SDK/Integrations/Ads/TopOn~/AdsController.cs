using ASTeams.Base.Analytics;
using ASTeams.Base.Utils;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using ASTeams.Base.Data;


#if TOPON
using AnyThinkAds.Api;
using AnyThinkAds.ThirdParty;
#endif

namespace ASTeams.Base.Ads
{
#if TOPON
    public class AdsController : MonoSingleton<AdsController>, ATSDKInitListener
#else
    public class AdsController : MonoSingleton<AdsController>
#endif
    {
        [SerializeField] private AdsConfigs configs;

        private float ReloadInterval => configs.reloadInterval;
        private float reloadTimer;

        private float lastAppPausedTime = 0f;
        private float TimeToTriggerOpenAd => configs.timeToTriggerOpenAd;
        private bool isRewardShowing = false;

        private float lastShowAdsTime = 0f;
        private float DelayBetweenAds => configs.delayBetweenAds;

        private bool isNoAds;
        public bool IsNoAds
        {
            get
            {
                return isNoAds;
            }
            set
            {
                isNoAds = value;
                UserData.Instance.SetParam("isNoAds", value);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
#if TOPON
            InterstitialAdOperator.Instance?.destroyAd();
            RewardVideoAdOperator.Instance?.destroyAd();
            BannerAdOperator.Instance?.destroyAd();
            SplashAdOperator.Instance?.destroyAd();
#endif
        }

        public void InitAds()
        {
#if TOPON
            Debug.Log("Initializing TopOn SDK...");
            ATSDKAPI.setLogDebug(true);
            ATSDKAPI.setChannel("unity_default");
            ATSDKAPI.setSubChannel("unity_sub_default");
            ATSDKAPI.initSDK(configs.SdkAppId, configs.SdkKey, this);

            IsNoAds = UserData.Instance.GetParam<bool>("isNoAds");
#endif
        }

        // -------- CALLBACKS ----------
        public void initSuccess()
        {
#if TOPON
            Debug.Log("✅ TopOn SDK Initialized Successfully");
            InterstitialAdOperator.Instance.initializeAd(configs.interstitialAdUnitId);
            RewardVideoAdOperator.Instance.initializeAd(configs.rewardedAdUnitId);
            BannerAdOperator.Instance.initializeAd(configs.bannerAdUnitId);
            BannerAdOperator.Instance.initPosition(configs.bannerPosition.ToString().ToLower());
            SplashAdOperator.Instance.initializeAd(configs.openAdUnitId);
            LoadAllAds();
#endif
        }

        public void initFail(string msg)
        {
#if TOPON
            Debug.LogError($"❌ TopOn SDK Init Failed: {msg}");
#endif
        }

        // -------- LOAD ----------
        public void LoadAllAds()
        {
#if TOPON
            Debug.Log("🔄 Loading all ads...");
            InterstitialAdOperator.Instance?.loadAd();
            RewardVideoAdOperator.Instance?.loadAd();
            BannerAdOperator.Instance?.loadAd();
            SplashAdOperator.Instance?.loadAd();
#endif
        }

        private void Update()
        {
#if TOPON
            reloadTimer += Time.deltaTime;
            if (reloadTimer >= ReloadInterval)
            {
                reloadTimer = 0f;
                CheckAndReloadAds();
            }
#endif
        }

        private void OnApplicationFocus(bool hasFocus)
        {
#if TOPON
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
            {
                if (hasFocus)
                {
                    float timeAway = Time.realtimeSinceStartup - lastAppPausedTime;
                    bool justFinishedAd = IsJustShowAds();

                    if (timeAway > TimeToTriggerOpenAd &&
                        !isRewardShowing &&
                        !justFinishedAd &&
                        SplashAdOperator.Instance != null &&
                        SplashAdOperator.Instance.isAdReady())
                    {
                        Debug.Log("Show Open App Ad on refocus");
                        ShowSplash();
                    }
                }
                else
                {
                    lastAppPausedTime = Time.realtimeSinceStartup;
                }
            });
#endif
        }

        private void CheckAndReloadAds()
        {
#if TOPON
            if (configs.isOpenAd && (SplashAdOperator.Instance != null && !SplashAdOperator.Instance.isAdReady()))
                SplashAdOperator.Instance.loadAd();

            if (InterstitialAdOperator.Instance != null && !InterstitialAdOperator.Instance.isAdReady())
                InterstitialAdOperator.Instance.loadAd();

            if (RewardVideoAdOperator.Instance != null && !RewardVideoAdOperator.Instance.isAdReady())
                RewardVideoAdOperator.Instance.loadAd();
#endif
        }

        // -------- INTERSTITIAL ----------
        public bool IsInterstitialReady()
        {
#if TOPON
            return InterstitialAdOperator.Instance != null && InterstitialAdOperator.Instance.isAdReady();
#else
            return false;
#endif
        }

        public void ShowInterstitialAds(string from)
        {
#if TOPON
            if (InterstitialAdOperator.Instance == null)
            {
                Debug.LogWarning("Interstitial Operator not initialized.");
                return;
            }

            if (IsNoAds)
            {
                Debug.Log("Removed ads");
                return;
            }

            if (InterstitialAdOperator.Instance.isAdReady())
            {
                InterstitialAdOperator.Instance.showAd();

                // Đảm bảo Unity API chạy trên main thread
                UnityMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    UpdateShowAdsTime();
                    AnalyticsController.Instance.LogEvent($"show_ads_interstitial_{from}");
                });
            }
            else
            {
                AnalyticsController.Instance.LogEvent($"fail_ads_interstitial_{from}");
                InterstitialAdOperator.Instance.loadAd();
            }
#endif
        }

        // -------- REWARDED ----------
        public void ShowRewardedAds(string from, UnityAction<bool> onComplete)
        {
#if UNITY_EDITOR
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
            {
                onComplete?.Invoke(true);
                return;
            });
            return;
#endif
#if TOPON
            // ✅ Đảm bảo toàn bộ logic dưới đây luôn chạy trên main thread
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
            {
                if (RewardVideoAdOperator.Instance == null)
                {
                    Debug.LogWarning("Reward Operator not initialized.");
                    onComplete?.Invoke(false);
                    return;
                }

                // 🔹 Kiểm tra xem quảng cáo đã sẵn sàng chưa
                if (RewardVideoAdOperator.Instance.isAdReady())
                {
                    isRewardShowing = true;

                    // ✅ Callback OnRewarded: cũng ép về main thread
                    RewardVideoAdOperator.Instance.OnRewared = (success) =>
                    {
                        UnityMainThreadDispatcher.Instance.Enqueue(() =>
                        {
                            isRewardShowing = false;
                            UpdateShowAdsTime();
                            onComplete?.Invoke(success);

                            Debug.Log(success
                                ? $"✅ Rewarded success from: {from}"
                                : $"⚠️ Rewarded closed/no reward from: {from}");
                        });
                    };

                    // 🔹 Hiển thị quảng cáo
                    RewardVideoAdOperator.Instance.showAd();

                    // 🔹 Ghi log analytic
                    AnalyticsController.Instance.LogEvent($"show_ads_rewarded_{from}");
                    Debug.Log($"▶️ Show rewarded ad from: {from}");
                }
                else
                {
                    // 🔹 Khi không có ad sẵn sàng, log và reload — tất cả trong main thread
                    onComplete?.Invoke(false);
                    AnalyticsController.Instance.LogEvent($"fail_ads_rewarded_{from}");
                    Debug.Log($"❌ Rewarded not ready, reloading... from: {from}");

                    RewardVideoAdOperator.Instance.loadAd();
                }
            });
#endif
        }

        // -------- BANNER ----------
        public void ShowBannerAds()
        {
#if TOPON
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
            {
                BannerAdOperator.Instance?.showAd();
            });
#endif
        }

        public void HideBannerAds()
        {
#if TOPON
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
            {
                BannerAdOperator.Instance?.hideBannerAd();
            });
#endif
        }

        // -------- OPEN APP / SPLASH ----------
        public void ShowSplash()
        {
#if TOPON
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
            {
                SplashAdOperator.Instance?.showAd();
                UpdateShowAdsTime();
            });
#endif
        }

        // -------- UTILITY ----------
        private void UpdateShowAdsTime()
        {
            // Đảm bảo luôn gọi trong main thread
            if (UnityMainThreadDispatcher.Instance != null)
            {
                UnityMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    lastShowAdsTime = Time.realtimeSinceStartup;
                });
            }
            else
            {
                // fallback (chạy khi chưa có dispatcher)
                lastShowAdsTime = Time.realtimeSinceStartup;
            }
        }

        private bool IsJustShowAds()
        {
            // Kiểm tra an toàn
            float now = Time.realtimeSinceStartup;
            return now - lastShowAdsTime < DelayBetweenAds;
        }

        public void SetRemoveAds(bool isNoAds)
        {
            IsNoAds = isNoAds;
        }
    }
}
