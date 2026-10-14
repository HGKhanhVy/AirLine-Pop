namespace AirlinePop.SingleLine.Core
{
    /// <summary>
    /// A square carrying a <see cref="CellKind"/> other than plain. A beacon also carries the
    /// direction its arrow points; the other kinds ignore it.
    /// </summary>
    public readonly struct CellMark
    {
        public int Cell { get; }

        public CellKind Kind { get; }

        public Direction Direction { get; }

        public CellMark(int cell, CellKind kind, Direction direction = Direction.Up)
        {
            Cell = cell;
            Kind = kind;
            Direction = direction;
        }

        public override string ToString()
        {
            return Kind == CellKind.Beacon ? Cell + ":" + Kind + "->" + Direction : Cell + ":" + Kind;
        }
    }
}
