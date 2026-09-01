namespace ASTeams.SingleLine.Core
{
    /// <summary>
    /// The four orthogonal step directions (rule MOV-01 forbids diagonals).
    /// Values are contiguous from zero so callers can iterate them with a plain
    /// int loop and cast, avoiding the allocation of Enum.GetValues.
    /// </summary>
    public enum Direction
    {
        Up = 0,
        Right = 1,
        Down = 2,
        Left = 3
    }
}
