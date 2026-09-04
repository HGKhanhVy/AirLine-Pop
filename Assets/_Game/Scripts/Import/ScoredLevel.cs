using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// A level together with why it scored what it did. The chapter assembler reads
    /// <see cref="Difficulty"/>; the editor tool shows the rest when a level looks
    /// misplaced.
    /// </summary>
    public sealed class ScoredLevel
    {
        public LevelData Level { get; }

        public LevelFeatures Features { get; }

        /// <summary>Weighted blend of the percentile ranked signals, from 0 to 1.</summary>
        public double RawScore { get; }

        /// <summary>The 1 to 10 value GDD 6.3 requires, taken from the decile of the raw score.</summary>
        public int Difficulty { get; }

        public ScoredLevel(LevelData level, LevelFeatures features, double rawScore, int difficulty)
        {
            Level = level;
            Features = features;
            RawScore = rawScore;
            Difficulty = difficulty;
        }

        public override string ToString()
        {
            return Level.Id + " -> " + Difficulty + " (" + Features + ")";
        }
    }
}
