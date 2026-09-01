namespace ASTeams.SingleLine.Core
{
    /// <summary>
    /// Gameplay states of a single level attempt (GDD 3.5).
    /// Loading, Resolving, Paused and Error are presentation-layer concerns and
    /// deliberately live outside the pure rules core.
    /// </summary>
    public enum PathState
    {
        /// <summary>Board is shown, no cell has been drawn yet.</summary>
        Ready = 0,

        /// <summary>The path holds at least one cell and legal moves remain.</summary>
        Drawing = 1,

        /// <summary>Cells remain uncovered but the head has no legal neighbour left.</summary>
        Stuck = 2,

        /// <summary>Every active cell is covered and the end constraint is satisfied.</summary>
        Won = 3
    }
}
