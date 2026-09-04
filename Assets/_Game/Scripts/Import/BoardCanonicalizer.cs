using System;
using System.Collections.Generic;
using System.Text;
using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// Reduces a board to a single identity shared by all eight of its symmetries, by
    /// generating every rotation and mirror and keeping the smallest rendering.
    ///
    /// The start cell is deliberately not part of the identity. Two levels with the same
    /// shape but different start dots are different puzzles, yet they look identical on
    /// the level select screen, and shipping both reads as padding.
    ///
    /// Buffers are reused across calls, so canonicalising a thousand levels allocates
    /// only the keys it returns.
    /// </summary>
    public sealed class BoardCanonicalizer
    {
        /// <summary>Four rotations, each optionally mirrored first.</summary>
        public const int SymmetryCount = 8;

        private readonly List<int> transformed = new List<int>(128);
        private readonly StringBuilder builder = new StringBuilder(512);

        public BoardKey GetKey(LevelData level)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            return GetKey(level.Grid, level.DeclaredActiveCells);
        }

        public BoardKey GetKey(Grid grid, IReadOnlyList<int> activeCells)
        {
            if (activeCells == null)
            {
                throw new ArgumentNullException(nameof(activeCells));
            }

            string best = null;
            int bestWidth = 0;
            int bestHeight = 0;

            for (int rotations = 0; rotations < 4; rotations++)
            {
                for (int mirrorPass = 0; mirrorPass < 2; mirrorPass++)
                {
                    bool mirror = mirrorPass == 1;
                    Transform(grid, activeCells, mirror, rotations, out int width, out int height);

                    string candidate = Render(width, height);

                    if (best == null || string.CompareOrdinal(candidate, best) < 0)
                    {
                        best = candidate;
                        bestWidth = width;
                        bestHeight = height;
                    }
                }
            }

            return new BoardKey(best, bestWidth, bestHeight);
        }

        /// <summary>
        /// Maps every cell through an optional mirror and then a number of quarter turns.
        /// A quarter turn sends (row, column) to (column, height - 1 - row) and swaps the
        /// board dimensions, so the dimensions have to be tracked alongside the cells.
        /// </summary>
        private void Transform(Grid grid, IReadOnlyList<int> cells, bool mirror, int rotations, out int width, out int height)
        {
            width = grid.Width;
            height = grid.Height;

            if ((rotations & 1) == 1)
            {
                width = grid.Height;
                height = grid.Width;
            }

            transformed.Clear();

            for (int i = 0; i < cells.Count; i++)
            {
                int cell = cells[i];
                int row = cell / grid.Width;
                int column = cell % grid.Width;
                int currentWidth = grid.Width;
                int currentHeight = grid.Height;

                if (mirror)
                {
                    column = currentWidth - 1 - column;
                }

                for (int turn = 0; turn < rotations; turn++)
                {
                    int nextRow = column;
                    int nextColumn = currentHeight - 1 - row;
                    row = nextRow;
                    column = nextColumn;

                    int swap = currentWidth;
                    currentWidth = currentHeight;
                    currentHeight = swap;
                }

                transformed.Add(row * currentWidth + column);
            }

            transformed.Sort();
        }

        private string Render(int width, int height)
        {
            builder.Length = 0;
            builder.Append(width);
            builder.Append('x');
            builder.Append(height);
            builder.Append(':');

            int previous = -1;

            for (int i = 0; i < transformed.Count; i++)
            {
                int cell = transformed[i];

                // A duplicated index in the source file must not change the identity.
                if (cell == previous)
                {
                    continue;
                }

                if (previous >= 0)
                {
                    builder.Append(',');
                }

                builder.Append(cell);
                previous = cell;
            }

            return builder.ToString();
        }
    }
}
