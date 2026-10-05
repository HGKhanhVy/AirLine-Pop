using System;
using System.Collections.Generic;
using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// Turns a flight into formation flying: the board is set beside its own mirror image
    /// and a wingman flies the mirror of the player's route. The two planes can never cross
    /// the middle without meeting, so the original board and its solution stay the left
    /// half, which keeps the level solvable by construction. Narrow, small boards only, so
    /// the doubled board still fits the screen.
    /// </summary>
    public sealed class FormationVariant : ILevelVariant
    {
        private readonly int maxWidth;
        private readonly int maxCells;

        public FormationVariant(int maxWidth = 6, int maxCells = 30)
        {
            this.maxWidth = Math.Max(1, maxWidth);
            this.maxCells = Math.Max(1, maxCells);
        }

        public LevelRule Rule => LevelRule.Formation;

        public LevelData Apply(LevelData level)
        {
            Grid source = level.Grid;

            // Mirrored about the last column actually used, so the two halves always touch
            // in the middle and the board stays one piece.
            int usedWidth = UsedWidth(level);

            if (level.IsFormation || !level.HasSolution || level.HasFixedEnd || level.HasWind ||
                usedWidth > maxWidth || level.ActiveCellCount > maxCells)
            {
                return level;
            }

            var doubled = new Grid(usedWidth * 2, source.Height);
            var active = new List<int>(level.ActiveCellCount * 2);

            for (int i = 0; i < level.DeclaredActiveCells.Count; i++)
            {
                int left = Widen(source, doubled, level.DeclaredActiveCells[i]);
                active.Add(left);
                active.Add(doubled.ToIndex(doubled.ToRow(left), doubled.Width - 1 - doubled.ToColumn(left)));
            }

            var solution = new int[level.Solution.Count];

            for (int i = 0; i < solution.Length; i++)
            {
                solution[i] = Widen(source, doubled, level.Solution[i]);
            }

            int start = level.HasFixedStart ? Widen(source, doubled, level.FixedStart) : LevelData.NoCell;
            return new LevelData(level.Id, level.Version, doubled, active.ToArray(), start, LevelData.NoCell, solution,
                level.Difficulty, level.ThemeId, TagList.With(level.Tags, LevelTags.Formation));
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
    }
}
