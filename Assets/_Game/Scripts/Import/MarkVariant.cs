using System;
using System.Collections.Generic;
using AirlinePop.SingleLine.Core;

namespace AirlinePop.SingleLine.Import
{
    /// <summary>
    /// Puts turbulence clouds on squares where the solution flies straight on, or beacons
    /// pointing the way the solution leaves the square, so the solution still flies. Squares
    /// open on three or four sides are chosen first: there the mark rules out real
    /// alternatives, while on a corridor it would only repeat what the board already says.
    /// A beacon is a one-way passage, flown in from its tail and out along its arrow, so it goes
    /// where the solution flies straight through, pointing the way the solution travels.
    /// </summary>
    public sealed class MarkVariant : ILevelVariant
    {
        /// <summary>Roughly one mark for this many squares.</summary>
        private const int CellsPerMark = 7;

        private readonly CellKind kind;
        private readonly int maxMarks;

        public MarkVariant(CellKind kind, int maxMarks = 5)
        {
            if (kind != CellKind.Straight && kind != CellKind.Beacon)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Only clouds and beacons are laid onto a solution.");
            }

            this.kind = kind;
            this.maxMarks = Math.Max(1, maxMarks);
        }

        public LevelRule Rule => kind == CellKind.Straight ? LevelRule.Turbulence : LevelRule.Beacon;

        public LevelData Apply(LevelData level)
        {
            if (!level.HasSolution || level.IsFormation || (level.Rules & Rule) != 0 || level.Solution.Count < 5)
            {
                return level;
            }

            List<int> candidates = Candidates(level, 4);

            if (candidates.Count < 2)
            {
                candidates.AddRange(Candidates(level, 3));
            }

            int wanted = Math.Min(maxMarks, Math.Max(2, level.ActiveCellCount / CellsPerMark));
            var marks = new List<CellMark>(level.Marks);
            int added = 0;

            // Spread along the route, skipping squares next to a mark already placed so the
            // marks read as separate obstacles rather than one clump.
            for (int pick = 0; pick < candidates.Count && added < wanted; pick++)
            {
                int cell = candidates[(int)((pick + 0.5) * candidates.Count / Math.Max(1, wanted)) % candidates.Count];

                if (Touches(level, marks, cell))
                {
                    continue;
                }

                marks.Add(new CellMark(cell, kind, ExitOf(level, cell)));
                added++;
            }

            return added == 0 ? level : level.WithMarks(marks.ToArray());
        }

        /// <summary>Squares inside the route, with exactly this many open sides, where the solution does what the mark asks.</summary>
        private List<int> Candidates(LevelData level, int sides)
        {
            var found = new List<int>();
            IReadOnlyList<int> path = level.Solution;

            for (int i = 1; i < path.Count - 1; i++)
            {
                int cell = path[i];

                if (cell == level.FixedEnd || level.KindOf(cell) != CellKind.Plain || level.TryGetWind(cell, out _) ||
                    OpenSides(level, cell) != sides || found.Contains(cell))
                {
                    continue;
                }

                bool isStraight = path[i + 1] - cell == cell - path[i - 1];

                if (isStraight)
                {
                    found.Add(cell);
                }
            }

            return found;
        }

        /// <summary>The way the solution leaves the square, which a beacon on it points.</summary>
        private static Direction ExitOf(LevelData level, int cell)
        {
            IReadOnlyList<int> path = level.Solution;

            for (int i = 0; i < path.Count - 1; i++)
            {
                if (path[i] != cell)
                {
                    continue;
                }

                for (int direction = 0; direction < Grid.NeighborCount; direction++)
                {
                    if (level.Grid.TryGetNeighbor(cell, (Direction)direction, out int next) && next == path[i + 1])
                    {
                        return (Direction)direction;
                    }
                }
            }

            return Direction.Up;
        }

        private static int OpenSides(LevelData level, int cell)
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

        private static bool Touches(LevelData level, List<CellMark> marks, int cell)
        {
            for (int i = 0; i < marks.Count; i++)
            {
                if (marks[i].Cell == cell || level.Grid.AreAdjacent(marks[i].Cell, cell))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
