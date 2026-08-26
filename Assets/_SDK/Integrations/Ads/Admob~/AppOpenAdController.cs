using System;
using UnityEngine;
using UnityEngine.Events;

#if ADS
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
#endif

namespace GoogleMobileAds.Sample
{
    /// <summary>
    /// Demonstrates how to use Google Mobile Ads app open ads.
    /// </summary>
    [AddComponentMenu("GoogleMobileAds/Samples/AppOpenAdController")]
    public class AppOpenAdController : MonoBehaviour
    {

#if ADS
        public UnityEvent<bool> OnAdLoaded = new UnityEvent<bool>();


        //// App open ads can be preloaded for up to 4 hours.
        //private readonly TimeSpan TIMEOUT = TimeSpan.FromHours(4);
        //private DateTime _expireTime;

        private AppOpenAd appOpenAd;
        private string adUnitId;

        /// <summary>
        /// Loads the ad.
        /// </summary>
        public void LoadAd(string adUnitId)
        {
            this.adUnitId = adUnitId;

            // Clean up the old ad before loading a new one.
            if (appOpenAd != null)
            {
                DestroyAd();
            }

            Debug.Log("Loading app open ad.");

            // Create our request used to load the ad.
            var adRequest = new AdRequest();

            // Send the request to load the ad.
            AppOpenAd.Load(adUnitId, adRequest, (AppOpenAd ad, LoadAdError error) =>
                {
                    // If the operation failed with a reason.
                    if (error != null)
                    {
                        Debug.LogError("App open ad failed to load an ad with error : "
                                        + error);
                        return;
                    }

                    // If the operation failed for unknown reasons.
                    // This is an unexpected error, please report this bug if it happens.
                    if (ad == null)
                    {
                        Debug.LogError("Unexpected error: App open ad load event fired with " +
                                       " null ad and null error.");
                        return;
                    }

                    // The operation completed successfully.
                    Debug.Log("App open ad loaded with response : " + ad.GetResponseInfo());
                    appOpenAd = ad;

                    // App open ads can be preloaded for up to 4 hours.
                    //_expireTime = DateTime.Now + TIMEOUT;

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
            // App open ads can be preloaded for up to 4 hours.
            if (appOpenAd != null && appOpenAd.CanShowAd()) // && DateTime.Now < _expireTime)
            {
               Debug.Log("Showing app open ad.");
                appOpenAd.Show();
            }
            else
            {
                Debug.LogError("App open ad is not ready yet.");
            }

            // Inform the UI that the ad is not ready.
            OnAdLoaded?.Invoke(false);
        }

        /// <summary>
        /// Destroys the ad.
        /// </summary>
        public void DestroyAd()
        {
            if (appOpenAd != null)
            {
                Debug.Log("Destroying app open ad.");
                appOpenAd.Destroy();
                appOpenAd = null;
            }

            // Inform the UI that the ad is not ready.
            OnAdLoaded?.Invoke(false);
        }

        /// <summary>
        /// Logs the ResponseInfo.
        /// </summary>
        public void LogResponseInfo()
        {
            if (appOpenAd != null)
            {
                var responseInfo = appOpenAd.GetResponseInfo();
                UnityEngine.Debug.Log(responseInfo);
            }
        }

        private void RegisterEventHandlers(AppOpenAd ad)
        {
            // Raised when the ad is estimated to have earned money.
            ad.OnAdPaid += (AdValue adValue) =>
            {
                Debug.Log(String.Format("App open ad paid {0} {1}.",
                    adValue.Value,
                    adValue.CurrencyCode));
            };
            // Raised when an impression is recorded for an ad.
            ad.OnAdImpressionRecorded += () =>
            {
                Debug.Log("App open ad recorded an impression.");
            };
            // Raised when a click is recorded for an ad.
            ad.OnAdClicked += () =>
            {
                Debug.Log("App open ad was clicked.");
            };
            // Raised when an ad opened full screen content.
            ad.OnAdFullScreenContentOpened += () =>
            {
                Debug.Log("App open ad full screen content opened.");

                // Inform the UI that the ad is consumed and not ready.
                OnAdLoaded?.Invoke(false);
            };
            // Raised when the ad closed full screen content.
            ad.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("App open ad full screen content closed.");

                // It may be useful to load a new ad when the current one is complete.
                LoadAd(this.adUnitId);
            };
            // Raised when the ad failed to open full screen content.
            ad.OnAdFullScreenContentFailed += (AdError error) =>
            {
                Debug.LogError("App open ad failed to open full screen content with error : "
                                + error);
            };
        }

        public bool IsAdLoaded()
        {
            return appOpenAd != null && appOpenAd.CanShowAd();
        }

#endif
    }
}
