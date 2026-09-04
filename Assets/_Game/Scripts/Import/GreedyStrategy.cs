namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// Deterministic stand-ins for the ways a person actually plays. Difficulty is
    /// measured by how many of them walk into a dead end, which is a far better proxy
    /// for a level being hard than counting its cells.
    ///
    /// No randomness anywhere: the same level must score the same on every import, or
    /// chapter contents would drift between builds.
    /// </summary>
    public enum GreedyStrategy
    {
        /// <summary>Always takes the first legal direction, in the order up, right, down, left.</summary>
        PreferUp = 0,

        /// <summary>Same, starting from right.</summary>
        PreferRight = 1,

        /// <summary>Same, starting from down.</summary>
        PreferDown = 2,

        /// <summary>Same, starting from left.</summary>
        PreferLeft = 3,

        /// <summary>
        /// Heads for the cell with the fewest onward moves. This is what a careful player
        /// learns to do: clear the corners before they get stranded.
        /// </summary>
        FewestOnwardMoves = 4,

        /// <summary>
        /// Heads for the most open cell, which is the classic beginner mistake. A level
        /// that survives this is genuinely forgiving.
        /// </summary>
        MostOnwardMoves = 5
    }
}
