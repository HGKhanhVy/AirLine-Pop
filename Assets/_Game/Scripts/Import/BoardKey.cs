using System;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// Identity of a board shape that is stable under the eight symmetries of a
    /// rectangle. Two levels share a key exactly when one is a rotation or a mirror of
    /// the other, which is how the ripped corpus hides 225 duplicates.
    ///
    /// The key is kept as text rather than a bitmask so the editor tool can print it
    /// when explaining why two levels were merged, and so boards larger than a machine
    /// word need no special case.
    /// </summary>
    public readonly struct BoardKey : IEquatable<BoardKey>
    {
        private readonly string value;

        /// <summary>Width after the board was turned into its canonical orientation.</summary>
        public int Width { get; }

        /// <summary>Height after the board was turned into its canonical orientation.</summary>
        public int Height { get; }

        public string Value => value ?? string.Empty;

        public bool IsEmpty => string.IsNullOrEmpty(value);

        public BoardKey(string value, int width, int height)
        {
            this.value = value;
            Width = width;
            Height = height;
        }

        public bool Equals(BoardKey other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is BoardKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
