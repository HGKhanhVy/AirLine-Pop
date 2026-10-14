using System;
using System.Collections.Generic;

namespace AirlinePop.SingleLine.Core
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
        private static readonly WindCell[] EmptyWind = new WindCell[0];
        private static readonly CellMark[] EmptyMarks = new CellMark[0];

        private readonly bool[] activeMask;
        private readonly int[] declaredActiveCells;
        private readonly int[] solution;
        private readonly string[] tags;
        private readonly WindCell[] wind;

        // Per square: the wind's direction plus one, or zero for still air.
        private readonly byte[] windByCell;

        private readonly CellMark[] marks;
        private readonly CellKind[] kindByCell;

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

        public IReadOnlyList<WindCell> Wind => wind;

        public bool HasWind => wind.Length > 0;

        /// <summary>Clouds, beacons and overpasses, see <see cref="CellKind"/>.</summary>
        public IReadOnlyList<CellMark> Marks => marks;

        /// <summary>Overpasses on the board; each one adds a second pass to the route.</summary>
        public int BridgeCount { get; }

        /// <summary>A night flight: tagged, since it changes only what the player sees.</summary>
        public bool IsNight { get; }

        /// <summary>
        /// A formation flight: the player flies the left half and a <see cref="Wingman"/> the right,
        /// making the mirror of every move and waiting wherever its own half blocks it.
        /// </summary>
        public bool IsFormation { get; }

        /// <summary>Squares on the player's half of a formation board; every square otherwise.</summary>
        public int PlayerSideCellCount { get; }

        /// <summary>Squares on the wingman's half of a formation board; none otherwise.</summary>
        public int WingSideCellCount { get; }

        /// <summary>The VIP flight closing a chapter.</summary>
        public bool IsVip { get; }

        /// <summary>The storm flight of a city, see <see cref="LevelTags.Storm"/>.</summary>
        public bool IsStorm { get; }

        /// <summary>
        /// Squares the player's own route visits to win: every square, or half of them in a
        /// formation flight, where the wingman covers the other half.
        /// </summary>
        public int PathLength => PlayerSideCellCount + BridgeCount;

        /// <summary>Passes over squares needed to win, both planes counted: every square once, overpasses twice.</summary>
        public int CoverageTarget => ActiveCellCount + BridgeCount;

        /// <summary>The extra conditions this level sets, for screens that explain them.</summary>
        public LevelRule Rules =>
            (HasFixedEnd ? LevelRule.Runway : LevelRule.None) |
            (HasWind ? LevelRule.Wind : LevelRule.None) |
            (IsNight ? LevelRule.Night : LevelRule.None) |
            (IsFormation ? LevelRule.Formation : LevelRule.None) |
            (IsVip ? LevelRule.Vip : LevelRule.None) |
            (IsStorm ? LevelRule.Storm : LevelRule.None) |
            MarkRules();

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
            string[] tags = null,
            WindCell[] wind = null,
            CellMark[] marks = null)
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
            IsNight = Array.IndexOf(this.tags, LevelTags.Night) >= 0;
            IsFormation = Array.IndexOf(this.tags, LevelTags.Formation) >= 0;
            IsVip = Array.IndexOf(this.tags, LevelTags.Vip) >= 0;
            IsStorm = Array.IndexOf(this.tags, LevelTags.Storm) >= 0;

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

            for (int cell = 0; cell < activeMask.Length; cell++)
            {
                if (activeMask[cell] && IsOnWingSide(cell))
                {
                    WingSideCellCount++;
                }
            }

            PlayerSideCellCount = ActiveCellCount - WingSideCellCount;

            this.wind = wind == null ? EmptyWind : (WindCell[])wind.Clone();
            windByCell = new byte[grid.CellCount];

            for (int i = 0; i < this.wind.Length; i++)
            {
                WindCell gust = this.wind[i];

                if (!grid.Contains(gust.Cell))
                {
                    throw new ArgumentOutOfRangeException(nameof(wind), gust.Cell, "Wind lies outside the grid of level " + id + ".");
                }

                windByCell[gust.Cell] = (byte)((int)gust.Direction + 1);
            }

            this.marks = marks == null ? EmptyMarks : (CellMark[])marks.Clone();
            kindByCell = new CellKind[grid.CellCount];

            for (int i = 0; i < this.marks.Length; i++)
            {
                CellMark mark = this.marks[i];

                if (!grid.Contains(mark.Cell))
                {
                    throw new ArgumentOutOfRangeException(nameof(marks), mark.Cell, "Mark lies outside the grid of level " + id + ".");
                }

                if (mark.Kind == CellKind.Bridge && kindByCell[mark.Cell] != CellKind.Bridge)
                {
                    BridgeCount++;
                }

                kindByCell[mark.Cell] = mark.Kind;
            }
        }

        /// <summary>What the square asks of the plane; plain for most.</summary>
        public CellKind KindOf(int cell)
        {
            return cell >= 0 && cell < kindByCell.Length ? kindByCell[cell] : CellKind.Plain;
        }

        /// <summary>How many times the route passes over the square: twice for an overpass, once otherwise.</summary>
        public int CapacityOf(int cell)
        {
            return KindOf(cell) == CellKind.Bridge ? 2 : 1;
        }

        private LevelRule MarkRules()
        {
            LevelRule rules = LevelRule.None;

            for (int i = 0; i < marks.Length; i++)
            {
                switch (marks[i].Kind)
                {
                    case CellKind.Straight:
                        rules |= LevelRule.Turbulence;
                        break;
                    case CellKind.Turn:
                        rules |= LevelRule.Beacon;
                        break;
                    case CellKind.Bridge:
                        rules |= LevelRule.Bridge;
                        break;
                }
            }

            return rules;
        }

        /// <summary>True when the square carries wind, and which way it blows.</summary>
        public bool TryGetWind(int cell, out Direction direction)
        {
            int stored = cell >= 0 && cell < windByCell.Length ? windByCell[cell] : 0;
            direction = stored == 0 ? Direction.Up : (Direction)(stored - 1);
            return stored != 0;
        }

        /// <summary>Copy of this level with its runway and wind replaced; everything else carries over.</summary>
        public LevelData WithRules(int fixedEnd, WindCell[] newWind)
        {
            return new LevelData(Id, Version, Grid, declaredActiveCells, FixedStart, fixedEnd, solution, Difficulty, ThemeId, tags,
                newWind, marks);
        }

        /// <summary>Copy of this level with its clouds, beacons and overpasses replaced; everything else carries over.</summary>
        public LevelData WithMarks(CellMark[] newMarks)
        {
            return new LevelData(Id, Version, Grid, declaredActiveCells, FixedStart, FixedEnd, solution, Difficulty, ThemeId, tags, wind,
                newMarks);
        }

        /// <summary>True for a square on the wingman's half, the right half, of a formation board.</summary>
        public bool IsOnWingSide(int cell)
        {
            return IsFormation && Grid.ToColumn(cell) >= Grid.Width / 2;
        }

        /// <summary>The square mirroring this one across the board's middle column, where a formation's wingman starts.</summary>
        public int MirrorOf(int cell)
        {
            return Grid.ToIndex(Grid.ToRow(cell), Grid.Width - 1 - Grid.ToColumn(cell));
        }

        /// <summary>Copy of this level carrying different tags; everything else carries over.</summary>
        public LevelData WithTags(string[] newTags)
        {
            return new LevelData(Id, Version, Grid, declaredActiveCells, FixedStart, FixedEnd, solution, Difficulty, ThemeId, newTags,
                wind, marks);
        }

        public bool IsActive(int cell)
        {
            return cell >= 0 && cell < activeMask.Length && activeMask[cell];
        }

        /// <summary>
        /// Copy of this level under a new id and difficulty. The importer builds its own
        /// chapter ordering, so a level keeps its board but is renamed and re-rated on the
        /// way into a build; everything else carries over untouched.
        /// </summary>
        public LevelData WithIdentity(string id, int difficulty)
        {
            return new LevelData(
                id,
                Version,
                Grid,
                declaredActiveCells,
                FixedStart,
                FixedEnd,
                solution,
                difficulty,
                ThemeId,
                tags,
                wind,
                marks);
        }

        /// <summary>
        /// Copy of this level carrying a different solution. Source data ships plenty of
        /// levels with no solution at all and a few with a broken one, and both are
        /// replaced by a solver result before export rather than shipped as they are.
        /// </summary>
        public LevelData WithSolution(int[] newSolution)
        {
            return new LevelData(
                Id,
                Version,
                Grid,
                declaredActiveCells,
                FixedStart,
                FixedEnd,
                newSolution,
                Difficulty,
                ThemeId,
                tags,
                wind,
                marks);
        }

        public override string ToString()
        {
            return $"{Id} ({Grid}, {ActiveCellCount} cells)";
        }
    }
}
