using System;
using System.Collections.Generic;

namespace ASTeams.SingleLine.Core
{
    /// <summary>
    /// Seats the cats a flight picks up.
    ///
    /// Every square has to be visited to win, so a cat on any square is always reachable:
    /// seating cats never changes whether a level can be solved, and the 300 shipped
    /// levels need no new data. The plan is derived from the level id, so it is the same
    /// on every visit and on every device.
    ///
    /// Cats are spread out rather than dropped at random: each next seat is the free
    /// square furthest from the seats taken so far (and from the start), so pickups are
    /// paced along the route instead of bunching up.
    /// </summary>
    public static class PassengerPlanner
    {
        public const int MinPassengers = 2;
        public const int MaxPassengers = 4;

        /// <summary>One cat per this many squares, within the bounds above.</summary>
        public const int SquaresPerPassenger = 7;

        public static PassengerPlan Plan(LevelData level)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            var candidates = new List<int>(level.ActiveCellCount);

            for (int cell = 0; cell < level.Grid.CellCount; cell++)
            {
                if (level.IsActive(cell) && cell != level.FixedStart)
                {
                    candidates.Add(cell);
                }
            }

            int seed = StableHash(level.Id);
            int wanted = Math.Max(MinPassengers, Math.Min(MaxPassengers, level.ActiveCellCount / SquaresPerPassenger));
            int count = Math.Min(wanted, candidates.Count);

            if (count <= 0)
            {
                return new PassengerPlan(Array.Empty<int>(), seed);
            }

            var random = new Random(seed);
            Shuffle(candidates, random);

            var seats = new int[count];
            var anchors = new List<int>(count + 1);

            if (level.HasFixedStart)
            {
                anchors.Add(level.FixedStart);
            }

            for (int i = 0; i < count; i++)
            {
                int pick = anchors.Count == 0 ? candidates[0] : Furthest(level.Grid, candidates, anchors);
                seats[i] = pick;
                anchors.Add(pick);
                candidates.Remove(pick);
            }

            return new PassengerPlan(seats, seed);
        }

        /// <summary>
        /// The candidate whose nearest anchor is furthest away. Ties go to the earlier
        /// candidate, and the candidates were shuffled by the level's seed, so ties still
        /// vary from level to level.
        /// </summary>
        private static int Furthest(Grid grid, List<int> candidates, List<int> anchors)
        {
            int best = candidates[0];
            int bestDistance = -1;

            for (int i = 0; i < candidates.Count; i++)
            {
                int nearest = int.MaxValue;

                for (int a = 0; a < anchors.Count; a++)
                {
                    nearest = Math.Min(nearest, Distance(grid, candidates[i], anchors[a]));
                }

                if (nearest > bestDistance)
                {
                    bestDistance = nearest;
                    best = candidates[i];
                }
            }

            return best;
        }

        private static int Distance(Grid grid, int a, int b)
        {
            return Math.Abs(grid.ToRow(a) - grid.ToRow(b)) + Math.Abs(grid.ToColumn(a) - grid.ToColumn(b));
        }

        private static void Shuffle(List<int> items, Random random)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (items[i], items[j]) = (items[j], items[i]);
            }
        }

        /// <summary>
        /// FNV-1a over the id. string.GetHashCode is randomised per process on modern
        /// runtimes, which would reseat the cats every launch.
        /// </summary>
        private static int StableHash(string text)
        {
            unchecked
            {
                uint hash = 2166136261;

                for (int i = 0; i < text.Length; i++)
                {
                    hash ^= text[i];
                    hash *= 16777619;
                }

                return (int)(hash & 0x7FFFFFFF);
            }
        }
    }
}
