using System;

namespace ASTeams.SingleLine.Import
{
    /// <summary>A route length and a count of wrong turns per square, see <see cref="FlightTargets"/>.</summary>
    public readonly struct FlightTarget
    {
        /// <summary>How much more a miss on wrong turns counts than a miss on length.</summary>
        private const double TrialsWeight = 1.5;

        public double Cells { get; }

        public double Trials { get; }

        public FlightTarget(double cells, double trials)
        {
            Cells = Math.Max(1, cells);
            Trials = Math.Max(1, trials);
        }

        /// <summary>How far a board sits from this target, comparing ratios so small and large boards weigh alike.</summary>
        public double DistanceTo(int cells, double trials)
        {
            return Math.Abs(Math.Log(Math.Max(1, cells) / Cells)) + TrialsWeight * Math.Abs(Math.Log(Math.Max(1, trials) / Trials));
        }

        public override string ToString()
        {
            return Cells.ToString("F0") + " cells x" + Trials.ToString("F2");
        }
    }
}
