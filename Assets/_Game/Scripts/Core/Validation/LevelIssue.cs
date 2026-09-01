namespace ASTeams.SingleLine.Core
{
    /// <summary>
    /// One validation failure, optionally pinned to the cell that caused it so the
    /// level editor can highlight it directly on the board.
    /// </summary>
    public readonly struct LevelIssue
    {
        public LevelIssueCode Code { get; }

        /// <summary>Offending cell, or <see cref="LevelData.NoCell"/> when board wide.</summary>
        public int Cell { get; }

        public LevelIssue(LevelIssueCode code, int cell = LevelData.NoCell)
        {
            Code = code;
            Cell = cell;
        }

        public override string ToString()
        {
            return Cell == LevelData.NoCell ? Code.ToString() : Code + " at cell " + Cell;
        }
    }
}
