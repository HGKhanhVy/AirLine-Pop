namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// The three layouts the reference game used for the numbers after the width and
    /// height header. Counted across the whole ripped corpus of 1059 files.
    /// </summary>
    public enum PayloadForm
    {
        /// <summary>
        /// Active cells in ascending order, then the start cell repeated. 662 files.
        /// </summary>
        SetWithStart = 0,

        /// <summary>
        /// Active cells in ascending order and nothing else, so the start is only
        /// recoverable from the tutorial file. 3 files.
        /// </summary>
        SetOnly = 1,

        /// <summary>
        /// The payload is itself a walk over the board, so the first entry is the start
        /// and the whole sequence is a valid solution. 394 files.
        /// </summary>
        Path = 2
    }
}
