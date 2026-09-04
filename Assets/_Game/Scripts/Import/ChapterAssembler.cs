using System;
using System.Collections.Generic;
using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// Lays scored levels onto the curve described by <see cref="ChapterLayout"/>.
    ///
    /// Each slot asks for a target difficulty and takes the unused level nearest to it,
    /// subject to two variety rules that stop the campaign from feeling repetitive or
    /// punishing. When no candidate satisfies the rules the nearest level is taken
    /// anyway and the relaxation is counted, because a silently reshaped curve would be
    /// worse than a reported one.
    ///
    /// The whole pass is deterministic. Level ids must not move between builds or saved
    /// progress would point at different puzzles.
    /// </summary>
    public sealed class ChapterAssembler
    {
        /// <summary>Beyond this many identical board sizes in a row the campaign reads as padding.</summary>
        private const int MaxSameSizeRun = 3;

        private readonly ChapterLayout layout;

        public ChapterAssembler()
            : this(ChapterLayout.Default)
        {
        }

        public ChapterAssembler(ChapterLayout layout)
        {
            this.layout = layout;
        }

        public Campaign Assemble(IReadOnlyList<ScoredLevel> pool)
        {
            if (pool == null)
            {
                throw new ArgumentNullException(nameof(pool));
            }

            List<ScoredLevel> sorted = SortByScoreThenId(pool);

            if (sorted.Count < layout.TotalLevels)
            {
                throw new ArgumentException(
                    "Need " + layout.TotalLevels + " levels but the pool holds only " + sorted.Count + ".",
                    nameof(pool));
            }

            var used = new bool[sorted.Count];
            var chapters = new List<Chapter>(layout.ChapterCount);
            var recentSizes = new List<string>(MaxSameSizeRun);
            int relaxed = 0;
            int placed = 0;

            for (int chapterIndex = 0; chapterIndex < layout.ChapterCount; chapterIndex++)
            {
                string chapterId = "ch" + (chapterIndex + 1).ToString("00");
                var placements = new List<PlacedLevel>(layout.LevelsPerChapter);

                for (int slot = 0; slot < layout.LevelsPerChapter; slot++)
                {
                    double target = layout.GetTarget(chapterIndex, slot);
                    int pick = FindNearest(sorted, used, target, recentSizes, respectVariety: true);

                    if (pick < 0)
                    {
                        pick = FindNearest(sorted, used, target, recentSizes, respectVariety: false);
                        relaxed++;
                    }

                    used[pick] = true;
                    ScoredLevel chosen = sorted[pick];

                    string levelId = chapterId + "_" + (slot + 1).ToString("000");
                    placements.Add(new PlacedLevel(
                        chosen.Level.WithIdentity(levelId, chosen.Difficulty),
                        chosen.Score,
                        target));

                    Remember(recentSizes, SizeKeyOf(chosen), MaxSameSizeRun);
                    placed++;
                }

                chapters.Add(new Chapter(chapterIndex + 1, chapterId, placements));
            }

            return new Campaign(chapters, placed, relaxed);
        }

        /// <summary>
        /// Walks outward from the target through the score-sorted pool, so the first
        /// acceptable level found is also the closest one.
        /// </summary>
        private static int FindNearest(
            List<ScoredLevel> sorted,
            bool[] used,
            double target,
            List<string> recentSizes,
            bool respectVariety)
        {
            int centre = LowerBound(sorted, target);
            int left = centre - 1;
            int right = centre;

            while (left >= 0 || right < sorted.Count)
            {
                bool takeRight;

                if (left < 0)
                {
                    takeRight = true;
                }
                else if (right >= sorted.Count)
                {
                    takeRight = false;
                }
                else
                {
                    double leftDistance = target - sorted[left].Score;
                    double rightDistance = sorted[right].Score - target;

                    // Ties go left, so the choice never depends on floating point noise.
                    takeRight = rightDistance < leftDistance;
                }

                int index = takeRight ? right++ : left--;

                if (used[index])
                {
                    continue;
                }

                if (respectVariety && !IsAcceptable(sorted[index], recentSizes))
                {
                    continue;
                }

                return index;
            }

            return -1;
        }

        /// <summary>
        /// Only the board size rule survives here. An earlier version also refused three
        /// levels in a row from the top fifth of the corpus, which sounded reasonable and
        /// is impossible: in chapters nine and ten every level is in the top fifth by
        /// construction, so the rule fired on every fourth slot and dragged the assembler
        /// far below its target, wrecking exactly the stretch it meant to protect. Relief
        /// is the rhythm's job, and the rhythm places rests relative to the local trend
        /// rather than against a fixed threshold.
        /// </summary>
        private static bool IsAcceptable(ScoredLevel candidate, List<string> recentSizes)
        {
            if (recentSizes.Count < MaxSameSizeRun)
            {
                return true;
            }

            string size = SizeKeyOf(candidate);

            for (int i = 0; i < recentSizes.Count; i++)
            {
                if (!string.Equals(recentSizes[i], size, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string SizeKeyOf(ScoredLevel level)
        {
            return level.Level.Grid.ToString();
        }

        private static void Remember<T>(List<T> history, T value, int capacity)
        {
            history.Add(value);

            if (history.Count > capacity)
            {
                history.RemoveAt(0);
            }
        }

        /// <summary>First index whose score is at least the target, or the end of the list.</summary>
        private static int LowerBound(List<ScoredLevel> sorted, double target)
        {
            int low = 0;
            int high = sorted.Count;

            while (low < high)
            {
                int middle = (low + high) / 2;

                if (sorted[middle].Score < target)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle;
                }
            }

            return low;
        }

        private static List<ScoredLevel> SortByScoreThenId(IReadOnlyList<ScoredLevel> pool)
        {
            var sorted = new List<ScoredLevel>(pool);

            sorted.Sort((a, b) =>
            {
                int byScore = a.Score.CompareTo(b.Score);
                return byScore != 0 ? byScore : string.CompareOrdinal(a.Level.Id, b.Level.Id);
            });

            return sorted;
        }
    }
}
