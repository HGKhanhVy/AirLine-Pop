using System;
using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// Plays a level through the real <see cref="PathSession"/> using one fixed
    /// strategy and never backtracking, then reports whether it reached a win.
    ///
    /// Driving the shipping rules rather than a private copy means the difficulty score
    /// can never drift away from what the game actually allows.
    /// </summary>
    public sealed class GreedyPlayer
    {
        /// <summary>How many strategies <see cref="CountFailures"/> tries.</summary>
        public const int StrategyCount = 6;

        public bool TryPlay(LevelData level, GreedyStrategy strategy)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (level.ActiveCellCount == 0)
            {
                return false;
            }

            var session = new PathSession(level);
            int start = level.HasFixedStart ? level.FixedStart : level.DeclaredActiveCells[0];

            if (session.Move(start) == MoveResult.Rejected)
            {
                return false;
            }

            while (session.State == PathState.Drawing)
            {
                int next = ChooseNext(level, session, strategy);

                if (next == LevelData.NoCell)
                {
                    break;
                }

                session.Move(next);
            }

            return session.State == PathState.Won;
        }

        /// <summary>
        /// Number of strategies that failed, from zero (every style wins) to
        /// <see cref="StrategyCount"/> (only real thought gets through).
        /// </summary>
        public int CountFailures(LevelData level)
        {
            int failures = 0;

            for (int i = 0; i < StrategyCount; i++)
            {
                if (!TryPlay(level, (GreedyStrategy)i))
                {
                    failures++;
                }
            }

            return failures;
        }

        private static int ChooseNext(LevelData level, PathSession session, GreedyStrategy strategy)
        {
            switch (strategy)
            {
                case GreedyStrategy.FewestOnwardMoves:
                    return ChooseByDegree(level, session, preferFewest: true);

                case GreedyStrategy.MostOnwardMoves:
                    return ChooseByDegree(level, session, preferFewest: false);

                default:
                    return ChooseByDirection(level, session, (int)strategy);
            }
        }

        /// <summary>
        /// Walks the four directions starting from <paramref name="firstDirection"/> and
        /// takes the first legal one, so each rotation is a different stubborn player.
        /// </summary>
        private static int ChooseByDirection(LevelData level, PathSession session, int firstDirection)
        {
            Grid grid = level.Grid;
            int head = session.Head;

            for (int step = 0; step < Grid.NeighborCount; step++)
            {
                var direction = (Direction)((firstDirection + step) % Grid.NeighborCount);

                if (!grid.TryGetNeighbor(head, direction, out int neighbor))
                {
                    continue;
                }

                if (level.IsActive(neighbor) && !session.IsVisited(neighbor))
                {
                    return neighbor;
                }
            }

            return LevelData.NoCell;
        }

        /// <summary>
        /// Ties break on direction order rather than arbitrarily, so the result does not
        /// depend on how the neighbours happened to be enumerated.
        /// </summary>
        private static int ChooseByDegree(LevelData level, PathSession session, bool preferFewest)
        {
            Grid grid = level.Grid;
            int head = session.Head;
            int best = LevelData.NoCell;
            int bestDegree = preferFewest ? int.MaxValue : int.MinValue;

            for (int direction = 0; direction < Grid.NeighborCount; direction++)
            {
                if (!grid.TryGetNeighbor(head, (Direction)direction, out int neighbor))
                {
                    continue;
                }

                if (!level.IsActive(neighbor) || session.IsVisited(neighbor))
                {
                    continue;
                }

                int degree = CountOnwardMoves(level, session, neighbor);
                bool better = preferFewest ? degree < bestDegree : degree > bestDegree;

                if (better)
                {
                    best = neighbor;
                    bestDegree = degree;
                }
            }

            return best;
        }

        private static int CountOnwardMoves(LevelData level, PathSession session, int cell)
        {
            Grid grid = level.Grid;
            int count = 0;

            for (int direction = 0; direction < Grid.NeighborCount; direction++)
            {
                if (!grid.TryGetNeighbor(cell, (Direction)direction, out int neighbor))
                {
                    continue;
                }

                if (level.IsActive(neighbor) && !session.IsVisited(neighbor))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
