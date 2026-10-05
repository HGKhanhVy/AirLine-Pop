namespace ASTeams.SingleLine.Core
{
    /// <summary>
    /// A square with a gust of wind on it: a plane that flies onto it must leave it in
    /// the wind's direction.
    /// </summary>
    public readonly struct WindCell
    {
        public int Cell { get; }

        public Direction Direction { get; }

        public WindCell(int cell, Direction direction)
        {
            Cell = cell;
            Direction = direction;
        }

        public override string ToString()
        {
            return Cell + "→" + Direction;
        }
    }
}
