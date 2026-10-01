using System;
using System.Collections.Generic;

namespace ASTeams.SingleLine.Core
{
    /// <summary>
    /// The cats waiting on one level's board: which squares they sit on, and a seed the
    /// view uses to dress them. Boarding needs no state of its own: a cat is aboard
    /// exactly when the path covers its square, so undo and restart put it back for free.
    /// </summary>
    public sealed class PassengerPlan
    {
        public static readonly PassengerPlan Empty = new PassengerPlan(Array.Empty<int>(), 0);

        private readonly int[] cells;

        public PassengerPlan(int[] cells, int seed)
        {
            this.cells = cells ?? throw new ArgumentNullException(nameof(cells));
            Seed = seed;
        }

        public IReadOnlyList<int> Cells => cells;

        public int Count => cells.Length;

        /// <summary>Stable per level, so the same cats sit in the same places on every visit.</summary>
        public int Seed { get; }

        public bool Contains(int cell)
        {
            return Array.IndexOf(cells, cell) >= 0;
        }
    }
}
