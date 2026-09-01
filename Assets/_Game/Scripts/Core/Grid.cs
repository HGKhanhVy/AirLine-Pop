using System;

namespace ASTeams.SingleLine.Core
{
    /// <summary>
    /// Row-major board geometry. Index 0 is the top-left cell, index increases
    /// left to right then top to bottom, matching the level data files.
    /// </summary>
    public readonly struct Grid : IEquatable<Grid>
    {
        public int Width { get; }

        public int Height { get; }

        public int CellCount => Width * Height;

        public Grid(int width, int height)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be positive.");
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be positive.");
            }

            Width = width;
            Height = height;
        }

        public bool Contains(int index)
        {
            return index >= 0 && index < CellCount;
        }

        public int ToIndex(int row, int column)
        {
            if (row < 0 || row >= Height)
            {
                throw new ArgumentOutOfRangeException(nameof(row), row, "Row is outside the grid.");
            }

            if (column < 0 || column >= Width)
            {
                throw new ArgumentOutOfRangeException(nameof(column), column, "Column is outside the grid.");
            }

            return row * Width + column;
        }

        public int ToRow(int index)
        {
            ThrowIfOutside(index);
            return index / Width;
        }

        public int ToColumn(int index)
        {
            ThrowIfOutside(index);
            return index % Width;
        }

        /// <summary>
        /// Orthogonal adjacency only. Diagonals are never adjacent (rule MOV-01).
        /// </summary>
        public bool AreAdjacent(int a, int b)
        {
            if (!Contains(a) || !Contains(b) || a == b)
            {
                return false;
            }

            int rowA = a / Width;
            int rowB = b / Width;
            int columnA = a % Width;
            int columnB = b % Width;

            if (rowA == rowB)
            {
                return columnA - columnB == 1 || columnB - columnA == 1;
            }

            if (columnA == columnB)
            {
                return rowA - rowB == 1 || rowB - rowA == 1;
            }

            return false;
        }

        /// <summary>
        /// Number of orthogonal directions. Callers iterate 0..NeighborCount-1 and
        /// cast to <see cref="Direction"/> to walk every neighbour without allocating.
        /// </summary>
        public const int NeighborCount = 4;

        /// <summary>
        /// Resolves the neighbour of <paramref name="index"/> in the given direction.
        /// Returns false when the step would leave the board, so wrap-around across a
        /// row edge can never be mistaken for a legal move.
        /// </summary>
        public bool TryGetNeighbor(int index, Direction direction, out int neighbor)
        {
            neighbor = -1;

            if (!Contains(index))
            {
                return false;
            }

            int row = index / Width;
            int column = index % Width;

            switch (direction)
            {
                case Direction.Up:
                    if (row == 0)
                    {
                        return false;
                    }

                    neighbor = index - Width;
                    return true;

                case Direction.Right:
                    if (column == Width - 1)
                    {
                        return false;
                    }

                    neighbor = index + 1;
                    return true;

                case Direction.Down:
                    if (row == Height - 1)
                    {
                        return false;
                    }

                    neighbor = index + Width;
                    return true;

                case Direction.Left:
                    if (column == 0)
                    {
                        return false;
                    }

                    neighbor = index - 1;
                    return true;

                default:
                    return false;
            }
        }

        private void ThrowIfOutside(int index)
        {
            if (!Contains(index))
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Index is outside the grid.");
            }
        }

        public bool Equals(Grid other)
        {
            return Width == other.Width && Height == other.Height;
        }

        public override bool Equals(object obj)
        {
            return obj is Grid other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (Width * 397) ^ Height;
        }

        public override string ToString()
        {
            return $"{Width}x{Height}";
        }
    }
}
