using System;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Plays a rewarded ad. The callback always fires exactly once: true when the reward
    /// is earned, false when the ad was skipped, failed or is unavailable.
    /// </summary>
    public interface IRewardedAdService
    {
        void Show(string placement, Action<bool> onFinished);
    }
}
