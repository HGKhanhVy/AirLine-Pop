namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// Why a level file could not be turned into a <see cref="ASTeams.SingleLine.Core.LevelData"/>.
    /// Only structural failures live here; a level that parses but is unsolvable or has
    /// a broken solution is reported by the validator instead.
    /// </summary>
    public enum LevelParseError
    {
        None = 0,

        /// <summary>The file is empty or contains only separators.</summary>
        EmptyFile = 1,

        /// <summary>A token is not an integer.</summary>
        NonNumericToken = 2,

        /// <summary>Fewer than three numbers, so there is not even a header plus one cell.</summary>
        TooFewValues = 3,

        /// <summary>Width or height is zero or negative.</summary>
        InvalidGridSize = 4,

        /// <summary>
        /// A cell index falls outside the grid the header declares. In the ripped corpus
        /// this always means a wrong header rather than a wrong cell list.
        /// </summary>
        CellOutsideGrid = 5,

        /// <summary>The payload form could not be determined because no cell remains.</summary>
        NoActiveCells = 6
    }
}
