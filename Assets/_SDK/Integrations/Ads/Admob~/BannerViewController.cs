using System;
using UnityEngine;
using System.Drawing;
using ASTeams.Base.Ads;


#if ADS
using GoogleMobileAds.Api;
#endif
using UnityEngine.Events;

namespace GoogleMobileAds.Sample
{
    /// <summary>
    /// Demonstrates how to use Google Mobile Ads banner views.
    /// </summary>
    [AddComponentMenu("GoogleMobileAds/Samples/BannerViewController")]
    public class BannerViewController : MonoBehaviour
    {
#if ADS
        /// <summary>
        /// UI element activated when an ad is ready to show.
        /// </summary>
        public UnityEvent<bool> OnAdLoaded = new UnityEvent<bool>();

        private BannerView bannerView;

        /// <summary>
        /// Creates the banner view and loads a banner ad.
        /// </summary>
        public void LoadAd(string adUnitId, AdBannerType adType, AdPositionType adPosition)
        {
            if (bannerView != null)
            {
                DestroyAd();
            }

            // Create an instance of a banner view first.
            if (bannerView == null)
            {
                switch (adType)
                {
                    case AdBannerType.Standard:
                        bannerView = new BannerView(adUnitId, AdSize.Banner, (AdPosition)((int)adPosition));
                        break;
                    case AdBannerType.SmartBanner:
                        bannerView = new BannerView(adUnitId, AdSize.SmartBanner, (AdPosition)((int)adPosition));
                        break;
                    default:
                        break;
                }         
                ListenToAdEvents();
            }

            // Create our request used to load the ad.
            var adRequest = new AdRequest();

            // Send the request to load the ad.
            Debug.Log("Loading banner ad.");
            bannerView.LoadAd(adRequest);
        }

        /// <summary>
        /// Shows the ad.
        /// </summary>
        public void ShowAd()
        {
            if (bannerView != null)
            {
                Debug.Log("Showing banner view.");
                bannerView.Show();
            }
        }

        /// <summary>
        /// Hides the ad.
        /// </summary>
        public void HideAd()
        {
            if (bannerView != null)
            {
                Debug.Log("Hiding banner view.");
                bannerView.Hide();
            }
        }

        /// <summary>
        /// Destroys the ad.
        /// When you are finished with a BannerView, make sure to call
        /// the Destroy() method before dropping your reference to it.
        /// </summary>
        public void DestroyAd()
        {
            if (bannerView != null)
            {
                Debug.Log("Destroying banner view.");
                bannerView.Destroy();
                bannerView = null;
            }

            // Inform the UI that the ad is not ready.
            OnAdLoaded?.Invoke(false);
        }

        /// <summary>
        /// Logs the ResponseInfo.
        /// </summary>
        public void LogResponseInfo()
        {
            if (bannerView != null)
            {
                var responseInfo = bannerView.GetResponseInfo();
                if (responseInfo != null)
                {
                    UnityEngine.Debug.Log(responseInfo);
                }
            }
        }

        /// <summary>
        /// Listen to events the banner may raise.
        /// </summary>
        private void ListenToAdEvents()
        {
            // Raised when an ad is loaded into the banner view.
            bannerView.OnBannerAdLoaded += () =>
            {
                Debug.Log("Banner view loaded an ad with response : "
                    + bannerView.GetResponseInfo());

                // Inform the UI that the ad is ready.
                OnAdLoaded?.Invoke(true);
            };
            // Raised when an ad fails to load into the banner view.
            bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
            {
                Debug.LogError("Banner view failed to load an ad with error : " + error);
            };
            // Raised when the ad is estimated to have earned money.
            bannerView.OnAdPaid += (AdValue adValue) =>
            {
                Debug.Log(String.Format("Banner view paid {0} {1}.",
                    adValue.Value,
                    adValue.CurrencyCode));
            };
            // Raised when an impression is recorded for an ad.
            bannerView.OnAdImpressionRecorded += () =>
            {
                Debug.Log("Banner view recorded an impression.");
            };
            // Raised when a click is recorded for an ad.
            bannerView.OnAdClicked += () =>
            {
                Debug.Log("Banner view was clicked.");
            };
            // Raised when an ad opened full screen content.
            bannerView.OnAdFullScreenContentOpened += () =>
            {
                Debug.Log("Banner view full screen content opened.");
            };
            // Raised when the ad closed full screen content.
            bannerView.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("Banner view full screen content closed.");
            };
        }
#endif
    }
}
