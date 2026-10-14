using System;
using System.Collections.Generic;

namespace AirlinePop.SingleLine.Core
{
    public static class PathSequenceValidator
    {
        // On success the caller can reuse the visited mask as its search seed.
        public static bool IsValid(LevelData level, IReadOnlyList<int> path,
            byte[] visits, bool needsCompletion = false)
        {
            return IsValid(level, path, visits, needsCompletion, out _, out _);
        }

        /// <summary>
        /// Replays <paramref name="path"/> under every rule, the wingman's moves included, and hands
        /// back where the wingman ended and how many of its squares it covered.
        /// </summary>
        public static bool IsValid(LevelData level, IReadOnlyList<int> path, byte[] visits, bool needsCompletion,
            out int wingHead, out int wingCovered)
        {
            wingHead = LevelData.NoCell;
            wingCovered = 0;
            Array.Clear(visits, 0, level.Grid.CellCount);

            if (path.Count == 0 || path.Count > level.PathLength || !PathRules.CanStart(level, path[0]))
            {
                return false;
            }

            for (int i = 0; i < path.Count; i++)
            {
                int cell = path[i];

                if (!level.IsActive(cell) || level.IsOnWingSide(cell) || !PathRules.IsOpen(level, visits, cell) ||
                    (i > 0 && !PathRules.CanEnter(level, visits, i > 1 ? path[i - 2] : LevelData.NoCell, path[i - 1], cell)))
                {
                    return false;
                }

                if (level.IsFormation)
                {
                    int wing = i == 0 ? Wingman.StartFor(level, cell) : Wingman.Step(level, visits, wingHead, path[i - 1], cell);

                    if (i == 0 && (!level.IsActive(wing) || !level.IsOnWingSide(wing)))
                    {
                        return false;
                    }

                    if (i == 0 || wing != wingHead)
                    {
                        PathRules.Visit(level, visits, wing, true);
                        wingCovered++;
                    }

                    wingHead = wing;
                }

                PathRules.Visit(level, visits, cell, true);
            }

            return !needsCompletion || PathRules.IsComplete(level, path.Count, path[path.Count - 1], wingCovered);
        }
    }
}
