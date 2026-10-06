namespace ASTeams.SingleLine.Data
{
    /// <summary>Remembers how many replays have paid coins, and on which day.</summary>
    public interface IReplayRewardStore
    {
        /// <summary>The day the count belongs to, as <see cref="ReplayAllowance.Claim"/> was given it.</summary>
        int Day { get; }

        int PaidCount { get; }

        void Save(int day, int paidCount);
    }
}
