using System;
using System.Collections.Generic;
using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// Sets gusts of wind on a few crossroads of the solution, each blowing the way the
    /// solution leaves that square. The solution still flies, but any other route has to
    /// reach those squares at just the right moment, which is what makes the wind bite.
    /// </summary>
    public sealed class WindVariant : ILevelVariant
    {
        /// <summary>Roughly one gust for this many squares.</summary>
        private const int CellsPerGust = 10;

        private readonly int maxGusts;

        public WindVariant(int maxGusts = 3)
        {
            this.maxGusts = Math.Max(1, maxGusts);
        }

        public LevelRule Rule => LevelRule.Wind;

        public LevelData Apply(LevelData level)
        {
            if (!level.HasSolution || level.HasWind || level.Solution.Count < 4)
            {
                return level;
            }

            List<int> candidates = Crossroads(level, 3);

            if (candidates.Count == 0)
            {
                candidates = Crossroads(level, 2);
            }

            int wanted = Math.Min(maxGusts, Math.Max(1, level.ActiveCellCount / CellsPerGust));
            var gusts = new List<WindCell>(wanted);
            IReadOnlyList<int> path = level.Solution;

            // Spread along the route rather than bunched at its start: take candidates at
            // even steps, skipping any square touching a gust already placed.
            for (int pick = 0; pick < candidates.Count && gusts.Count < wanted; pick++)
            {
                int index = candidates[(int)((pick + 0.5) * candidates.Count / Math.Max(1, wanted)) % candidates.Count];
                int cell = path[index];

                if (Touches(level.Grid, gusts, cell))
                {
                    continue;
                }

                gusts.Add(new WindCell(cell, DirectionBetween(level.Grid, cell, path[index + 1])));
            }

            return gusts.Count == 0 ? level : level.WithRules(level.FixedEnd, gusts.ToArray());
        }

        /// <summary>Indices along the solution, past its start and before its end, of squares with many neighbours.</summary>
        private static List<int> Crossroads(LevelData level, int minNeighbours)
        {
            var found = new List<int>();
            IReadOnlyList<int> path = level.Solution;

            for (int i = 1; i < path.Count - 1; i++)
            {
                if (path[i] != level.FixedEnd && NeighbourCount(level, path[i]) >= minNeighbours)
                {
                    found.Add(i);
                }
            }

            return found;
        }

        private static int NeighbourCount(LevelData level, int cell)
        {
            int count = 0;

            for (int direction = 0; direction < Grid.NeighborCount; direction++)
            {
                if (level.Grid.TryGetNeighbor(cell, (Direction)direction, out int neighbour) && level.IsActive(neighbour))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool Touches(Grid grid, List<WindCell> gusts, int cell)
        {
            for (int i = 0; i < gusts.Count; i++)
            {
                if (gusts[i].Cell == cell || grid.AreAdjacent(gusts[i].Cell, cell))
                {
                    return true;
                }
            }

            return false;
        }

        private static Direction DirectionBetween(Grid grid, int from, int to)
        {
            for (int direction = 0; direction < Grid.NeighborCount; direction++)
            {
                if (grid.TryGetNeighbor(from, (Direction)direction, out int neighbour) && neighbour == to)
                {
                    return (Direction)direction;
                }
            }

            throw new ArgumentException("Squares " + from + " and " + to + " are not neighbours.");
        }
    }
}
