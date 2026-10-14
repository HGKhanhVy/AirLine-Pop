using System;
using System.Collections.Generic;
using AirlinePop.SingleLine.Core;

namespace AirlinePop.SingleLine.Import
{
    /// <summary>
    /// Lays out the daily flights from the boards the campaign left unused: a solid
    /// mid-route challenge each day, the conditions rotating so two days in a row never feel
    /// the same, and every seventh day a little tougher to round off the week.
    /// </summary>
    public sealed class DailyFlightPlanner
    {
        public const string ChapterId = "daily";

        private static readonly LevelRule[] Rotation =
        {
            LevelRule.Runway, LevelRule.Turbulence, LevelRule.Wind, LevelRule.Beacon, LevelRule.None,
            LevelRule.Runway | LevelRule.Wind, LevelRule.Turbulence | LevelRule.Beacon
        };

        private readonly int dayCount;
        private readonly FlightTarget everyday;
        private readonly FlightTarget weekend;

        public DailyFlightPlanner(int dayCount = 60)
            : this(dayCount, new FlightTarget(24, 2.0), new FlightTarget(28, 2.8))
        {
        }

        public DailyFlightPlanner(int dayCount, FlightTarget everyday, FlightTarget weekend)
        {
            this.dayCount = Math.Max(1, dayCount);
            this.everyday = everyday;
            this.weekend = weekend;
        }

        public List<LevelData> Plan(BoardPicker picker)
        {
            if (picker == null)
            {
                throw new ArgumentNullException(nameof(picker));
            }

            var days = new List<LevelData>(dayCount);

            for (int day = 0; day < dayCount; day++)
            {
                LevelRule rules = Rotation[day % Rotation.Length];
                FlightTarget target = day % 7 == 6 ? weekend : everyday;

                if (!picker.TryPick(target, new[] { rules, LevelRule.None }, false, null, out PickedBoard picked))
                {
                    break;
                }

                LevelData level = picked.Level.WithIdentity(ChapterId + "_" + (day + 1).ToString("000"), 5);
                BoardPicker.Verify(level);
                days.Add(level);
            }

            return days;
        }
    }
}
