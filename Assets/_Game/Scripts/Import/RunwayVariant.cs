using System.Collections.Generic;
using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// Puts the runway on the square the solution ends on: the flight has to land there,
    /// so the player must plan the whole route backwards from it as well as forwards.
    /// </summary>
    public sealed class RunwayVariant : ILevelVariant
    {
        public LevelRule Rule => LevelRule.Runway;

        public LevelData Apply(LevelData level)
        {
            if (!level.HasSolution || level.HasFixedEnd)
            {
                return level;
            }

            int last = level.Solution[level.Solution.Count - 1];
            return level.WithRules(last, ToArray(level.Wind));
        }

        private static WindCell[] ToArray(IReadOnlyList<WindCell> wind)
        {
            var copy = new WindCell[wind.Count];

            for (int i = 0; i < wind.Count; i++)
            {
                copy[i] = wind[i];
            }

            return copy;
        }
    }
}
