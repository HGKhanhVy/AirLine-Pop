using System;
using AirlinePop.SingleLine.Data;

namespace AirlinePop.SingleLine.Unity
{
    /// <summary>Keeps the day's paid replays in the saved profile, so closing the game does not reset them.</summary>
    public sealed class ProfileReplayRewardStore : IReplayRewardStore
    {
        private const string DayKey = "replay_day";
        private const string PaidKey = "replay_paid";

        private readonly PlayerProfile profile;

        public ProfileReplayRewardStore(PlayerProfile profile)
        {
            this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
        }

        public int Day => profile.Get<int>(DayKey);

        public int PaidCount => profile.Get<int>(PaidKey);

        public void Save(int day, int paidCount)
        {
            profile.Set(DayKey, day);
            profile.Set(PaidKey, paidCount);
        }
    }
}
