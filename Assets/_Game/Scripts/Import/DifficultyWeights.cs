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
        /// Defaults led by how much trial and error a careful player needs, which tells apart
        /// boards of the same size; path length comes next, as what a player sees before
        /// starting. The greedy failure rate and the solver's search barely count: nearly
        /// every board defeats a greedy player, and the solver solves nearly every one
        /// without backtracking, so neither separates easy from hard.
        /// </summary>
        public static readonly DifficultyWeights Default = new DifficultyWeights(0.30, 0.05, 0.05, 0.05, 0.55);

        public double CellCount { get; }

        public double GreedyFailureRate { get; }

        public double SolverNodes { get; }

        /// <summary>
        /// Small on purpose. Average degree separates corridors from open boards, but on
        /// its own it predicts difficulty poorly: both extremes are easy.
        /// </summary>
        public double AverageDegree { get; }

        /// <summary>Steps a careful player takes, see <see cref="CautiousPlayer"/>.</summary>
        public double PlayerSteps { get; }

        public double Total => CellCount + GreedyFailureRate + SolverNodes + AverageDegree + PlayerSteps;

        public DifficultyWeights(double cellCount, double greedyFailureRate, double solverNodes, double averageDegree,
            double playerSteps = 0)
        {
            PlayerSteps = playerSteps;
            CellCount = cellCount;
            GreedyFailureRate = greedyFailureRate;
            SolverNodes = solverNodes;
            AverageDegree = averageDegree;
        }
    }
}
