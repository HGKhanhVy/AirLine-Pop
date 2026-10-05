using System;
using System.Collections.Generic;

namespace ASTeams.SingleLine.Core
{
    public static class PathSequenceValidator
    {
        // On success the caller can reuse the visited mask as its search seed.
        public static bool IsValid(LevelData level, IReadOnlyList<int> path,
            bool[] visited, bool needsCompletion = false)
        {
            Array.Clear(visited, 0, level.Grid.CellCount);
            if (path.Count == 0 || path.Count > level.PathLength || !PathRules.CanStart(level, path[0]))
            {
                return false;
            }

            for (int i = 0; i < path.Count; i++)
            {
                int cell = path[i];
                if (!level.IsActive(cell) || visited[cell] ||
                    (i > 0 && !PathRules.CanEnter(level, visited, path[i - 1], cell)))
                {
                    return false;
                }

                PathRules.Visit(level, visited, cell, true);
            }

            return !needsCompletion || PathRules.IsComplete(level, path.Count, path[path.Count - 1]);
        }
    }
}
