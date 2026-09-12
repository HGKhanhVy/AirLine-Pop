namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The order the board's world renderers draw in.
    ///
    /// Cells and the path both sat at the default order and happened to come out the
    /// right way round; nothing said they had to, and a renderer added later could have
    /// landed anywhere in the stack. Naming the arrangement in one place makes it
    /// deliberate, and it is the sort of ordering bug that only shows on a device.
    ///
    /// Gaps of ten leave room to slide something between two layers without renumbering.
    ///
    /// A cell is no longer one sprite. The square reuses the reference block art, whose
    /// own children already sort from 0 to 10 among themselves, so the path has to start
    /// above that whole range or it flickers in and out of the block faces.
    /// </summary>
    public static class BoardSortingOrder
    {
        public const int Cells = 0;

        /// <summary>Highest order used inside the cell prefab's own art.</summary>
        public const int CellArtCeiling = 10;

        public const int Path = 20;

        public const int Dust = 30;
    }
}
