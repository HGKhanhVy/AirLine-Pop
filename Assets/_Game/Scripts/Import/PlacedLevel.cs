using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// A level in its slot, together with the target the curve asked for and the score
    /// the chosen level actually had.
    ///
    /// The 1 to 10 difficulty GDD 6.3 requires is far too coarse to see the rhythm in:
    /// one band spans roughly one chapter, so every level in a chapter carries almost
    /// the same number. The rest and peak beats live in <see cref="Score"/>, which is
    /// what the acceptance checks and the editor preview read.
    /// </summary>
    public readonly struct PlacedLevel
    {
        public LevelData Level { get; }

        /// <summary>Percentile difficulty of the level that was placed, 0 to 1.</summary>
        public double Score { get; }

        /// <summary>What the curve asked for at this slot, 0 to 1.</summary>
        public double Target { get; }

        /// <summary>How far the pool forced the curve to bend here.</summary>
        public double Miss => Score - Target;

        public PlacedLevel(LevelData level, double score, double target)
        {
            Level = level;
            Score = score;
            Target = target;
        }

        public override string ToString()
        {
            return Level.Id + " score " + Score.ToString("0.000") + " target " + Target.ToString("0.000");
        }
    }
}
