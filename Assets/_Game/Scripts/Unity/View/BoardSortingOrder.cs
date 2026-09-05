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
    /// </summary>
    public static class BoardSortingOrder
    {
        public const int Cells = 0;

        public const int Path = 10;

        public const int Dust = 20;
    }
}
