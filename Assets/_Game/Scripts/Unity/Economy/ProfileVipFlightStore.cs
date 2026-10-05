using ASTeams.Base.Data;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>Keeps the VIP flights landed in the saved profile, beside the player's other progress.</summary>
    public sealed class ProfileVipFlightStore : IVipFlightStore
    {
        private const string FlightPrefix = "vip_flight_";
        private const string DayKey = "vip_day";

        // How many days after the landing the guest stays: that day and the next.
        private const int VisitDays = 1;

        private readonly UserProfileController profile;

        public ProfileVipFlightStore(UserProfileController profile)
        {
            this.profile = profile;
        }

        public void MarkFlown(int levelNumber, int day)
        {
            if (profile == null)
            {
                return;
            }

            profile.SetParam(FlightPrefix + levelNumber, true);
            profile.SetParam(DayKey, day);
        }

        public bool HasFlown(int levelNumber)
        {
            return profile != null && profile.GetParam<bool>(FlightPrefix + levelNumber);
        }

        public bool IsGuestVisiting(int today)
        {
            if (profile == null)
            {
                return false;
            }

            int day = profile.GetParam<int>(DayKey);
            return day > 0 && today >= day && today - day <= VisitDays;
        }
    }
}
