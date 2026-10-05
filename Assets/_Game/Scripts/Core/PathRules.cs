using System;

namespace ASTeams.SingleLine.Core
{
    /// <summary>
    /// Stateless predicates for the MVP movement rules (GDD 3.2). Kept free of any
    /// session state so that both <see cref="PathSession"/> and the solver share one
    /// definition of what a legal step is.
    ///
    /// MOV-03 (path continuity) is an invariant of these predicates rather than a
    /// separate check: every accepted step is adjacent to the head, so a path built
    /// only through them is continuous by construction.
    /// MOV-04 (path survives a finger lift), MOV-05 (interpolate a fast drag) and
    /// MOV-06 (ignore illegal input instead of resetting) are input layer duties;
    /// this class serves them by being pure and by never mutating on rejection.
    /// </summary>
    public static class PathRules
    {
        /// <summary>MVP allows any active cell to start, unless the level pins one.</summary>
        public static bool CanStart(LevelData level, int cell)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (!level.IsActive(cell))
            {
                return false;
            }

            return !level.HasFixedStart || level.FixedStart == cell;
        }

        /// <summary>
        /// MOV-01 and MOV-02: the target must be an unvisited active cell that is
        /// orthogonally adjacent to the head.
        /// </summary>
        public static bool CanEnter(LevelData level, bool[] visited, int head, int target)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (visited == null)
            {
                throw new ArgumentNullException(nameof(visited));
            }

            if (!level.IsActive(target) || IsCovered(level, visited, target))
            {
                return false;
            }

            return level.Grid.AreAdjacent(head, target) && CanLeave(level, head, target);
        }

        /// <summary>
        /// Marks a square visited or not. In a formation flight the wingman flies the mirror
        /// square at the same time, so both are marked together and the rest of the rules
        /// read one mask for both planes.
        /// </summary>
        public static void Visit(LevelData level, bool[] visited, int cell, bool isVisited)
        {
            visited[cell] = isVisited;

            if (level.IsFormation)
            {
                visited[level.MirrorOf(cell)] = isVisited;
            }
        }

        /// <summary>True when either plane has already flown over the square.</summary>
        private static bool IsCovered(LevelData level, bool[] visited, int cell)
        {
            return visited[cell] || (level.IsFormation && visited[level.MirrorOf(cell)]);
        }

        /// <summary>
        /// The level's own conditions on leaving a square: nothing flies on from the runway,
        /// and a square with wind only lets the plane go the way it blows. The solver asks
        /// the same question, so the two can never disagree.
        /// </summary>
        public static bool CanLeave(LevelData level, int head, int target)
        {
            if (level.HasFixedEnd && head == level.FixedEnd)
            {
                return false;
            }

            if (level.TryGetWind(head, out Direction wind))
            {
                return level.Grid.TryGetNeighbor(head, wind, out int downwind) && downwind == target;
            }

            return true;
        }

        /// <summary>
        /// True when the head still has at least one legal continuation. Used to tell
        /// Drawing from Stuck (GDD 3.4) without allocating a neighbour list.
        /// </summary>
        public static bool HasAnyMove(LevelData level, bool[] visited, int head)
        {
            return CountAvailableMoves(level, visited, head) > 0;
        }

        /// <summary>
        /// Counts legal continuations from a cell. The solver orders its candidates by
        /// this value, which is the Warnsdorff heuristic.
        /// </summary>
        public static int CountAvailableMoves(LevelData level, bool[] visited, int cell)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (visited == null)
            {
                throw new ArgumentNullException(nameof(visited));
            }

            Grid grid = level.Grid;
            int count = 0;

            for (int direction = 0; direction < Grid.NeighborCount; direction++)
            {
                if (!grid.TryGetNeighbor(cell, (Direction)direction, out int neighbor))
                {
                    continue;
                }

                if (level.IsActive(neighbor) && !IsCovered(level, visited, neighbor) && CanLeave(level, cell, neighbor))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// MOV-07: every active cell covered and the end constraint satisfied. Takes the
        /// length of the player's route, which in a formation flight covers two squares a step.
        /// </summary>
        public static bool IsComplete(LevelData level, int pathLength, int head)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (pathLength != level.PathLength)
            {
                return false;
            }

            return !level.HasFixedEnd || level.FixedEnd == head;
        }
    }
}
