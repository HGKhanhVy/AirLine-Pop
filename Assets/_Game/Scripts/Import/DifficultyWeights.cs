namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// How much each measured signal counts toward the final difficulty. Exposed as data
    /// rather than buried in the scorer so the weights can be tuned against playtest
    /// feedback without touching the logic that applies them.
    ///
    /// Every signal is turned into a percentile within the corpus before it is weighed,
    /// so the weights compare like with like and do not need unit conversion.
    /// </summary>
    public readonly struct DifficultyWeights
    {
        /// <summary>
        /// Defaults chosen so path length leads but does not dominate. Cell count is what
        /// a player sees before starting; the greedy failure rate is what they feel while
        /// playing, and the two together outweigh the search cost.
        /// </summary>
        public static readonly DifficultyWeights Default = new DifficultyWeights(0.40, 0.35, 0.20, 0.05);

        public double CellCount { get; }

        public double GreedyFailureRate { get; }

        public double SolverNodes { get; }

        /// <summary>
        /// Small on purpose. Average degree separates corridors from open boards, but on
        /// its own it predicts difficulty poorly: both extremes are easy.
        /// </summary>
        public double AverageDegree { get; }

        public double Total => CellCount + GreedyFailureRate + SolverNodes + AverageDegree;

        public DifficultyWeights(double cellCount, double greedyFailureRate, double solverNodes, double averageDegree)
        {
            CellCount = cellCount;
            GreedyFailureRate = greedyFailureRate;
            SolverNodes = solverNodes;
            AverageDegree = averageDegree;
        }
    }
}
