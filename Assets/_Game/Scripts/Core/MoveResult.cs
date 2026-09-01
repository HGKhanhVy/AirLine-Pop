namespace ASTeams.SingleLine.Core
{
    /// <summary>
    /// Outcome of a single input step. Rejected is a normal, non-destructive
    /// outcome: rule MOV-06 requires an illegal drag to be ignored, never to
    /// reset the path.
    /// </summary>
    public enum MoveResult
    {
        Rejected = 0,
        Started = 1,
        Moved = 2,
        Backtracked = 3,
        Completed = 4
    }
}
