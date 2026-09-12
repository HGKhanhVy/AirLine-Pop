using System;
using System.Collections.Generic;
using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// The gate every level passes before it can be scored or shipped: it must have a
    /// solution that satisfies the rules.
    ///
    /// This matters more than it sounds. Of the ripped corpus, 357 files ship no solution
    /// at all because their pack has no tutorial data, and a further handful ship one that
    /// is the wrong length or jumps across the board. Exporting either would give the hint
    /// system nothing to show and would leave a level that may not even be solvable in the
    /// build.
    /// </summary>
    public sealed class LevelPreparer
    {
        private readonly LevelValidator validator;

        public LevelPreparer(LevelValidator validator)
        {
            this.validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public PreparedPool Prepare(IReadOnlyList<LevelData> levels)
        {
            if (levels == null)
            {
                throw new ArgumentNullException(nameof(levels));
            }

            var kept = new List<LevelData>(levels.Count);
            var dropped = new List<string>();
            int solved = 0;
            int repaired = 0;

            for (int i = 0; i < levels.Count; i++)
            {
                LevelData level = NormalizeActiveCells(levels[i]);
                LevelValidationResult result = validator.Validate(level);

                // The validator hands back the stored solution when it checked out, and a
                // freshly solved one otherwise. Either way a full length solution is proof
                // the board is playable; anything short means no solution exists or the
                // search gave up, and the level cannot ship.
                if (result.Solution.Count != level.ActiveCellCount)
                {
                    dropped.Add(level.Id);
                    continue;
                }

                if (!level.HasSolution)
                {
                    solved++;
                    kept.Add(level.WithSolution(ToArray(result.Solution)));
                    continue;
                }

                if (!ReferenceEquals(result.Solution, level.Solution))
                {
                    repaired++;
                    kept.Add(level.WithSolution(ToArray(result.Solution)));
                    continue;
                }

                if (!ReferenceEquals(level, levels[i]))
                {
                    repaired++;
                }

                kept.Add(level);
            }

            return new PreparedPool(kept, dropped, solved, repaired);
        }

        private static int[] ToArray(IReadOnlyList<int> source)
        {
            var copy = new int[source.Count];

            for (int i = 0; i < source.Count; i++)
            {
                copy[i] = source[i];
            }

            return copy;
        }

        private static LevelData NormalizeActiveCells(LevelData level)
        {
            if (level.DeclaredActiveCells.Count == level.ActiveCellCount)
            {
                return level;
            }

            var cells = new int[level.ActiveCellCount];
            var seen = new bool[level.Grid.CellCount];
            int count = 0;
            for (int i = 0; i < level.DeclaredActiveCells.Count; i++)
            {
                int cell = level.DeclaredActiveCells[i];
                if (!seen[cell])
                {
                    seen[cell] = true;
                    cells[count++] = cell;
                }
            }

            return new LevelData(level.Id, level.Version, level.Grid, cells, level.FixedStart,
                level.FixedEnd, ToArray(level.Solution), level.Difficulty, level.ThemeId, CopyTags(level.Tags));
        }

        private static string[] CopyTags(IReadOnlyList<string> tags)
        {
            var copy = new string[tags.Count];
            for (int i = 0; i < tags.Count; i++)
            {
                copy[i] = tags[i];
            }

            return copy;
        }
    }
}
