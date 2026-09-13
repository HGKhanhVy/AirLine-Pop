namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The order the board's world renderers draw in.
    ///
    /// The connector runs *under* the squares. The reference game joins two blocks with a
    /// short bar that only shows in the gap between them, and the cheapest way to get that
    /// is to draw one line through the whole path and let the squares cover the parts
    /// crossing them. Ninety squares would otherwise each need their own renderer to draw
    /// the same picture.
    ///
    /// A cell is no longer one sprite either: it reuses the reference block art, whose own
    /// children sort from 0 to 10 among themselves, so anything meant to sit above a square
    /// has to clear that whole range.
    /// </summary>
    public static class BoardSortingOrder
    {
        /// <summary>The bar joining two squares, hidden wherever a square covers it.</summary>
        public const int Connector = -10;

        public const int Cells = 0;

        /// <summary>Highest order used inside the cell prefab's own art.</summary>
        public const int CellArtCeiling = 10;

        /// <summary>The light riding the head of the path, above the squares.</summary>
        public const int Spark = 20;

        public const int Dust = 30;
    }
}
