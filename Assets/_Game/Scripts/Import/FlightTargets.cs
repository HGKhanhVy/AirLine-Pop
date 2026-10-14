using System;

namespace AirlinePop.SingleLine.Import
{
    /// <summary>
    /// What each flight of the campaign should feel like, on an absolute scale: how long the
    /// route is and how many wrong turns a <see cref="ThoughtfulPlayer"/> takes on it. Both rise
    /// along the route, fast at first so the opening chapters stop being a formality, then
    /// more gently. Within a city the climbs step up, rests drop back, and the storm stands
    /// well above everything around it.
    /// </summary>
    public sealed class FlightTargets
    {
        public static readonly FlightTargets Default = new FlightTargets(10, 410, 12, 38, 0.6, 1.05, 4.0, 0.8);

        // Climbs within a city lean further into the trend the later they come.
        private static readonly double[] ClimbSteps = { 0.7, 0.8, 0.9, 1.0, 1.1, 1.2 };

        private readonly int firstLevel;
        private readonly int lastLevel;
        private readonly double startCells;
        private readonly double endCells;
        private readonly double cellsPower;
        private readonly double startTrials;
        private readonly double endTrials;
        private readonly double trialsPower;

        public FlightTargets(int onboardingCount, int lastLevel, double startCells, double endCells, double cellsPower,
            double startTrials, double endTrials, double trialsPower)
        {
            firstLevel = onboardingCount + 1;
            this.lastLevel = lastLevel;
            this.startCells = startCells;
            this.endCells = endCells;
            this.cellsPower = cellsPower;
            this.startTrials = startTrials;
            this.endTrials = endTrials;
            this.trialsPower = trialsPower;
        }

        /// <summary>The route length and the wrong turns a flight should have.</summary>
        public FlightTarget For(int levelNumber, FlightRole role, int slotInCity)
        {
            double progress = Math.Max(0, Math.Min(1, (levelNumber - firstLevel) / (double)(lastLevel - firstLevel)));
            double cells = startCells + (endCells - startCells) * Math.Pow(progress, cellsPower);
            double extraTrials = (startTrials - 1) + (endTrials - startTrials) * Math.Pow(progress, trialsPower);

            switch (role)
            {
                case FlightRole.Rest:
                    return new FlightTarget(cells * 0.8, 1 + extraTrials * 0.4);
                case FlightRole.Storm:
                    return new FlightTarget(cells * 1.1, 1 + extraTrials * 2.2);
                case FlightRole.Landing:
                    return new FlightTarget(cells, 1 + extraTrials * 1.3);
                default:
                    return new FlightTarget(cells, 1 + extraTrials * ClimbSteps[ClimbIndex(slotInCity)]);
            }
        }

        private static int ClimbIndex(int slot)
        {
            int index = slot < 3 ? slot : slot < 6 ? slot - 1 : slot - 2;
            return Math.Max(0, Math.Min(ClimbSteps.Length - 1, index));
        }
    }
}
