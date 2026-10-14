namespace AirlinePop.SingleLine.Core
{
    /// <summary>
    /// The wingman of a formation flight. It starts on the square mirroring the player's start
    /// and makes the mirror of every move the player makes: the same way up or down, the other
    /// way left or right. Its half of the board has a shape of its own, so when the square ahead
    /// of it is a hole, the board's edge, the middle or a square it has already flown, it holds
    /// its place and waits for the next move. Covering both halves means steering it as well.
    /// </summary>
    public static class Wingman
    {
        /// <summary>Where the wingman starts for a player starting on <paramref name="playerStart"/>.</summary>
        public static int StartFor(LevelData level, int playerStart)
        {
            return level.MirrorOf(playerStart);
        }

        /// <summary>
        /// Where the wingman ends up when the player flies from <paramref name="from"/> to
        /// <paramref name="to"/>: one square on in the mirrored direction, or where it was.
        /// </summary>
        public static int Step(LevelData level, byte[] visits, int wingHead, int from, int to)
        {
            Grid grid = level.Grid;
            int target = grid.ToRow(from) == grid.ToRow(to) ? wingHead - (to - from) : wingHead + (to - from);

            if (!grid.Contains(target) || !grid.AreAdjacent(wingHead, target) || !level.IsActive(target) ||
                !level.IsOnWingSide(target) || visits[target] > 0)
            {
                return wingHead;
            }

            return target;
        }
    }
}
