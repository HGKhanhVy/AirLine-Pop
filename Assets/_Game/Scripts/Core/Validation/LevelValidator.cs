using System;
using System.Collections.Generic;

namespace ASTeams.SingleLine.Core
{
    /// <summary>
    /// Checks one level against the export gate in GDD 6.4. Depends on the
    /// <see cref="ILevelSolver"/> abstraction rather than on a concrete search, so the
    /// solver can be swapped or budgeted per environment.
    ///
    /// Level id uniqueness is deliberately out of scope: it is a property of a level
    /// collection, not of a level, and belongs to the repository that owns the set.
    /// </summary>
    public sealed class LevelValidator
    {
        private const int MinDifficulty = 1;
        private const int MaxDifficulty = 10;

        private readonly ILevelSolver solver;

        private bool[] seen;
        private int[] stack;
        private int[] solverBuffer;

        public LevelValidator(ILevelSolver solver)
        {
            this.solver = solver ?? throw new ArgumentNullException(nameof(solver));
        }

        public LevelValidationResult Validate(LevelData level)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            var issues = new List<LevelIssue>();

            if (level.ActiveCellCount == 0)
            {
                issues.Add(new LevelIssue(LevelIssueCode.EmptyBoard));
                return new LevelValidationResult(level.Id, issues, null, 0);
            }

            EnsureCapacity(level.Grid.CellCount);

            if (level.DeclaredActiveCells.Count != level.ActiveCellCount)
            {
                issues.Add(new LevelIssue(LevelIssueCode.DuplicateActiveCell));
            }

            if (level.Difficulty < MinDifficulty || level.Difficulty > MaxDifficulty)
            {
                issues.Add(new LevelIssue(LevelIssueCode.DifficultyOutOfRange));
            }

            if (level.HasFixedStart && !level.IsActive(level.FixedStart))
            {
                issues.Add(new LevelIssue(LevelIssueCode.FixedStartNotActive, level.FixedStart));
            }

            if (level.HasFixedEnd && !level.IsActive(level.FixedEnd))
            {
                issues.Add(new LevelIssue(LevelIssueCode.FixedEndNotActive, level.FixedEnd));
            }

            bool connected = IsBoardConnected(level);

            if (!connected)
            {
                issues.Add(new LevelIssue(LevelIssueCode.DisconnectedBoard));
            }

            bool storedSolutionIsValid = level.HasSolution && ValidateStoredSolution(level, issues);

            if (storedSolutionIsValid)
            {
                return new LevelValidationResult(level.Id, issues, level.Solution, 0);
            }

            // A disconnected board can never be solved, so spending the solver budget on
            // it would only slow a batch import down.
            if (!connected)
            {
                return new LevelValidationResult(level.Id, issues, null, 0);
            }

            return SolveAndReport(level, issues);
        }

        private LevelValidationResult SolveAndReport(LevelData level, List<LevelIssue> issues)
        {
            if (solverBuffer == null || solverBuffer.Length < level.ActiveCellCount)
            {
                solverBuffer = new int[level.ActiveCellCount];
            }

            bool solved = solver.TrySolveAny(level, solverBuffer, out int length, out int _);
            long nodeCount = solver.LastNodeCount;

            if (!solved)
            {
                issues.Add(new LevelIssue(solver.LastRunHitBudget
                    ? LevelIssueCode.SolverBudgetExceeded
                    : LevelIssueCode.NoSolutionFound));

                return new LevelValidationResult(level.Id, issues, null, nodeCount);
            }

            var solution = new int[length];
            Array.Copy(solverBuffer, solution, length);
            return new LevelValidationResult(level.Id, issues, solution, nodeCount);
        }

        /// <summary>
        /// Returns true only when the stored solution passes every check. Issues are
        /// appended either way, so a broken solution is reported and then replaced by a
        /// freshly solved one.
        /// </summary>
        private bool ValidateStoredSolution(LevelData level, List<LevelIssue> issues)
        {
            IReadOnlyList<int> solution = level.Solution;
            bool valid = true;

            if (solution.Count != level.ActiveCellCount)
            {
                issues.Add(new LevelIssue(LevelIssueCode.SolutionWrongLength));
                valid = false;
            }

            Array.Clear(seen, 0, level.Grid.CellCount);

            for (int step = 0; step < solution.Count; step++)
            {
                int cell = solution[step];

                if (!level.IsActive(cell))
                {
                    issues.Add(new LevelIssue(LevelIssueCode.SolutionLeavesBoard, cell));
                    return false;
                }

                if (seen[cell])
                {
                    issues.Add(new LevelIssue(LevelIssueCode.SolutionRepeatsCell, cell));
                    valid = false;
                    continue;
                }

                seen[cell] = true;

                if (step > 0 && !level.Grid.AreAdjacent(solution[step - 1], cell))
                {
                    issues.Add(new LevelIssue(LevelIssueCode.SolutionNotContinuous, cell));
                    valid = false;
                }
            }

            if (solution.Count > 0)
            {
                if (level.HasFixedStart && solution[0] != level.FixedStart)
                {
                    issues.Add(new LevelIssue(LevelIssueCode.SolutionWrongStart, solution[0]));
                    valid = false;
                }

                int last = solution[solution.Count - 1];

                if (level.HasFixedEnd && last != level.FixedEnd)
                {
                    issues.Add(new LevelIssue(LevelIssueCode.SolutionWrongEnd, last));
                    valid = false;
                }
            }

            return valid;
        }

        /// <summary>Flood fill over active cells; a solvable board is always one region.</summary>
        private bool IsBoardConnected(LevelData level)
        {
            Grid grid = level.Grid;

            Array.Clear(seen, 0, grid.CellCount);

            int seed = level.DeclaredActiveCells[0];
            int top = 0;
            stack[top++] = seed;
            seen[seed] = true;
            int reached = 0;

            while (top > 0)
            {
                int cell = stack[--top];
                reached++;

                for (int direction = 0; direction < Grid.NeighborCount; direction++)
                {
                    if (!grid.TryGetNeighbor(cell, (Direction)direction, out int neighbor))
                    {
                        continue;
                    }

                    if (!level.IsActive(neighbor) || seen[neighbor])
                    {
                        continue;
                    }

                    seen[neighbor] = true;
                    stack[top++] = neighbor;
                }
            }

            return reached == level.ActiveCellCount;
        }

        private void EnsureCapacity(int cellCount)
        {
            if (seen != null && seen.Length >= cellCount)
            {
                return;
            }

            seen = new bool[cellCount];
            stack = new int[cellCount];
        }
    }
}
