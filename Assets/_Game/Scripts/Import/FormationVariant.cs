using System;
using System.Collections.Generic;
using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// Turns a flight into formation flying: the board becomes the player's left half and a
    /// wingman's right half, and the wingman makes the mirror of every move, waiting wherever
    /// its half blocks it (see <see cref="Wingman"/>).
    ///
    /// The wingman's half is drawn, not mirrored: starting from the mirror image, some squares
    /// are knocked out and a few added, then the wingman flies the level's own solution over
    /// that ground. The squares it covers become its half. Every square it was blocked by stays
    /// a hole and every square it entered is on the board, so the solution still covers both
    /// halves, by construction. Several grounds are tried and one where the wingman is held up
    /// along the way is kept, so its half truly differs from the player's.
    /// </summary>
    public sealed class FormationVariant : ILevelVariant
    {
        private const int Attempts = 40;

        private readonly int maxWidth;
        private readonly int maxCells;
        private readonly double holeChance;
        private readonly double extraChance;
        private readonly int minWaits;

        public FormationVariant(int maxWidth = 6, int maxCells = 30, double holeChance = 0.18, double extraChance = 0.2,
            int minWaits = 2)
        {
            this.maxWidth = Math.Max(1, maxWidth);
            this.maxCells = Math.Max(1, maxCells);
            this.holeChance = holeChance;
            this.extraChance = extraChance;
            this.minWaits = Math.Max(1, minWaits);
        }

        public LevelRule Rule => LevelRule.Formation;

        public LevelData Apply(LevelData level)
        {
            Grid source = level.Grid;

            // Mirrored about the last column actually used, so the two halves meet in the middle.
            int usedWidth = UsedWidth(level);

            if (level.IsFormation || !level.HasSolution || !level.HasFixedStart || level.HasFixedEnd || level.HasWind ||
                level.Marks.Count > 0 || usedWidth > maxWidth || level.ActiveCellCount > maxCells)
            {
                return level;
            }

            var doubled = new Grid(usedWidth * 2, source.Height);
            var left = new List<int>(level.ActiveCellCount);

            for (int i = 0; i < level.DeclaredActiveCells.Count; i++)
            {
                left.Add(Widen(source, doubled, level.DeclaredActiveCells[i]));
            }

            var solution = new int[level.Solution.Count];

            for (int i = 0; i < solution.Length; i++)
            {
                solution[i] = Widen(source, doubled, level.Solution[i]);
            }

            int start = Widen(source, doubled, level.FixedStart);
            string[] tags = TagList.With(level.Tags, LevelTags.Formation);
            var random = new Random(StableHash(level.Id));

            for (int attempt = 0; attempt < Attempts; attempt++)
            {
                List<int> ground = DrawGround(doubled, left, Mirror(doubled, start), random);
                var trial = new LevelData(level.Id, level.Version, doubled, Union(left, ground), start, LevelData.NoCell, solution,
                    level.Difficulty, level.ThemeId, tags);

                if (!TryFly(trial, solution, out List<int> covered, out int waits) || waits < minWaits ||
                    covered.Count * 10 < left.Count * 7 || IsPlainMirror(doubled, left, covered))
                {
                    continue;
                }

                return new LevelData(level.Id, level.Version, doubled, Union(left, covered), start, LevelData.NoCell, solution,
                    level.Difficulty, level.ThemeId, tags);
            }

            return level;
        }

        /// <summary>The mirror image of the player's half, with holes knocked in and a few squares added beside it.</summary>
        private List<int> DrawGround(Grid grid, List<int> left, int wingStart, Random random)
        {
            var isGround = new bool[grid.CellCount];

            foreach (int cell in left)
            {
                int mirror = Mirror(grid, cell);
                isGround[mirror] = mirror == wingStart || random.NextDouble() >= holeChance;
            }

            int half = grid.Width / 2;

            for (int cell = 0; cell < grid.CellCount; cell++)
            {
                if (isGround[cell] || grid.ToColumn(cell) < half || !TouchesGround(grid, isGround, cell))
                {
                    continue;
                }

                if (random.NextDouble() < extraChance)
                {
                    isGround[cell] = true;
                }
            }

            var ground = new List<int>();

            for (int cell = 0; cell < grid.CellCount; cell++)
            {
                if (isGround[cell])
                {
                    ground.Add(cell);
                }
            }

            return ground;
        }

        /// <summary>Flies the solution with its wingman; false if the player's route itself breaks.</summary>
        private static bool TryFly(LevelData level, int[] solution, out List<int> covered, out int waits)
        {
            covered = new List<int>();
            waits = 0;
            var visits = new byte[level.Grid.CellCount];
            int wing = Wingman.StartFor(level, solution[0]);
            visits[wing] = 1;
            covered.Add(wing);

            for (int i = 1; i < solution.Length; i++)
            {
                int next = Wingman.Step(level, visits, wing, solution[i - 1], solution[i]);

                if (next == wing)
                {
                    waits++;
                    continue;
                }

                visits[next] = 1;
                covered.Add(next);
                wing = next;
            }

            return true;
        }

        private static bool IsPlainMirror(Grid grid, List<int> left, List<int> covered)
        {
            if (covered.Count != left.Count)
            {
                return false;
            }

            var mirrors = new HashSet<int>();

            foreach (int cell in left)
            {
                mirrors.Add(Mirror(grid, cell));
            }

            foreach (int cell in covered)
            {
                if (!mirrors.Contains(cell))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TouchesGround(Grid grid, bool[] isGround, int cell)
        {
            for (int direction = 0; direction < Grid.NeighborCount; direction++)
            {
                if (grid.TryGetNeighbor(cell, (Direction)direction, out int neighbour) && isGround[neighbour])
                {
                    return true;
                }
            }

            return false;
        }

        private static int[] Union(List<int> left, List<int> right)
        {
            var cells = new int[left.Count + right.Count];
            left.CopyTo(cells, 0);
            right.CopyTo(cells, left.Count);
            return cells;
        }

        private static int Mirror(Grid grid, int cell)
        {
            return grid.ToIndex(grid.ToRow(cell), grid.Width - 1 - grid.ToColumn(cell));
        }

        private static int UsedWidth(LevelData level)
        {
            int last = 0;

            for (int i = 0; i < level.DeclaredActiveCells.Count; i++)
            {
                last = Math.Max(last, level.Grid.ToColumn(level.DeclaredActiveCells[i]));
            }

            return last + 1;
        }

        /// <summary>The same square on the left half of the doubled board.</summary>
        private static int Widen(Grid source, Grid doubled, int cell)
        {
            return doubled.ToIndex(source.ToRow(cell), source.ToColumn(cell));
        }

        private static int StableHash(string text)
        {
            unchecked
            {
                int hash = 17;

                for (int i = 0; i < text.Length; i++)
                {
                    hash = hash * 31 + text[i];
                }

                return hash & 0x7FFFFFFF;
            }
        }
    }
}
