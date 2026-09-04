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

        /// <summary>
        /// Where this level sits among the whole corpus, from 0 to 1, and the number the
        /// chapter assembler places by.
        ///
        /// It is the percentile rank of the blended signals rather than the blend itself.
        /// Averaging four uniform rankings does not stay uniform: the tails compress, so
        /// the hardest level in the corpus blends to about 0.88 and never reaches 1. An
        /// assembler asking for a level at 0.95 would then find nothing and quietly bend
        /// its curve. Ranking once more restores an even spread across the range.
        /// </summary>
        public double Score { get; }

        /// <summary>The 1 to 10 value GDD 6.3 requires, taken from the decile of the raw score.</summary>
        public int Difficulty { get; }

        public ScoredLevel(LevelData level, LevelFeatures features, double score, int difficulty)
        {
            Level = level;
            Features = features;
            Score = score;
            Difficulty = difficulty;
        }

        public override string ToString()
        {
            return Level.Id + " -> " + Difficulty + " (" + Features + ")";
        }
    }
}
