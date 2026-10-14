using System;
using AirlinePop.SingleLine.Core;

namespace AirlinePop.SingleLine.Import
{
    /// <summary>
    /// Plays a level the way an attentive person does and counts the squares entered on the
    /// way to the answer, wrong turns included. Turns are picked at random, every rule of the
    /// level is obeyed, and a move is undone as soon as the board shows it cannot work: the
    /// squares left over split into two, or more than one of them can only be reached from one
    /// side and so would all have to end the route.
    ///
    /// Those two checks are what separates this from <see cref="CautiousPlayer"/>. People see a
    /// board cut in half at a glance, so an open board that only punishes blind wandering
    /// measures easy here, while a board whose traps only show several moves later still sends
    /// the player back again and again.
    /// </summary>
    public sealed class ThoughtfulPlayer
    {
        public const int DefaultPlays = 15;
        public const int DefaultStepBudget = 20000;

        private readonly int plays;
        private readonly int stepBudget;

        private LevelData level;
        private byte[] visits;
        private int[] path;
        private int[] wingPath;
        private int wingCovered;
        private int[] candidates;
        private int[] floodStack;
        private int[] floodStamp;
        private int floodGeneration;
        private Random random;
        private int steps;

        public ThoughtfulPlayer(int plays = DefaultPlays, int stepBudget = DefaultStepBudget)
        {
            this.plays = Math.Max(1, plays);
            this.stepBudget = Math.Max(1, stepBudget);
        }

        /// <summary>
        /// Squares entered per square of the route, over the median play: 1 for a board solved
        /// without a single wrong turn, more the more it misleads. A play that runs out of budget
        /// counts as the budget.
        /// </summary>
        public double MeasureTrials(LevelData target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (target.ActiveCellCount == 0)
            {
                return 1;
            }

            Prepare(target);
            int start = target.HasFixedStart ? target.FixedStart : target.DeclaredActiveCells[0];
            var results = new int[plays];
            int seed = BoardHash(target);

            for (int play = 0; play < plays; play++)
            {
                Array.Clear(visits, 0, visits.Length);
                random = new Random(seed + play * 7919);
                steps = 0;
                PathRules.Visit(level, visits, start, true);
                path[0] = start;
                wingCovered = 0;

                if (level.IsFormation)
                {
                    wingPath[0] = Wingman.StartFor(level, start);
                    PathRules.Visit(level, visits, wingPath[0], true);
                    wingCovered = 1;
                }

                bool finished = Play(0);
                results[play] = finished ? steps : stepBudget;
            }

            Array.Sort(results);
            return results[plays / 2] / (double)target.PathLength;
        }

        private void Prepare(LevelData target)
        {
            level = target;
            int cellCount = target.Grid.CellCount;
            visits = new byte[cellCount];
            path = new int[target.PathLength];
            wingPath = new int[target.PathLength];
            candidates = new int[target.PathLength * Grid.NeighborCount];
            floodStack = new int[cellCount * 2];
            floodStamp = new int[cellCount];
            floodGeneration = 0;
        }

        private bool Play(int depth)
        {
            steps++;

            if (depth + 1 == level.PathLength)
            {
                return PathRules.IsComplete(level, depth + 1, path[depth], wingCovered);
            }

            if (steps >= stepBudget)
            {
                return false;
            }

            int head = path[depth];
            int previous = depth > 0 ? path[depth - 1] : LevelData.NoCell;
            int baseIndex = depth * Grid.NeighborCount;
            int count = Collect(previous, head, baseIndex);
            Shuffle(baseIndex, count);

            for (int i = 0; i < count; i++)
            {
                int next = candidates[baseIndex + i];
                bool wingMoved = level.IsFormation && FlyWingman(depth, head, next);
                PathRules.Visit(level, visits, next, true);
                path[depth + 1] = next;

                if (LooksSolvable(next, depth + 2) && Play(depth + 1))
                {
                    return true;
                }

                PathRules.Visit(level, visits, next, false);

                if (wingMoved)
                {
                    PathRules.Visit(level, visits, wingPath[depth + 1], false);
                    wingCovered--;
                }

                if (steps >= stepBudget)
                {
                    return false;
                }
            }

            return false;
        }

        private bool FlyWingman(int depth, int head, int next)
        {
            int from = wingPath[depth];
            int wing = Wingman.Step(level, visits, from, head, next);
            wingPath[depth + 1] = wing;

            if (wing == from)
            {
                return false;
            }

            PathRules.Visit(level, visits, wing, true);
            wingCovered++;
            return true;
        }

        private int Collect(int previous, int head, int baseIndex)
        {
            Grid grid = level.Grid;
            int count = 0;

            for (int direction = 0; direction < Grid.NeighborCount; direction++)
            {
                if (grid.TryGetNeighbor(head, (Direction)direction, out int next) && level.IsActive(next) && IsOnPlayerSide(next) &&
                    PathRules.IsOpen(level, visits, next) && PathRules.CanLeave(level, previous, head, next))
                {
                    candidates[baseIndex + count] = next;
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// What a person sees without thinking ahead: the open squares must still hang together
        /// from the head, and at most one of them may be a dead end, which then has to be where
        /// the route finishes. In formation the flood keeps to the player's half, and the wingman
        /// cannot have more of its half left than the player has steps left.
        /// </summary>
        private bool LooksSolvable(int head, int pathLength)
        {
            int remaining = level.PathLength - pathLength;

            if (level.IsFormation && level.WingSideCellCount - wingCovered > remaining)
            {
                return false;
            }

            if (remaining <= 0)
            {
                return true;
            }

            Grid grid = level.Grid;
            floodGeneration++;
            int top = 0;
            int deadEnds = 0;

            // The head is never flooded into, but an overpass crossed once still owes its second pass.
            int reached = level.CapacityOf(head) - visits[head];

            for (int direction = 0; direction < Grid.NeighborCount; direction++)
            {
                if (grid.TryGetNeighbor(head, (Direction)direction, out int neighbor) && IsOnPlayerSide(neighbor) &&
                    level.IsActive(neighbor) && PathRules.IsOpen(level, visits, neighbor) && floodStamp[neighbor] != floodGeneration)
                {
                    floodStamp[neighbor] = floodGeneration;
                    floodStack[top++] = neighbor;
                }
            }

            while (top > 0)
            {
                int cell = floodStack[--top];
                reached += level.CapacityOf(cell) - visits[cell];
                int exits = 0;

                for (int direction = 0; direction < Grid.NeighborCount; direction++)
                {
                    if (!grid.TryGetNeighbor(cell, (Direction)direction, out int neighbor) || !level.IsActive(neighbor) ||
                        !IsOnPlayerSide(neighbor))
                    {
                        continue;
                    }

                    if (neighbor == head)
                    {
                        exits++;
                        continue;
                    }

                    if (!PathRules.IsOpen(level, visits, neighbor))
                    {
                        continue;
                    }

                    exits++;

                    if (floodStamp[neighbor] != floodGeneration)
                    {
                        floodStamp[neighbor] = floodGeneration;
                        floodStack[top++] = neighbor;
                    }
                }

                if (exits <= 1 && level.CapacityOf(cell) == 1)
                {
                    deadEnds++;

                    if (deadEnds > 1 || (level.HasFixedEnd && cell != level.FixedEnd))
                    {
                        return false;
                    }
                }
            }

            return reached == remaining;
        }

        /// <summary>The player's own route keeps to the left half of a formation board.</summary>
        private bool IsOnPlayerSide(int cell)
        {
            return !level.IsFormation || level.Grid.ToColumn(cell) < level.Grid.Width / 2;
        }

        private void Shuffle(int baseIndex, int count)
        {
            for (int i = count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                int swap = candidates[baseIndex + i];
                candidates[baseIndex + i] = candidates[baseIndex + j];
                candidates[baseIndex + j] = swap;
            }
        }

        /// <summary>
        /// Seeds the plays from the board itself rather than its name, so a board measures the
        /// same before and after a build renames it.
        /// </summary>
        private static int BoardHash(LevelData level)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + level.Grid.Width;
                hash = hash * 31 + level.Grid.Height;
                hash = hash * 31 + level.FixedStart;
                hash = hash * 31 + level.FixedEnd;

                for (int i = 0; i < level.DeclaredActiveCells.Count; i++)
                {
                    hash = hash * 31 + level.DeclaredActiveCells[i];
                }

                for (int i = 0; i < level.Marks.Count; i++)
                {
                    hash = hash * 31 + level.Marks[i].Cell * 4 + (int)level.Marks[i].Kind;
                }

                for (int i = 0; i < level.Wind.Count; i++)
                {
                    hash = hash * 31 + level.Wind[i].Cell * 4 + (int)level.Wind[i].Direction;
                }

                return hash & 0x7FFFFFFF;
            }
        }
    }
}
