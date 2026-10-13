using System;
using System.Collections.Generic;
using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// Draws new boards by flying a random route over an empty grid and keeping the squares it
    /// covered. The route is the board's solution, so every board is solvable by construction,
    /// and the squares it skipped become the holes that give a board its shape.
    ///
    /// The route leans towards squares with few free neighbours, the way a person sweeps a
    /// corner before it gets cut off, with enough noise to wander. With overpasses enabled it may
    /// also fly straight across a stretch of its own route, which turns that square into one.
    /// </summary>
    public sealed class BoardGenerator
    {
        private readonly BoardGeneratorSettings settings;

        public BoardGenerator(BoardGeneratorSettings settings)
        {
            this.settings = settings;
        }

        /// <summary>A new board, or null when this attempt covered too little of the grid.</summary>
        public LevelData TryGenerate(string id, Random random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            var grid = new Grid(settings.Width, settings.Height);
            var walk = new Walk(grid, random, settings);

            if (!walk.Run())
            {
                return null;
            }

            int cells = walk.CoveredCount;

            if (cells < settings.MinCells || cells > settings.MaxCells || walk.BridgeCount < settings.MinBridges)
            {
                return null;
            }

            int[] route = walk.Route.ToArray();

            // Either end of the route may be its start; the walk ends where it got stuck, which
            // is often the more interesting place to begin.
            if (random.Next(2) == 0)
            {
                Array.Reverse(route);
            }

            var active = new List<int>(cells);

            for (int cell = 0; cell < grid.CellCount; cell++)
            {
                if (walk.IsCovered(cell))
                {
                    active.Add(cell);
                }
            }

            return new LevelData(id, 1, grid, active.ToArray(), route[0], solution: route, marks: walk.BridgeMarks());
        }

        /// <summary>One random flight over the grid, with its own scratch state.</summary>
        private sealed class Walk
        {
            private readonly Grid grid;
            private readonly Random random;
            private readonly BoardGeneratorSettings settings;
            private readonly bool[] covered;
            private readonly bool[] isBridge;

            // Per square: the axis the route crossed it on, 1 across, 2 down, 0 for a turn or unknown.
            private readonly byte[] straightAxis;
            private readonly int[] options = new int[Grid.NeighborCount];
            private readonly int[] weights = new int[Grid.NeighborCount];

            public List<int> Route { get; } = new List<int>();

            public int CoveredCount { get; private set; }

            public int BridgeCount { get; private set; }

            public Walk(Grid grid, Random random, BoardGeneratorSettings settings)
            {
                this.grid = grid;
                this.random = random;
                this.settings = settings;
                covered = new bool[grid.CellCount];
                isBridge = new bool[grid.CellCount];
                straightAxis = new byte[grid.CellCount];
            }

            public bool IsCovered(int cell)
            {
                return covered[cell];
            }

            public CellMark[] BridgeMarks()
            {
                var marks = new List<CellMark>(BridgeCount);

                for (int cell = 0; cell < isBridge.Length; cell++)
                {
                    if (isBridge[cell])
                    {
                        marks.Add(new CellMark(cell, CellKind.Bridge));
                    }
                }

                return marks.Count == 0 ? null : marks.ToArray();
            }

            public bool Run()
            {
                int head = random.Next(grid.CellCount);
                Enter(head);
                int target = (int)Math.Round(grid.CellCount * settings.FillTarget);

                while (CoveredCount < target)
                {
                    if (BridgeCount < settings.MaxBridges && random.NextDouble() < settings.BridgeChance && TryCross(ref head))
                    {
                        continue;
                    }

                    int count = CollectOptions(head);

                    if (count == 0)
                    {
                        break;
                    }

                    head = Pick(count);
                    Enter(head);
                }

                return CoveredCount >= settings.MinCells;
            }

            private void Enter(int cell)
            {
                int length = Route.Count;

                if (length >= 2)
                {
                    int before = Route[length - 2];
                    int middle = Route[length - 1];

                    if (!isBridge[middle] && middle - before == cell - middle)
                    {
                        straightAxis[middle] = AxisOf(middle, cell);
                    }
                }

                Route.Add(cell);

                if (!covered[cell])
                {
                    covered[cell] = true;
                    CoveredCount++;
                }
            }

            /// <summary>
            /// Flies over a square the route already crossed straight, at right angles, landing on
            /// the free square beyond. The crossed square becomes an overpass: its four neighbours
            /// are all on the board, and no other overpass touches it.
            /// </summary>
            private bool TryCross(ref int head)
            {
                for (int turn = 0; turn < Grid.NeighborCount; turn++)
                {
                    var direction = (Direction)((turn + random.Next(Grid.NeighborCount)) % Grid.NeighborCount);

                    if (!grid.TryGetNeighbor(head, direction, out int over) || !covered[over] || isBridge[over] ||
                        straightAxis[over] == 0 || straightAxis[over] == AxisOf(head, over) || TouchesBridge(over) ||
                        Route[0] == over || Route[Route.Count - 1] == over)
                    {
                        continue;
                    }

                    if (!grid.TryGetNeighbor(over, direction, out int beyond) || covered[beyond])
                    {
                        continue;
                    }

                    isBridge[over] = true;
                    BridgeCount++;
                    Route.Add(over);
                    Enter(beyond);
                    head = beyond;
                    return true;
                }

                return false;
            }

            private bool TouchesBridge(int cell)
            {
                for (int direction = 0; direction < Grid.NeighborCount; direction++)
                {
                    if (grid.TryGetNeighbor(cell, (Direction)direction, out int neighbour) && isBridge[neighbour])
                    {
                        return true;
                    }
                }

                return false;
            }

            private int CollectOptions(int head)
            {
                int count = 0;

                for (int direction = 0; direction < Grid.NeighborCount; direction++)
                {
                    if (grid.TryGetNeighbor(head, (Direction)direction, out int next) && !covered[next])
                    {
                        options[count] = next;
                        weights[count] = Weight(next);
                        count++;
                    }
                }

                return count;
            }

            /// <summary>Squares with fewer free neighbours weigh more, so corners get swept before they are cut off.</summary>
            private int Weight(int cell)
            {
                int free = 0;

                for (int direction = 0; direction < Grid.NeighborCount; direction++)
                {
                    if (grid.TryGetNeighbor(cell, (Direction)direction, out int neighbour) && !covered[neighbour])
                    {
                        free++;
                    }
                }

                return 1 + (int)Math.Round(settings.CornerBias * (Grid.NeighborCount - free));
            }

            private int Pick(int count)
            {
                int total = 0;

                for (int i = 0; i < count; i++)
                {
                    total += weights[i];
                }

                int roll = random.Next(total);

                for (int i = 0; i < count; i++)
                {
                    roll -= weights[i];

                    if (roll < 0)
                    {
                        return options[i];
                    }
                }

                return options[count - 1];
            }

            private byte AxisOf(int from, int to)
            {
                return (byte)(grid.ToRow(from) == grid.ToRow(to) ? 1 : 2);
            }
        }
    }
}
