using System;
using System.Collections.Generic;
using System.Threading;

namespace ASTeams.SingleLine.Core
{
    /// <summary>
    /// Depth first Hamiltonian path search ordered by the Warnsdorff heuristic: always
    /// try the neighbour with the fewest onward moves first, which on grid boards walks
    /// the border inward and usually reaches a solution with almost no backtracking.
    ///
    /// Two prunes keep the worst case in check. Before descending, the still unvisited
    /// cells must form a single connected region, which also subsumes the cheaper
    /// "isolated cell" test. A node budget bounds the search so one pathological level
    /// cannot stall a batch validation run over the whole level set.
    ///
    /// All scratch buffers are pooled on the instance and grown on demand, so solving
    /// hundreds of levels in a row allocates only when a bigger board appears. The
    /// instance is therefore stateful and not safe to share across threads.
    /// </summary>
    public sealed class WarnsdorffSolver : ILevelSolver
    {
        public const int DefaultNodeBudget = 2_000_000;

        private readonly int nodeBudget;
        private readonly CancellationToken cancellationToken;

        private LevelData level;
        private bool[] visited;
        private int[] path;
        private int[] candidates;
        private int[] candidateDegrees;
        private int[] floodStack;
        private int[] floodStamp;
        private int floodGeneration;
        private long nodeCount;
        private bool isBudgetExhausted;

        /// <summary>Nodes expanded by the most recent search, for profiling level difficulty.</summary>
        public long LastNodeCount { get; private set; }

        /// <summary>
        /// True when the last search stopped on the node budget rather than proving the
        /// level unsolvable. Callers must not treat that as "no solution exists".
        /// </summary>
        public bool LastRunHitBudget { get; private set; }

        public WarnsdorffSolver(int nodeBudget = DefaultNodeBudget, CancellationToken cancellationToken = default)
        {
            if (nodeBudget <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(nodeBudget), nodeBudget, "Node budget must be positive.");
            }

            this.nodeBudget = nodeBudget;
            this.cancellationToken = cancellationToken;
        }

        public bool TrySolve(LevelData level, int startCell, int[] destination, out int length)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            length = 0;
            LastNodeCount = 0;
            LastRunHitBudget = false;

            if (level.ActiveCellCount == 0 || !PathRules.CanStart(level, startCell))
            {
                return false;
            }

            if (destination.Length < level.ActiveCellCount)
            {
                throw new ArgumentException("Destination is too small for the solution.", nameof(destination));
            }

            PrepareFor(level);

            visited[startCell] = true;
            bool solved = Search(startCell, 0);

            LastNodeCount = nodeCount;
            LastRunHitBudget = isBudgetExhausted;

            if (!solved)
            {
                return false;
            }

            length = level.ActiveCellCount;
            Array.Copy(path, destination, length);
            return true;
        }

        public bool TrySolveAny(LevelData level, int[] destination, out int length, out int startCell)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (level.HasFixedStart)
            {
                startCell = level.FixedStart;
                return TrySolve(level, startCell, destination, out length);
            }

            long totalNodes = 0;
            bool hitBudget = false;

            for (int i = 0; i < level.DeclaredActiveCells.Count; i++)
            {
                int candidate = level.DeclaredActiveCells[i];

                if (TrySolve(level, candidate, destination, out length))
                {
                    LastNodeCount += totalNodes;
                    startCell = candidate;
                    return true;
                }

                totalNodes += LastNodeCount;
                hitBudget |= LastRunHitBudget;
            }

            LastNodeCount = totalNodes;
            LastRunHitBudget = hitBudget;
            length = 0;
            startCell = LevelData.NoCell;
            return false;
        }

        public bool TryContinue(LevelData level, IReadOnlyList<int> pathSoFar, int[] destination, out int length)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (pathSoFar == null)
            {
                throw new ArgumentNullException(nameof(pathSoFar));
            }

            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            length = 0;
            LastNodeCount = 0;
            LastRunHitBudget = false;

            if (pathSoFar.Count == 0 || pathSoFar.Count > level.ActiveCellCount ||
                !PathRules.CanStart(level, pathSoFar[0]))
            {
                return false;
            }

            if (destination.Length < level.ActiveCellCount)
            {
                throw new ArgumentException("Destination is too small for the solution.", nameof(destination));
            }

            PrepareFor(level);

            if (!PathSequenceValidator.IsValid(level, pathSoFar, visited))
            {
                return false;
            }

            for (int i = 0; i < pathSoFar.Count; i++)
            {
                path[i] = pathSoFar[i];
            }

            int head = pathSoFar[pathSoFar.Count - 1];
            bool solved = Search(head, pathSoFar.Count - 1);

            LastNodeCount = nodeCount;
            LastRunHitBudget = isBudgetExhausted;

            if (!solved)
            {
                return false;
            }

            length = level.ActiveCellCount;
            Array.Copy(path, destination, length);
            return true;
        }

        private void PrepareFor(LevelData target)
        {
            level = target;
            nodeCount = 0;
            isBudgetExhausted = false;

            int cellCount = target.Grid.CellCount;

            if (visited == null || visited.Length < cellCount)
            {
                visited = new bool[cellCount];
                path = new int[cellCount];
                candidates = new int[cellCount * Grid.NeighborCount];
                candidateDegrees = new int[cellCount * Grid.NeighborCount];
                floodStack = new int[cellCount];
                floodStamp = new int[cellCount];
                floodGeneration = 0;
                return;
            }

            Array.Clear(visited, 0, visited.Length);

            // The flood fill identifies visited nodes by a monotonically rising stamp so
            // it never has to clear its scratch array. Reset only on the rare wrap.
            if (floodGeneration == int.MaxValue)
            {
                Array.Clear(floodStamp, 0, floodStamp.Length);
                floodGeneration = 0;
            }
        }

        private bool Search(int current, int depth)
        {
            cancellationToken.ThrowIfCancellationRequested();
            nodeCount++;

            if (nodeCount > nodeBudget)
            {
                isBudgetExhausted = true;
                return false;
            }

            path[depth] = current;
            int visitedCount = depth + 1;

            if (visitedCount == level.ActiveCellCount)
            {
                return !level.HasFixedEnd || level.FixedEnd == current;
            }

            int baseIndex = depth * Grid.NeighborCount;
            int count = CollectCandidates(current, baseIndex);

            if (count == 0)
            {
                return false;
            }

            SortCandidatesByDegree(baseIndex, count);

            for (int i = 0; i < count; i++)
            {
                int next = candidates[baseIndex + i];

                visited[next] = true;

                if (IsRemainderReachable(next, visitedCount + 1) && Search(next, depth + 1))
                {
                    return true;
                }

                visited[next] = false;

                if (isBudgetExhausted)
                {
                    return false;
                }
            }

            return false;
        }

        private int CollectCandidates(int current, int baseIndex)
        {
            Grid grid = level.Grid;
            int count = 0;

            for (int direction = 0; direction < Grid.NeighborCount; direction++)
            {
                if (!grid.TryGetNeighbor(current, (Direction)direction, out int neighbor))
                {
                    continue;
                }

                if (!level.IsActive(neighbor) || visited[neighbor])
                {
                    continue;
                }

                candidates[baseIndex + count] = neighbor;
                candidateDegrees[baseIndex + count] = PathRules.CountAvailableMoves(level, visited, neighbor);
                count++;
            }

            return count;
        }

        /// <summary>Insertion sort over at most four entries, ascending by onward degree.</summary>
        private void SortCandidatesByDegree(int baseIndex, int count)
        {
            for (int i = 1; i < count; i++)
            {
                int cell = candidates[baseIndex + i];
                int degree = candidateDegrees[baseIndex + i];
                int j = i - 1;

                while (j >= 0 && candidateDegrees[baseIndex + j] > degree)
                {
                    candidates[baseIndex + j + 1] = candidates[baseIndex + j];
                    candidateDegrees[baseIndex + j + 1] = candidateDegrees[baseIndex + j];
                    j--;
                }

                candidates[baseIndex + j + 1] = cell;
                candidateDegrees[baseIndex + j + 1] = degree;
            }
        }

        /// <summary>
        /// The remaining cells must form one connected region, because the path leaves
        /// the head and can never come back through it. Flooding from a single unvisited
        /// neighbour and comparing the count against the remainder detects both a split
        /// board and a cell that has been stranded with no unvisited neighbour.
        /// </summary>
        private bool IsRemainderReachable(int head, int visitedCount)
        {
            int remaining = level.ActiveCellCount - visitedCount;

            if (remaining <= 0)
            {
                return true;
            }

            Grid grid = level.Grid;
            int seed = LevelData.NoCell;

            for (int direction = 0; direction < Grid.NeighborCount; direction++)
            {
                if (!grid.TryGetNeighbor(head, (Direction)direction, out int neighbor))
                {
                    continue;
                }

                if (level.IsActive(neighbor) && !visited[neighbor])
                {
                    seed = neighbor;
                    break;
                }
            }

            if (seed == LevelData.NoCell)
            {
                return false;
            }

            floodGeneration++;
            floodStamp[seed] = floodGeneration;

            int top = 0;
            floodStack[top++] = seed;
            int reached = 0;

            while (top > 0)
            {
                int cell = floodStack[--top];
                reached++;

                if (reached > remaining)
                {
                    return false;
                }

                for (int direction = 0; direction < Grid.NeighborCount; direction++)
                {
                    if (!grid.TryGetNeighbor(cell, (Direction)direction, out int neighbor))
                    {
                        continue;
                    }

                    if (!level.IsActive(neighbor) || visited[neighbor])
                    {
                        continue;
                    }

                    if (floodStamp[neighbor] == floodGeneration)
                    {
                        continue;
                    }

                    floodStamp[neighbor] = floodGeneration;
                    floodStack[top++] = neighbor;
                }
            }

            return reached == remaining;
        }
    }
}
