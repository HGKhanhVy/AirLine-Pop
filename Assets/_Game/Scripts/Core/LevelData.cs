using System;
using System.Collections.Generic;

namespace ASTeams.SingleLine.Core
{
    /// <summary>
    /// Immutable description of one level, matching the authoring schema in GDD 6.3.
    /// Structural correctness beyond what the constructor needs to build its lookup
    /// mask (connectivity, solvability, a well formed solution) is not checked here;
    /// that is the single responsibility of <see cref="LevelValidator"/>.
    /// </summary>
    public sealed class LevelData
    {
        /// <summary>
        /// Sentinel for an unset cell reference. The GDD models fixedStart/fixedEnd as
        /// nullable ints; a sentinel keeps the rule checks free of nullable unwrapping
        /// on the per move hot path.
        /// </summary>
        public const int NoCell = -1;

        private static readonly int[] EmptyCells = new int[0];
        private static readonly string[] EmptyTags = new string[0];

        private readonly bool[] activeMask;
        private readonly int[] declaredActiveCells;
        private readonly int[] solution;
        private readonly string[] tags;

        public string Id { get; }

        public int Version { get; }

        public Grid Grid { get; }

        /// <summary>
        /// Number of distinct active cells, derived from the lookup mask rather than
        /// from the declared array length, so a level file containing a duplicated
        /// index still produces a correct win condition. The validator reports the
        /// duplicate separately by comparing this against the declared array.
        /// </summary>
        public int ActiveCellCount { get; }

        public IReadOnlyList<int> DeclaredActiveCells => declaredActiveCells;

        public int FixedStart { get; }

        public int FixedEnd { get; }

        public bool HasFixedStart => FixedStart != NoCell;

        public bool HasFixedEnd => FixedEnd != NoCell;

        public IReadOnlyList<int> Solution => solution;

        public bool HasSolution => solution.Length > 0;

        public int Difficulty { get; }

        public string ThemeId { get; }

        public IReadOnlyList<string> Tags => tags;

        public LevelData(
            string id,
            int version,
            Grid grid,
            int[] activeCells,
            int fixedStart = NoCell,
            int fixedEnd = NoCell,
            int[] solution = null,
            int difficulty = 1,
            string themeId = null,
            string[] tags = null)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Level id must not be empty.", nameof(id));
            }

            if (activeCells == null)
            {
                throw new ArgumentNullException(nameof(activeCells));
            }

            Id = id;
            Version = version;
            Grid = grid;
            FixedStart = fixedStart;
            FixedEnd = fixedEnd;
            Difficulty = difficulty;
            ThemeId = themeId;

            // Defensive copies: the caller owns the arrays it passed in and may reuse
            // its parse buffers, but a level must stay immutable once constructed.
            declaredActiveCells = (int[])activeCells.Clone();
            this.solution = solution == null ? EmptyCells : (int[])solution.Clone();
            this.tags = tags == null ? EmptyTags : (string[])tags.Clone();

            activeMask = new bool[grid.CellCount];
            int distinctCount = 0;

            for (int i = 0; i < declaredActiveCells.Length; i++)
            {
                int cell = declaredActiveCells[i];

                if (!grid.Contains(cell))
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(activeCells),
                        cell,
                        "Active cell lies outside the grid of level " + id + ".");
                }

                if (activeMask[cell])
                {
                    continue;
                }

                activeMask[cell] = true;
                distinctCount++;
            }

            ActiveCellCount = distinctCount;
        }

        public bool IsActive(int cell)
        {
            return cell >= 0 && cell < activeMask.Length && activeMask[cell];
        }

        public override string ToString()
        {
            return $"{Id} ({Grid}, {ActiveCellCount} cells)";
        }
    }
}
