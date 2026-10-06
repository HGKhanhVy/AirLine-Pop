using System;
using ASTeams.Base.Data;
using ASTeams.SingleLine.Data;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>Keeps the day's paid replays in the saved profile, so closing the game does not reset them.</summary>
    public sealed class ProfileReplayRewardStore : IReplayRewardStore
    {
        private const string DayKey = "replay_day";
        private const string PaidKey = "replay_paid";

        private readonly UserProfileController profile;

        public ProfileReplayRewardStore(UserProfileController profile)
        {
            this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
        }

        public int Day => profile.GetParam<int>(DayKey);

        public int PaidCount => profile.GetParam<int>(PaidKey);

        public void Save(int day, int paidCount)
        {
            profile.SetParam(DayKey, day);
            profile.SetParam(PaidKey, paidCount);
        }
    }
}
