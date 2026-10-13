namespace ASTeams.SingleLine.Core
{
    /// <summary>A square carrying a <see cref="CellKind"/> other than plain.</summary>
    public readonly struct CellMark
    {
        public int Cell { get; }

        public CellKind Kind { get; }

        public CellMark(int cell, CellKind kind)
        {
            Cell = cell;
            Kind = kind;
        }

        public override string ToString()
        {
            return Cell + ":" + Kind;
        }
    }
}
