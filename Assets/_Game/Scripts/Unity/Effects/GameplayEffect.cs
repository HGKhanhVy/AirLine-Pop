namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The moments on the board that are worth a particle. Naming the moment rather than
    /// the asset is what lets the reference game's effects be swapped, retimed or turned
    /// off per theme without a line of gameplay code changing.
    /// </summary>
    public enum GameplayEffect
    {
        /// <summary>The path just stepped onto a square.</summary>
        CellConnected = 0,

        /// <summary>A move was refused, at a wall or at a square already covered.</summary>
        CellRejected = 1,

        /// <summary>The board is complete. Fired once, at the last square.</summary>
        PathCompleted = 2,

        /// <summary>A level opened and its start square is waiting to be touched.</summary>
        StartCue = 3
    }
}
