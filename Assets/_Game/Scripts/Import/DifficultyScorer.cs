using System;
using System.Collections.Generic;
using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// Turns four raw signals into the 1 to 10 difficulty GDD 6.3 requires.
    ///
    /// Difficulty is scored relative to the corpus rather than on an absolute scale.
    /// There is no natural unit that puts "37 cells" and "four of six greedy players
    /// failed" on the same axis, but ranking each signal within the set does, and it
    /// also guarantees the ten difficulty bands come out evenly populated, which is
    /// exactly what the chapter assembler needs to draw from.
    /// </summary>
    public sealed class DifficultyScorer
    {
        public const int MinDifficulty = 1;
        public const int MaxDifficulty = 10;

        private readonly LevelFeatureExtractor extractor;
        private readonly DifficultyWeights weights;

        public DifficultyScorer(ILevelSolver solver)
            : this(new LevelFeatureExtractor(solver), DifficultyWeights.Default)
        {
        }

        public DifficultyScorer(LevelFeatureExtractor extractor, DifficultyWeights weights)
        {
            this.extractor = extractor ?? throw new ArgumentNullException(nameof(extractor));

            if (weights.Total <= 0)
            {
                throw new ArgumentException("Weights must not all be zero.", nameof(weights));
            }

            this.weights = weights;
        }

        /// <summary>
        /// Measures and ranks every level in one pass. Order of the result matches the
        /// order of the input, so the caller can zip it back against its own bookkeeping.
        /// </summary>
        public List<ScoredLevel> Score(IReadOnlyList<LevelData> levels)
        {
            if (levels == null)
            {
                throw new ArgumentNullException(nameof(levels));
            }

            int count = levels.Count;
            var scored = new List<ScoredLevel>(count);

            if (count == 0)
            {
                return scored;
            }

            var features = new LevelFeatures[count];
            var cellCounts = new double[count];
            var greedyRates = new double[count];
            var solverNodes = new double[count];
            var degrees = new double[count];

            for (int i = 0; i < count; i++)
            {
                features[i] = extractor.Extract(levels[i]);
                cellCounts[i] = features[i].CellCount;
                greedyRates[i] = features[i].GreedyFailureRate;

                // Node counts span six orders of magnitude, so one pathological level
                // would otherwise flatten the ranking for everything else.
                solverNodes[i] = Math.Log(1 + features[i].SolverNodes);
                degrees[i] = features[i].AverageDegree;
            }

            double[] cellRanks = PercentileRanks(cellCounts);
            double[] greedyRanks = PercentileRanks(greedyRates);
            double[] nodeRanks = PercentileRanks(solverNodes);
            double[] degreeRanks = PercentileRanks(degrees);

            var rawScores = new double[count];

            for (int i = 0; i < count; i++)
            {
                rawScores[i] = (cellRanks[i] * weights.CellCount +
                                greedyRanks[i] * weights.GreedyFailureRate +
                                nodeRanks[i] * weights.SolverNodes +
                                degreeRanks[i] * weights.AverageDegree) / weights.Total;
            }

            // Ranking the blend again is what makes the result usable downstream. The
            // blend of four uniform rankings is not itself uniform, so both the ten bands
            // and the chapter curve would be fed a distribution with compressed tails.
            double[] finalRanks = PercentileRanks(rawScores);

            for (int i = 0; i < count; i++)
            {
                scored.Add(new ScoredLevel(levels[i], features[i], finalRanks[i], ToDifficulty(finalRanks[i])));
            }

            return scored;
        }

        /// <summary>
        /// Maps a rank in [0, 1] onto the ten bands. Ties in the underlying signal keep
        /// the same rank and therefore land in the same band.
        /// </summary>
        public static int ToDifficulty(double percentileRank)
        {
            int band = MinDifficulty + (int)(percentileRank * MaxDifficulty);
            return band > MaxDifficulty ? MaxDifficulty : band < MinDifficulty ? MinDifficulty : band;
        }

        /// <summary>
        /// Fractional rank of every value within the set, where equal values share the
        /// average of the positions they span. A single element ranks at zero, which
        /// keeps a one level corpus at difficulty 1 instead of dividing by zero.
        /// </summary>
        public static double[] PercentileRanks(double[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            int count = values.Length;
            var ranks = new double[count];

            if (count <= 1)
            {
                return ranks;
            }

            var order = new int[count];

            for (int i = 0; i < count; i++)
            {
                order[i] = i;
            }

            Array.Sort(order, (a, b) => values[a].CompareTo(values[b]));

            int position = 0;

            while (position < count)
            {
                int tieEnd = position;

                while (tieEnd + 1 < count && values[order[tieEnd + 1]] == values[order[position]])
                {
                    tieEnd++;
                }

                double sharedRank = (position + tieEnd) / 2.0 / (count - 1);

                for (int i = position; i <= tieEnd; i++)
                {
                    ranks[order[i]] = sharedRank;
                }

                position = tieEnd + 1;
            }

            return ranks;
        }
    }
}
