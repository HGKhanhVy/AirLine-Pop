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
    public sealed class ChapterAssembler : ICampaignAssembler
    {
        /// <summary>Beyond this many identical board sizes in a row the campaign reads as padding.</summary>
        private const int MaxSameSizeRun = 3;

        private readonly ChapterLayout layout;
        private readonly Dictionary<int, SpecialLevel> specials = new Dictionary<int, SpecialLevel>();

        public ChapterAssembler()
            : this(ChapterLayout.Default)
        {
        }

        public ChapterAssembler(ChapterLayout layout)
            : this(layout, null)
        {
        }

        /// <param name="specialLevels">
        /// Hand-made boards that take fixed places in the run, such as a picture board on the
        /// flight that lands in a new country. Each must already carry a solution.
        /// </param>
        public ChapterAssembler(ChapterLayout layout, IReadOnlyList<SpecialLevel> specialLevels)
        {
            this.layout = layout;

            if (specialLevels == null)
            {
                return;
            }

            for (int i = 0; i < specialLevels.Count; i++)
            {
                SpecialLevel special = specialLevels[i];

                if (special.LevelNumber < 1 || special.LevelNumber > layout.TotalLevels)
                {
                    throw new ArgumentException("Special level " + special.Level.Id + " sits outside the campaign.", nameof(specialLevels));
                }

                if (specials.ContainsKey(special.LevelNumber))
                {
                    throw new ArgumentException("Two special levels claim level " + special.LevelNumber + ".", nameof(specialLevels));
                }

                specials.Add(special.LevelNumber, special);
            }
        }

        public Campaign Assemble(IReadOnlyList<ScoredLevel> pool)
        {
            if (pool == null)
            {
                throw new ArgumentNullException(nameof(pool));
            }

            List<ScoredLevel> sorted = SortByScoreThenId(pool);

            int needed = layout.TotalLevels - specials.Count;

            if (sorted.Count < needed)
            {
                throw new ArgumentException(
                    "Need " + needed + " levels but the pool holds only " + sorted.Count + ".",
                    nameof(pool));
            }

            var used = new bool[sorted.Count];
            var chapters = new List<Chapter>(layout.ChapterCount);
            var recentSizes = new List<string>(MaxSameSizeRun);
            int relaxed = 0;
            int placed = 0;

            for (int chapterIndex = 0; chapterIndex < layout.ChapterCount && placed < layout.TotalLevels; chapterIndex++)
            {
                string chapterId = "ch" + (chapterIndex + 1).ToString("00");
                var placements = new List<PlacedLevel>(layout.LevelsPerChapter);

                for (int slot = 0; slot < layout.LevelsPerChapter && placed < layout.TotalLevels; slot++)
                {
                    double target = layout.GetTarget(chapterIndex, slot);

                    if (specials.TryGetValue(placed + 1, out SpecialLevel special))
                    {
                        string specialId = chapterId + "_" + (slot + 1).ToString("000");
                        placements.Add(new PlacedLevel(special.Level.WithIdentity(specialId, DifficultyOf(target)), target, target));
                        Remember(recentSizes, special.Level.Grid.ToString(), MaxSameSizeRun);
                        placed++;
                        continue;
                    }

                    int minCells = placed < layout.OnboardingLevelCount ? layout.OnboardingMinCells : 1;
                    int maxCells = placed < layout.OnboardingLevelCount ? layout.OnboardingMaxCells : int.MaxValue;
                    int pick = placed < layout.OnboardingLevelCount
                        ? FindOnboarding(sorted, used, minCells, layout.OnboardingCellLimit(placed), maxCells)
                        : FindNearest(sorted, used, target, recentSizes, true, minCells, maxCells);

                    if (pick < 0)
                    {
                        pick = FindNearest(sorted, used, target, recentSizes, false, minCells, maxCells);
                        relaxed++;
                    }

                    if (pick < 0)
                    {
                        throw new ArgumentException("Not enough onboarding levels within the configured cell limits.", nameof(pool));
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
        /// Onboarding grows the board one step at a time instead of following the curve:
        /// the biggest unused board within the step's size limit, the easiest of those on a
        /// tie. A pool with no board of that size moves on to the next size up.
        /// </summary>
        private static int FindOnboarding(List<ScoredLevel> sorted, bool[] used, int minCells, int limit, int maxCells)
        {
            for (int cap = limit; cap <= maxCells; cap++)
            {
                int best = -1;

                for (int i = 0; i < sorted.Count; i++)
                {
                    int cells = sorted[i].Level.ActiveCellCount;

                    if (used[i] || cells < minCells || cells > cap)
                    {
                        continue;
                    }

                    // The list is sorted by score, so the first board of a size is its easiest.
                    if (best < 0 || cells > sorted[best].Level.ActiveCellCount)
                    {
                        best = i;
                    }
                }

                if (best >= 0)
                {
                    return best;
                }
            }

            return -1;
        }

        /// <summary>A special board is rated where the curve wanted that slot, on the 1 to 10 scale.</summary>
        private static int DifficultyOf(double target)
        {
            int rating = DifficultyScorer.MinDifficulty +
                (int)Math.Round(target * (DifficultyScorer.MaxDifficulty - DifficultyScorer.MinDifficulty));
            return Math.Max(DifficultyScorer.MinDifficulty, Math.Min(DifficultyScorer.MaxDifficulty, rating));
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
            bool respectVariety,
            int minCells,
            int maxCells)
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

                if (used[index] || sorted[index].Level.ActiveCellCount < minCells ||
                    sorted[index].Level.ActiveCellCount > maxCells)
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
