using System;
using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// Makes a flight a night flight: the route is shown once, then the board goes dark and
    /// the player flies it from memory. Only routes short enough to remember take it; in a
    /// formation flight that is the player's own half.
    /// </summary>
    public sealed class NightVariant : ILevelVariant
    {
        private readonly int maxCells;

        public NightVariant(int maxCells = 25)
        {
            this.maxCells = Math.Max(1, maxCells);
        }

        public LevelRule Rule => LevelRule.Night;

        public LevelData Apply(LevelData level)
        {
            if (level.IsNight || !level.HasSolution || level.PathLength > maxCells)
            {
                return level;
            }

            return level.WithTags(TagList.With(level.Tags, LevelTags.Night));
        }
    }
}
