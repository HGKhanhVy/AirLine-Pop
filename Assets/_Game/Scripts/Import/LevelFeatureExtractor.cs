using System;
using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// Measures one level. Separated from the scoring so the five signals can be
    /// inspected and tested on their own, and so re-weighting never means re-measuring.
    /// </summary>
    public sealed class LevelFeatureExtractor
    {
        private readonly ILevelSolver solver;
        private readonly GreedyPlayer player;
        private readonly CautiousPlayer carefulPlayer;

        private int[] solutionBuffer;

        public LevelFeatureExtractor(ILevelSolver solver)
        {
            this.solver = solver ?? throw new ArgumentNullException(nameof(solver));
            player = new GreedyPlayer();
            carefulPlayer = new CautiousPlayer();
        }

        public LevelFeatures Extract(LevelData level)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (level.ActiveCellCount == 0)
            {
                return new LevelFeatures(0, 0, 0, GreedyPlayer.StrategyCount, false);
            }

            if (solutionBuffer == null || solutionBuffer.Length < level.ActiveCellCount)
            {
                solutionBuffer = new int[level.ActiveCellCount];
            }

            bool solvable = level.HasFixedStart
                ? solver.TrySolve(level, level.FixedStart, solutionBuffer, out int _)
                : solver.TrySolveAny(level, solutionBuffer, out int _, out int _);

            return new LevelFeatures(
                level.ActiveCellCount,
                MeasureAverageDegree(level),
                solver.LastNodeCount,
                player.CountFailures(level),
                solvable,
                carefulPlayer.MeasureSteps(level));
        }

        /// <summary>Mean count of active orthogonal neighbours over the active cells.</summary>
        private static double MeasureAverageDegree(LevelData level)
        {
            Grid grid = level.Grid;
            int total = 0;
            int counted = 0;

            for (int cell = 0; cell < grid.CellCount; cell++)
            {
                if (!level.IsActive(cell))
                {
                    continue;
                }

                counted++;

                for (int direction = 0; direction < Grid.NeighborCount; direction++)
                {
                    if (grid.TryGetNeighbor(cell, (Direction)direction, out int neighbor) && level.IsActive(neighbor))
                    {
                        total++;
                    }
                }
            }

            return counted == 0 ? 0 : total / (double)counted;
        }
    }
}
