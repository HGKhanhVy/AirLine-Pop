namespace ASTeams.SingleLine.Core
{
    /// <summary>
    /// Why a level failed validation (GDD 6.4). Codes rather than strings so the
    /// importer can count and group failures across a whole level pack.
    /// </summary>
    public enum LevelIssueCode
    {
        None = 0,

        /// <summary>The level declares no active cell at all.</summary>
        EmptyBoard = 1,

        /// <summary>The same cell index appears more than once in the active list.</summary>
        DuplicateActiveCell = 2,

        /// <summary>Active cells split into two or more regions with no orthogonal link.</summary>
        DisconnectedBoard = 3,

        /// <summary>fixedStart points at a cell that is not active.</summary>
        FixedStartNotActive = 4,

        /// <summary>fixedEnd points at a cell that is not active.</summary>
        FixedEndNotActive = 5,

        /// <summary>The stored solution does not have one entry per active cell.</summary>
        SolutionWrongLength = 6,

        /// <summary>The stored solution visits a cell twice.</summary>
        SolutionRepeatsCell = 7,

        /// <summary>Two consecutive solution cells are not orthogonally adjacent.</summary>
        SolutionNotContinuous = 8,

        /// <summary>The stored solution steps on a cell that is not active.</summary>
        SolutionLeavesBoard = 9,

        /// <summary>The stored solution does not begin at the pinned start.</summary>
        SolutionWrongStart = 10,

        /// <summary>The stored solution does not end at the pinned end.</summary>
        SolutionWrongEnd = 11,

        /// <summary>No solution is stored and the solver proved none exists.</summary>
        NoSolutionFound = 12,

        /// <summary>The solver ran out of node budget, so solvability stays unknown.</summary>
        SolverBudgetExceeded = 13,

        /// <summary>Difficulty falls outside the 1 to 10 range required by GDD 6.3.</summary>
        DifficultyOutOfRange = 14
    }
}
