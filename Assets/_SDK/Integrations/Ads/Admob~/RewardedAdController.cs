using System;
using UnityEngine;
#if ADS
using GoogleMobileAds.Api;
#endif
using UnityEngine.Events;

namespace GoogleMobileAds.Sample
{
    /// <summary>
    /// Demonstrates how to use Google Mobile Ads rewarded ads.
    /// </summary>
    [AddComponentMenu("GoogleMobileAds/Samples/RewardedAdController")]
    public class RewardedAdController : MonoBehaviour
    {
#if ADS
        public UnityEvent<bool> OnAdLoaded = new UnityEvent<bool>();
        public UnityAction<bool> OnRewarded;

        private RewardedAd rewardedAd;

        /// <summary>
        /// Loads the ad.
        /// </summary>
        public void LoadAd(string adUnitId)
        {
            // Clean up the old ad before loading a new one.
            if (rewardedAd != null)
            {
                DestroyAd();
            }

            Debug.Log("Loading rewarded ad.");

            // Create our request used to load the ad.
            var adRequest = new AdRequest();

            // Send the request to load the ad.
            RewardedAd.Load(adUnitId, adRequest, (RewardedAd ad, LoadAdError error) =>
            {
                // If the operation failed with a reason.
                if (error != null)
                {
                    Debug.LogError("Rewarded ad failed to load an ad with error : " + error);
                    return;
                }
                // If the operation failed for unknown reasons.
                // This is an unexpected error, please report this bug if it happens.
                if (ad == null)
                {
                    Debug.LogError("Unexpected error: Rewarded load event fired with null ad and null error.");
                    return;
                }

                // The operation completed successfully.
                Debug.Log("Rewarded ad loaded with response : " + ad.GetResponseInfo());
                rewardedAd = ad;

                // Register to ad events to extend functionality.
                RegisterEventHandlers(ad);

                // Inform the UI that the ad is ready.
                OnAdLoaded?.Invoke(true);
            });
        }

        /// <summary>
        /// Shows the ad.
        /// </summary>
        public void ShowAd()
        {
            if (rewardedAd != null && rewardedAd.CanShowAd())
            {
                Debug.Log("Showing rewarded ad.");
                rewardedAd.Show((Reward reward) =>
                {
                    Debug.Log(String.Format("Rewarded ad granted a reward: {0} {1}",
                                            reward.Amount,
                                            reward.Type));
                    OnRewarded?.Invoke(true);
                });
            }
            else
            {
                Debug.LogError("Rewarded ad is not ready yet.");
                OnRewarded?.Invoke(false);
            }

            // Inform the UI that the ad is not ready.
            OnAdLoaded?.Invoke(false);
        }

        /// <summary>
        /// Destroys the ad.
        /// </summary>
        public void DestroyAd()
        {
            if (rewardedAd != null)
            {
                Debug.Log("Destroying rewarded ad.");
                rewardedAd.Destroy();
                rewardedAd = null;
            }

            // Inform the UI that the ad is not ready.
            OnAdLoaded?.Invoke(false);
        }

        /// <summary>
        /// Logs the ResponseInfo.
        /// </summary>
        public void LogResponseInfo()
        {
            if (rewardedAd != null)
            {
                var responseInfo = rewardedAd.GetResponseInfo();
                UnityEngine.Debug.Log(responseInfo);
            }
        }

        private void RegisterEventHandlers(RewardedAd ad)
        {
            // Raised when the ad is estimated to have earned money.
            ad.OnAdPaid += (AdValue adValue) =>
            {
                Debug.Log(String.Format("Rewarded ad paid {0} {1}.",
                    adValue.Value,
                    adValue.CurrencyCode));
            };
            // Raised when an impression is recorded for an ad.
            ad.OnAdImpressionRecorded += () =>
            {
                Debug.Log("Rewarded ad recorded an impression.");
            };
            // Raised when a click is recorded for an ad.
            ad.OnAdClicked += () =>
            {
                Debug.Log("Rewarded ad was clicked.");
            };
            // Raised when the ad opened full screen content.
            ad.OnAdFullScreenContentOpened += () =>
            {
                Debug.Log("Rewarded ad full screen content opened.");
            };
            // Raised when the ad closed full screen content.
            ad.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("Rewarded ad full screen content closed.");
            };
            // Raised when the ad failed to open full screen content.
            ad.OnAdFullScreenContentFailed += (AdError error) =>
            {
                Debug.LogError("Rewarded ad failed to open full screen content with error : "
                    + error);
            };
        }

        public bool IsAdLoaded()
        {
            return rewardedAd != null && rewardedAd.CanShowAd();
        }
#endif
    }
}
