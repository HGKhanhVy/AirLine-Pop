namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// Raw measurements of one level, before they are weighed against the rest of the
    /// corpus. Kept separate from the final score so the editor tool can show why a
    /// level landed where it did, rather than only the number.
    /// </summary>
    public readonly struct LevelFeatures
    {
        /// <summary>How long the path will be. Drives perceived difficulty more than anything else.</summary>
        public int CellCount { get; }

        /// <summary>
        /// Mean number of active orthogonal neighbours. A board near two is a corridor
        /// where most moves are forced; a board near four is wide open.
        /// </summary>
        public double AverageDegree { get; }

        /// <summary>Nodes the solver expanded. High means the board punishes a wrong turn.</summary>
        public long SolverNodes { get; }

        /// <summary>How many of the six greedy player styles failed, zero to six.</summary>
        public int GreedyFailures { get; }

        /// <summary>False when the solver could not finish, so the level must not ship.</summary>
        public bool IsSolvable { get; }

        public double GreedyFailureRate => GreedyFailures / (double)GreedyPlayer.StrategyCount;

        public LevelFeatures(int cellCount, double averageDegree, long solverNodes, int greedyFailures, bool isSolvable)
        {
            CellCount = cellCount;
            AverageDegree = averageDegree;
            SolverNodes = solverNodes;
            GreedyFailures = greedyFailures;
            IsSolvable = isSolvable;
        }

        public override string ToString()
        {
            return CellCount + " cells, degree " + AverageDegree.ToString("0.00") +
                   ", " + SolverNodes + " nodes, " + GreedyFailures + "/6 greedy failures";
        }
    }
}
