using ASTeams.Base.Data;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>Keeps the daily flight's record in the saved profile, beside the player's other progress.</summary>
    public sealed class ProfileDailyFlightStore : IDailyFlightStore
    {
        private const string DayKey = "daily_day";
        private const string StreakKey = "daily_streak";

        private readonly UserProfileController profile;

        public ProfileDailyFlightStore(UserProfileController profile)
        {
            this.profile = profile;
        }

        public int LastFlownDay => profile == null ? 0 : profile.GetParam<int>(DayKey);

        public int DayStreak => profile == null ? 0 : profile.GetParam<int>(StreakKey);

        public void Save(int day, int dayStreak)
        {
            if (profile == null)
            {
                return;
            }

            profile.SetParam(DayKey, day);
            profile.SetParam(StreakKey, dayStreak);
        }
    }
}
