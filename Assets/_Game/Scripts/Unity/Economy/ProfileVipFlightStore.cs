
namespace AirlinePop.SingleLine.Unity
{
    /// <summary>Keeps the VIP flights landed in the saved profile, beside the player's other progress.</summary>
    public sealed class ProfileVipFlightStore : IVipFlightStore
    {
        private const string FlightPrefix = "vip_flight_";
        private const string DayKey = "vip_day";

        // How many days after the landing the guest stays: that day and the next.
        private const int VisitDays = 1;

        private readonly PlayerProfile profile;

        public ProfileVipFlightStore(PlayerProfile profile)
        {
            this.profile = profile;
        }

        public void MarkFlown(int levelNumber, int day)
        {
            if (profile == null)
            {
                return;
            }

            profile.Set(FlightPrefix + levelNumber, true);
            profile.Set(DayKey, day);
        }

        public bool HasFlown(int levelNumber)
        {
            return profile != null && profile.Get<bool>(FlightPrefix + levelNumber);
        }

        public bool IsGuestVisiting(int today)
        {
            if (profile == null)
            {
                return false;
            }

            int day = profile.Get<int>(DayKey);
            return day > 0 && today >= day && today - day <= VisitDays;
        }
    }
}
