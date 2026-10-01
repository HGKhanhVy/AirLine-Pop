using System;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// One swatch of the CubeAnimals colour atlas: a 16 x 14 grid where every model face
    /// takes its colour from the swatch its UVs fall in. Rows count from the top of the
    /// image, matching how the atlas reads when opened.
    /// </summary>
    [Serializable]
    public struct PaletteCell : IEquatable<PaletteCell>
    {
        public const int Columns = 16;
        public const int Rows = 14;

        [SerializeField, Range(0, Columns - 1)] private int column;
        [SerializeField, Range(0, Rows - 1)] private int row;

        public PaletteCell(int column, int row)
        {
            this.column = column;
            this.row = row;
        }

        public int Column => column;

        public int Row => row;

        /// <summary>The swatch a UV coordinate samples.</summary>
        public static PaletteCell FromUv(Vector2 uv)
        {
            int c = Mathf.Clamp(Mathf.FloorToInt(uv.x * Columns), 0, Columns - 1);
            int r = Mathf.Clamp(Mathf.FloorToInt((1f - uv.y) * Rows), 0, Rows - 1);
            return new PaletteCell(c, r);
        }

        /// <summary>
        /// The UV shift that carries a point from this swatch to <paramref name="target"/>
        /// while keeping its place inside the swatch, so gradient swatches stay gradients.
        /// </summary>
        public Vector2 OffsetTo(PaletteCell target)
        {
            return new Vector2(
                (target.column - column) / (float)Columns,
                -(target.row - row) / (float)Rows);
        }

        /// <summary>UV at the middle of the swatch, for generated meshes that want a flat colour.</summary>
        public Vector2 CenterUv => new Vector2((column + 0.5f) / Columns, 1f - (row + 0.5f) / Rows);

        public bool Equals(PaletteCell other)
        {
            return column == other.column && row == other.row;
        }

        public override bool Equals(object obj)
        {
            return obj is PaletteCell other && Equals(other);
        }

        public override int GetHashCode()
        {
            return column * Rows + row;
        }
    }
}
