using System;

namespace ASTeams.SingleLine.Core
{
    /// <summary>
    /// Runtime state of one attempt at a level: the drawn path plus the state machine
    /// of GDD 3.5 restricted to its gameplay states. Pure C# and engine free, so the
    /// rules can be unit tested without entering play mode.
    ///
    /// Every buffer is allocated once in the constructor and reused across restarts,
    /// so dragging a finger across the board produces no garbage.
    /// </summary>
    public sealed class PathSession
    {
        private readonly LevelData level;
        private readonly bool[] visited;
        private readonly int[] path;

        private int length;
        private PathState state;

        /// <summary>Raised with the cell index each time the path grows.</summary>
        public event Action<int> OnCellVisited;

        /// <summary>Raised with the cell index each time a backtrack or undo shrinks the path.</summary>
        public event Action<int> OnCellUnvisited;

        /// <summary>Raised as (previous, current) only when the state actually changes.</summary>
        public event Action<PathState, PathState> OnStateChanged;

        public LevelData Level => level;

        public PathState State => state;

        /// <summary>Number of cells currently drawn, which equals the visited count.</summary>
        public int Length => length;

        /// <summary>Last cell of the path, or <see cref="LevelData.NoCell"/> when empty.</summary>
        public int Head => length > 0 ? path[length - 1] : LevelData.NoCell;

        public PathSession(LevelData level)
        {
            this.level = level ?? throw new ArgumentNullException(nameof(level));

            visited = new bool[level.Grid.CellCount];
            path = new int[level.ActiveCellCount];
            state = PathState.Ready;
        }

        public bool IsVisited(int cell)
        {
            return cell >= 0 && cell < visited.Length && visited[cell];
        }

        /// <summary>Cell drawn at <paramref name="step"/>, zero based.</summary>
        public int GetCell(int step)
        {
            if (step < 0 || step >= length)
            {
                throw new ArgumentOutOfRangeException(nameof(step), step, "Step is outside the drawn path.");
            }

            return path[step];
        }

        public void CopyPathTo(int[] destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (destination.Length < length)
            {
                throw new ArgumentException("Destination is too small for the current path.", nameof(destination));
            }

            Array.Copy(path, destination, length);
        }

        /// <summary>
        /// Single entry point for pointer input. Starts the path, extends it, or
        /// backtracks one step when the target is the cell drawn just before the head.
        /// An illegal target is reported as <see cref="MoveResult.Rejected"/> and
        /// changes nothing, which is what MOV-06 requires. Touching the head itself is
        /// also a no-op, so the input layer can re-acquire a lifted drag (MOV-04) by
        /// simply feeding the head back in.
        /// </summary>
        public MoveResult Move(int cell)
        {
            if (state == PathState.Won)
            {
                return MoveResult.Rejected;
            }

            if (length == 0)
            {
                if (!PathRules.CanStart(level, cell))
                {
                    return MoveResult.Rejected;
                }

                Push(cell);
                UpdateState();
                return state == PathState.Won ? MoveResult.Completed : MoveResult.Started;
            }

            if (cell == path[length - 1])
            {
                return MoveResult.Rejected;
            }

            if (length >= 2 && cell == path[length - 2])
            {
                Pop();
                UpdateState();
                return MoveResult.Backtracked;
            }

            if (!PathRules.CanEnter(level, visited, path[length - 1], cell))
            {
                return MoveResult.Rejected;
            }

            Push(cell);
            UpdateState();
            return state == PathState.Won ? MoveResult.Completed : MoveResult.Moved;
        }

        /// <summary>
        /// Removes the last drawn cell (GDD 3.3). Identical in effect to a drag
        /// backtrack, so both the button and the gesture share one code path.
        /// Returns false when there is nothing to undo, or once the level is won.
        /// </summary>
        public bool Undo()
        {
            if (state == PathState.Won || length == 0)
            {
                return false;
            }

            Pop();
            UpdateState();
            return true;
        }

        /// <summary>
        /// Clears the path without reloading anything. Only the cells actually drawn
        /// are unmarked, so the cost is proportional to the path, not to the board.
        /// </summary>
        public void Restart()
        {
            while (length > 0)
            {
                Pop();
            }

            UpdateState();
        }

        private void Push(int cell)
        {
            path[length] = cell;
            length++;
            visited[cell] = true;
            OnCellVisited?.Invoke(cell);
        }

        private void Pop()
        {
            length--;
            int cell = path[length];
            visited[cell] = false;
            OnCellUnvisited?.Invoke(cell);
        }

        private void UpdateState()
        {
            PathState next;

            if (length == 0)
            {
                next = PathState.Ready;
            }
            else if (PathRules.IsComplete(level, length, path[length - 1]))
            {
                next = PathState.Won;
            }
            else if (!PathRules.HasAnyMove(level, visited, path[length - 1]))
            {
                next = PathState.Stuck;
            }
            else
            {
                next = PathState.Drawing;
            }

            if (next == state)
            {
                return;
            }

            PathState previous = state;
            state = next;
            OnStateChanged?.Invoke(previous, next);
        }
    }
}
