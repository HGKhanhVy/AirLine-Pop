using System.Collections.Generic;

namespace ASTeams.SingleLine.Core
{
    /// <summary>
    /// Outcome of validating one level. Carries every issue found rather than stopping
    /// at the first, so a level author fixes one board in one pass.
    /// </summary>
    public sealed class LevelValidationResult
    {
        private static readonly LevelIssue[] NoIssues = new LevelIssue[0];

        private readonly IReadOnlyList<LevelIssue> issues;

        public string LevelId { get; }

        public IReadOnlyList<LevelIssue> Issues => issues;

        public bool IsValid => issues.Count == 0;

        /// <summary>
        /// A solution for the level: the stored one when it validated, otherwise the one
        /// the solver found. Empty when the level is unsolvable or the search gave up.
        /// </summary>
        public IReadOnlyList<int> Solution { get; }

        /// <summary>Nodes the solver expanded, or zero when the stored solution was used.</summary>
        public long SolverNodeCount { get; }

        public LevelValidationResult(
            string levelId,
            IReadOnlyList<LevelIssue> issues,
            IReadOnlyList<int> solution,
            long solverNodeCount)
        {
            LevelId = levelId;
            this.issues = issues ?? NoIssues;
            Solution = solution ?? new int[0];
            SolverNodeCount = solverNodeCount;
        }
    }
}
