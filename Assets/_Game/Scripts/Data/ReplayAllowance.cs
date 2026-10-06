using System;

namespace ASTeams.SingleLine.Data
{
    /// <summary>
    /// The small pay for replaying a level already beaten. Only so many replays pay each
    /// day, so a whole day of replaying one easy board earns about what one new level
    /// does: enough to make a replay feel worth it, never more than flying on.
    /// </summary>
    public sealed class ReplayAllowance
    {
        private readonly IReplayRewardStore store;
        private readonly int coinsPerReplay;
        private readonly int paidPerDay;

        public ReplayAllowance(IReplayRewardStore store, int coinsPerReplay, int paidPerDay)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.coinsPerReplay = Math.Max(0, coinsPerReplay);
            this.paidPerDay = Math.Max(0, paidPerDay);
        }

        /// <summary>
        /// Pays one replay on <paramref name="today"/> and returns the coins, or 0 once the
        /// day's paid replays are used up. The count starts again each new day.
        /// </summary>
        public int Claim(int today)
        {
            if (coinsPerReplay == 0 || paidPerDay == 0)
            {
                return 0;
            }

            int paid = store.Day == today ? store.PaidCount : 0;

            if (paid >= paidPerDay)
            {
                return 0;
            }

            store.Save(today, paid + 1);
            return coinsPerReplay;
        }
    }
}
