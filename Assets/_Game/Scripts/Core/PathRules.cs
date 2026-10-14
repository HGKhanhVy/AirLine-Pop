using System;

namespace AirlinePop.SingleLine.Core
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
        /// MOV-01 and MOV-02: the target must be an active square with a pass left that is
        /// orthogonally adjacent to the head, and the head's own conditions must let the plane
        /// go there. <paramref name="previous"/> is the square flown before the head, or
        /// <see cref="LevelData.NoCell"/> at the start, since clouds and beacons judge the turn.
        /// </summary>
        public static bool CanEnter(LevelData level, byte[] visits, int previous, int head, int target)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (visits == null)
            {
                throw new ArgumentNullException(nameof(visits));
            }

            if (!level.IsActive(target) || level.IsOnWingSide(target) || !IsOpen(level, visits, target))
            {
                return false;
            }

            return level.Grid.AreAdjacent(head, target) && CanLeave(level, previous, head, target);
        }

        /// <summary>
        /// Adds or removes one pass over a square. The wingman's squares go in the same mask, so
        /// the rest of the rules read one mask for both planes.
        /// </summary>
        public static void Visit(LevelData level, byte[] visits, int cell, bool isVisited)
        {
            visits[cell] = (byte)(isVisited ? visits[cell] + 1 : visits[cell] - 1);
        }

        /// <summary>True when the route may still pass over the square: unflown, or an overpass crossed once.</summary>
        public static bool IsOpen(LevelData level, byte[] visits, int cell)
        {
            return visits[cell] < level.CapacityOf(cell);
        }

        /// <summary>
        /// The level's own conditions on a step from <paramref name="head"/> to
        /// <paramref name="target"/>: nothing flies on from the runway, a square with wind only
        /// lets the plane go the way it blows, a beacon is flown in from its tail and out along
        /// its arrow, and a cloud or an overpass only straight on. The solver asks the same
        /// question, so the two can never disagree.
        /// </summary>
        public static bool CanLeave(LevelData level, int previous, int head, int target)
        {
            return CanArrive(level, head, target) && CanLeaveSquare(level, previous, head, target);
        }

        /// <summary>A beacon is only entered from its tail: the step onto it already heads the way its arrow points.</summary>
        private static bool CanArrive(LevelData level, int head, int target)
        {
            return !level.TryGetBeacon(target, out Direction beacon) ||
                (level.Grid.TryGetNeighbor(head, beacon, out int ahead) && ahead == target);
        }

        private static bool CanLeaveSquare(LevelData level, int previous, int head, int target)
        {
            if (level.HasFixedEnd && head == level.FixedEnd)
            {
                return false;
            }

            if (level.TryGetWind(head, out Direction wind) &&
                (!level.Grid.TryGetNeighbor(head, wind, out int downwind) || downwind != target))
            {
                return false;
            }

            if (level.TryGetBeacon(head, out Direction beacon))
            {
                return level.Grid.TryGetNeighbor(head, beacon, out int lit) && lit == target;
            }

            CellKind kind = level.KindOf(head);

            if (kind == CellKind.Plain || previous == LevelData.NoCell)
            {
                return true;
            }

            // Adjacent squares on one line differ by the same index step, so equal steps in and
            // out mean the plane flew straight on. Clouds and overpasses both ask for that.
            return target - head == head - previous;
        }

        /// <summary>
        /// True when the head still has at least one legal continuation. Used to tell
        /// Drawing from Stuck (GDD 3.4) without allocating a neighbour list.
        /// </summary>
        public static bool HasAnyMove(LevelData level, byte[] visits, int previous, int head)
        {
            return CountAvailableMoves(level, visits, previous, head) > 0;
        }

        /// <summary>
        /// Counts legal continuations from a cell reached from <paramref name="previous"/>. The
        /// solver orders its candidates by this value, which is the Warnsdorff heuristic.
        /// </summary>
        public static int CountAvailableMoves(LevelData level, byte[] visits, int previous, int cell)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (visits == null)
            {
                throw new ArgumentNullException(nameof(visits));
            }

            Grid grid = level.Grid;
            int count = 0;

            for (int direction = 0; direction < Grid.NeighborCount; direction++)
            {
                if (!grid.TryGetNeighbor(cell, (Direction)direction, out int neighbor))
                {
                    continue;
                }

                if (level.IsActive(neighbor) && !level.IsOnWingSide(neighbor) && IsOpen(level, visits, neighbor) &&
                    CanLeave(level, previous, cell, neighbor))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// MOV-07: every square of the player's route covered and the end constraint satisfied.
        /// Only for a level without a wingman; a formation flight also needs the wingman's half.
        /// </summary>
        public static bool IsComplete(LevelData level, int pathLength, int head)
        {
            return IsComplete(level, pathLength, head, 0);
        }

        /// <summary>
        /// MOV-07 for any level: the player's route at full length, the end constraint met, and
        /// in a formation flight every square of the wingman's half covered too.
        /// </summary>
        public static bool IsComplete(LevelData level, int pathLength, int head, int wingCovered)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (pathLength != level.PathLength || (level.IsFormation && wingCovered != level.WingSideCellCount))
            {
                return false;
            }

            return !level.HasFixedEnd || level.FixedEnd == head;
        }
    }
}
