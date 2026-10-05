using System;
using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// Plays a level the way a careful person does, to measure how much trial and error it
    /// takes. Turns are chosen at random, but a square that the next step would cut off for
    /// good is never left behind: like a player, it goes there first, and two such squares at
    /// once mean a dead end it backs out of. Each square entered counts as a step.
    ///
    /// A board that is open but forgiving finishes in about one step per square; one full of
    /// traps sends the player back again and again. The typical count over several plays,
    /// each with its own fixed seed, is the measure, so the same board always scores the same.
    /// </summary>
    public sealed class CautiousPlayer
    {
        public const int DefaultPlays = 12;
        public const int DefaultStepBudget = 30000;

        private readonly int plays;
        private readonly int stepBudget;

        private LevelData level;
        private bool[] visited;
        private int[] candidates;
        private Random random;
        private int steps;

        public CautiousPlayer(int plays = DefaultPlays, int stepBudget = DefaultStepBudget)
        {
            this.plays = Math.Max(1, plays);
            this.stepBudget = Math.Max(1, stepBudget);
        }

        /// <summary>The median number of steps the plays took to finish, or the budget for those that ran out.</summary>
        public long MeasureSteps(LevelData target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (target.ActiveCellCount == 0)
            {
                return 0;
            }

            level = target;
            visited = new bool[target.Grid.CellCount];
            candidates = new int[target.ActiveCellCount * Grid.NeighborCount];
            int start = target.HasFixedStart ? target.FixedStart : target.DeclaredActiveCells[0];
            var results = new long[plays];
            int seed = StableHash(target.Id);

            for (int play = 0; play < plays; play++)
            {
                Array.Clear(visited, 0, visited.Length);
                random = new Random(seed + play * 7919);
                steps = 0;
                visited[start] = true;
                bool finished = Play(start, 1);
                results[play] = finished ? steps : stepBudget;
            }

            Array.Sort(results);
            return results[plays / 2];
        }

        /// <summary>True when the path from <paramref name="head"/> covers the board; false on a dead end or out of budget.</summary>
        private bool Play(int head, int covered)
        {
            steps++;

            if (covered == level.ActiveCellCount)
            {
                return true;
            }

            if (steps >= stepBudget)
            {
                return false;
            }

            int baseIndex = (covered - 1) * Grid.NeighborCount;
            int count = Collect(head, baseIndex, covered);

            if (count < 0)
            {
                return false;
            }

            Shuffle(baseIndex, count);

            for (int i = 0; i < count; i++)
            {
                int next = candidates[baseIndex + i];
                visited[next] = true;
                bool done = Play(next, covered + 1);
                visited[next] = false;

                if (done)
                {
                    return true;
                }

                if (steps >= stepBudget)
                {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// The squares worth trying from the head: all open neighbours, or only the one that
        /// would otherwise be cut off. Returns -1 when two would be cut off, a certain dead end.
        /// </summary>
        private int Collect(int head, int baseIndex, int covered)
        {
            Grid grid = level.Grid;
            int count = 0;
            int forced = LevelData.NoCell;

            for (int direction = 0; direction < Grid.NeighborCount; direction++)
            {
                if (!grid.TryGetNeighbor(head, (Direction)direction, out int next) || !level.IsActive(next) || visited[next])
                {
                    continue;
                }

                // The last square may be a dead end: the path ends there anyway.
                if (covered + 1 < level.ActiveCellCount && OpenNeighbours(next, head) == 0)
                {
                    if (forced != LevelData.NoCell)
                    {
                        return -1;
                    }

                    forced = next;
                }

                candidates[baseIndex + count] = next;
                count++;
            }

            if (forced != LevelData.NoCell)
            {
                candidates[baseIndex] = forced;
                return 1;
            }

            return count;
        }

        /// <summary>Open neighbours a square would have once the head has moved on, the head itself not counted.</summary>
        private int OpenNeighbours(int cell, int head)
        {
            Grid grid = level.Grid;
            int count = 0;

            for (int direction = 0; direction < Grid.NeighborCount; direction++)
            {
                if (grid.TryGetNeighbor(cell, (Direction)direction, out int next) && next != head && level.IsActive(next) && !visited[next])
                {
                    count++;
                }
            }

            return count;
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

        private static int StableHash(string text)
        {
            unchecked
            {
                int hash = 17;

                for (int i = 0; i < text.Length; i++)
                {
                    hash = hash * 31 + text[i];
                }

                return hash & 0x7FFFFFFF;
            }
        }
    }
}
