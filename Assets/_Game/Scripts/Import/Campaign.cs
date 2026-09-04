using System.Collections.Generic;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// The whole ordered set of chapters an import run produced, ready to be exported.
    /// </summary>
    public sealed class Campaign
    {
        private readonly List<Chapter> chapters;

        public IReadOnlyList<Chapter> Chapters => chapters;

        public int LevelCount { get; }

        /// <summary>
        /// Levels the assembler had to place while ignoring the variety rules because no
        /// candidate satisfied them. A healthy import reports zero; anything else means
        /// the pool is too thin somewhere on the curve.
        /// </summary>
        public int RelaxedPlacements { get; }

        public Campaign(List<Chapter> chapters, int levelCount, int relaxedPlacements)
        {
            this.chapters = chapters;
            LevelCount = levelCount;
            RelaxedPlacements = relaxedPlacements;
        }
    }
}
