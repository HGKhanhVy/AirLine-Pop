using System;
using ASTeams.Base.Ads;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Rewarded ads through the SDK's AdsController.
    ///
    /// Without the ADS define the SDK compiles its rewarded call to nothing and never
    /// calls back, which would leave a button waiting forever. This answers for it
    /// instead: Editor and development builds are granted the reward so the feature can
    /// be tried, a release build is refused.
    /// </summary>
    public sealed class SdkRewardedAdService : IRewardedAdService
    {
        private readonly AdsController ads;

        public SdkRewardedAdService(AdsController ads)
        {
            this.ads = ads;
        }

        public void Show(string placement, Action<bool> onFinished)
        {
#if ADS
            if (ads == null)
            {
                onFinished?.Invoke(false);
                return;
            }

            ads.ShowRewardedAds(placement, isRewarded => onFinished?.Invoke(isRewarded));
#elif UNITY_EDITOR || DEVELOPMENT_BUILD
            onFinished?.Invoke(true);
#else
            onFinished?.Invoke(false);
#endif
        }
    }
}
