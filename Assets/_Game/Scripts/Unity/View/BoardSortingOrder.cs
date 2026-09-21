namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The order the board's world renderers draw in.
    ///
    /// The connector runs *over* the squares. The reference game draws the path as a thick
    /// pipe in a lighter tint of the level's own colour, visibly crossing the middle of
    /// every square it has covered, not just the gaps between them.
    ///
    /// A cell is no longer one sprite either: it reuses the reference block art, whose own
    /// children sort from 0 to 10 among themselves, so anything meant to sit above a square
    /// has to clear that whole range.
    /// </summary>
    public static class BoardSortingOrder
    {
        public const int Cells = 0;

        /// <summary>Highest order used inside the cell prefab's own art.</summary>
        public const int CellArtCeiling = 10;

        /// <summary>The pipe joining the squares, drawn across their faces.</summary>
        public const int Connector = 15;

        /// <summary>
        /// The white sheet a square washes over itself when the path lands on it. Above the
        /// pipe, because it is meant to read as light on the surface rather than under it.
        /// </summary>
        public const int ConnectFlash = 17;

        /// <summary>
        /// The start and end dots. They sit above the pipe: the pipe runs through the
        /// middle of a square and would otherwise bury the very marks that say where the
        /// path begins and where it has stopped.
        /// </summary>
        public const int Marker = 18;

        /// <summary>The light riding the head of the path, above the pipe.</summary>
        public const int Spark = 20;

        public const int Dust = 30;

        /// <summary>The tutorial hand, above everything on the board it points at.</summary>
        public const int Guide = 40;
    }
}
