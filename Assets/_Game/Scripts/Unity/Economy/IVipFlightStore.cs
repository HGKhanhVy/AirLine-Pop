namespace ASTeams.SingleLine.Unity
{
    /// <summary>The VIP flights the player has landed, and when the last one was.</summary>
    public interface IVipFlightStore
    {
        void MarkFlown(int levelNumber, int day);

        bool HasFlown(int levelNumber);

        /// <summary>True on the day of the last VIP landing and the day after, while the VIP guest visits.</summary>
        bool IsGuestVisiting(int today);
    }
}
