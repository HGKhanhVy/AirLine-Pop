
namespace AirlinePop.SingleLine.Unity
{
    /// <summary>Keeps the daily flight's record in the saved profile, beside the player's other progress.</summary>
    public sealed class ProfileDailyFlightStore : IDailyFlightStore
    {
        private const string DayKey = "daily_day";
        private const string StreakKey = "daily_streak";

        private readonly PlayerProfile profile;

        public ProfileDailyFlightStore(PlayerProfile profile)
        {
            this.profile = profile;
        }

        public int LastFlownDay => profile == null ? 0 : profile.Get<int>(DayKey);

        public int DayStreak => profile == null ? 0 : profile.Get<int>(StreakKey);

        public void Save(int day, int dayStreak)
        {
            if (profile == null)
            {
                return;
            }

            profile.Set(DayKey, day);
            profile.Set(StreakKey, dayStreak);
        }
    }
}
