using System;

namespace AirlinePop.SingleLine.Unity
{
    /// <summary>
    /// One special flight a day, the same for everyone on that date. Flying it pays coins, a
    /// few more for each day in a row it has been flown, up to a cap; missing a day starts
    /// the count again.
    /// </summary>
    public sealed class DailyFlightService
    {
        private readonly IDailyFlightStore store;
        private readonly IDayClock clock;
        private readonly int baseCoins;
        private readonly int coinsPerDayInRow;
        private readonly int maxDaysCounted;

        public DailyFlightService(IDailyFlightStore store, IDayClock clock, int baseCoins, int coinsPerDayInRow, int maxDaysCounted)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            this.baseCoins = Math.Max(0, baseCoins);
            this.coinsPerDayInRow = Math.Max(0, coinsPerDayInRow);
            this.maxDaysCounted = Math.Max(1, maxDaysCounted);
        }

        public int Today => clock.Today;

        public bool IsFlownToday => store.LastFlownDay == clock.Today;

        /// <summary>Days in a row so far, counting today only once it is flown; zero once a day has been missed.</summary>
        public int DayStreak
        {
            get
            {
                int last = store.LastFlownDay;
                return last == clock.Today || last == clock.Today - 1 ? store.DayStreak : 0;
            }
        }

        /// <summary>What flying today would pay, given the days in a row it would make.</summary>
        public int RewardForToday => RewardFor(IsFlownToday ? DayStreak : DayStreak + 1);

        /// <summary>Which of <paramref name="flightCount"/> daily boards today flies: they take turns, one a day.</summary>
        public int FlightIndexFor(int flightCount)
        {
            return flightCount <= 0 ? 0 : ((clock.Today % flightCount) + flightCount) % flightCount;
        }

        /// <summary>Records today's flight and returns the coins it pays; nothing when today was already flown.</summary>
        public int Complete(out int dayStreak)
        {
            if (IsFlownToday)
            {
                dayStreak = DayStreak;
                return 0;
            }

            dayStreak = DayStreak + 1;
            store.Save(clock.Today, dayStreak);
            return RewardFor(dayStreak);
        }

        private int RewardFor(int dayStreak)
        {
            return baseCoins + coinsPerDayInRow * (Math.Max(1, Math.Min(dayStreak, maxDaysCounted)) - 1);
        }
    }
}
