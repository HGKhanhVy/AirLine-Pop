using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ASTeams.SingleLine.Core
{
    public sealed class ContinuationHintService : IHintService
    {
        private readonly ILevelSolverFactory solverFactory;

        public ContinuationHintService(ILevelSolverFactory solverFactory)
        {
            this.solverFactory = solverFactory ?? throw new ArgumentNullException(nameof(solverFactory));
        }

        public Task<HintResult> FindHintAsync(LevelData level, IReadOnlyList<int> path, int steps,
            int nodeBudget, int timeBudgetMilliseconds, CancellationToken cancellationToken)
        {
            if (level == null || path == null)
            {
                throw new ArgumentNullException(level == null ? nameof(level) : nameof(path));
            }

            if (steps < 1 || steps > 3 || nodeBudget < 1 || timeBudgetMilliseconds < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(steps), "Hint configuration must have positive budgets and 1 to 3 steps.");
            }

            // The caller may continue drawing while the worker owns this snapshot.
            var snapshot = new int[path.Count];
            for (int i = 0; i < path.Count; i++)
            {
                snapshot[i] = path[i];
            }

            return Task.Run(() => FindHint(level, snapshot, steps, nodeBudget,
                timeBudgetMilliseconds, cancellationToken), cancellationToken);
        }

        private HintResult FindHint(LevelData level, int[] path, int steps,
            int nodeBudget, int timeBudgetMilliseconds, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var visited = new bool[level.Grid.CellCount];
            if (!PathSequenceValidator.IsValid(level, path, visited))
            {
                return HintResult.RestartRequired;
            }

            using (var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                budget.CancelAfter(timeBudgetMilliseconds);
                ILevelSolver solver = solverFactory.Create(nodeBudget, budget.Token);
                var prefix = new List<int>(path);
                var solution = new int[level.ActiveCellCount];

                try
                {
                    while (prefix.Count > 0)
                    {
                        budget.Token.ThrowIfCancellationRequested();
                        if (solver.TryContinue(level, prefix, solution, out int length))
                        {
                            return CreateResult(solution, length, prefix.Count, path.Length, steps);
                        }

                        if (solver.LastRunHitBudget)
                        {
                            break;
                        }

                        prefix.RemoveAt(prefix.Count - 1);
                    }
                }
                catch (OperationCanceledException)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            return StoredSolutionFallback(level, path, steps, visited);
        }

        private static HintResult StoredSolutionFallback(LevelData level, int[] path, int steps, bool[] visited)
        {
            if (!PathSequenceValidator.IsValid(level, level.Solution, visited, needsCompletion: true))
            {
                return HintResult.RestartRequired;
            }

            int shared = 0;
            while (shared < path.Length && shared < level.Solution.Count && path[shared] == level.Solution[shared])
            {
                shared++;
            }

            if (shared == 0)
            {
                return HintResult.RestartRequired;
            }

            return CreateResult(level.Solution, level.Solution.Count, shared, path.Length, steps);
        }

        private static HintResult CreateResult(IReadOnlyList<int> solution, int length,
            int retained, int originalLength, int steps)
        {
            int count = Math.Min(steps, length - retained);
            var next = new int[count];
            for (int i = 0; i < count; i++)
            {
                next[i] = solution[retained + i];
            }

            return new HintResult(originalLength - retained, next);
        }

    }
}
