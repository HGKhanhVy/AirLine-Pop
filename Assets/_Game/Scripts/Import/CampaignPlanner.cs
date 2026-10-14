using System;
using System.Collections.Generic;
using AirlinePop.SingleLine.Core;

namespace AirlinePop.SingleLine.Import
{
    /// <summary>
    /// Builds the campaign flight by flight against absolute targets. For each flight it asks
    /// the <see cref="RulePlan"/> for the conditions and the part the flight plays in its city,
    /// the <see cref="FlightTargets"/> for the length and the wrong turns wanted, and the
    /// <see cref="BoardPicker"/> for the board that comes closest once those conditions are on.
    ///
    /// Picture boards keep their fixed flights; each chapter closes on a VIP flight mixing the
    /// conditions met so far, and each city's storm flight is tagged so the game can bill it.
    /// The pass is deterministic, so the same pool always builds the same campaign.
    /// </summary>
    public sealed class CampaignPlanner
    {
        /// <summary>Beyond this many identical board sizes in a row the campaign reads as padding.</summary>
        private const int MaxSameSizeRun = 2;

        private readonly RulePlan plan;
        private readonly FlightTargets targets;
        private readonly int levelCount;
        private readonly int levelsPerChapter;
        private readonly int onboardingMinCells;
        private readonly int onboardingMaxCells;

        public CampaignPlanner(RulePlan plan, FlightTargets targets, int levelCount = 410, int levelsPerChapter = 30,
            int onboardingMinCells = 3, int onboardingMaxCells = 10)
        {
            this.plan = plan ?? throw new ArgumentNullException(nameof(plan));
            this.targets = targets ?? throw new ArgumentNullException(nameof(targets));
            this.levelCount = levelCount;
            this.levelsPerChapter = levelsPerChapter;
            this.onboardingMinCells = onboardingMinCells;
            this.onboardingMaxCells = onboardingMaxCells;
        }

        public static CampaignPlanner Default => new CampaignPlanner(RulePlan.Default, FlightTargets.Default);

        /// <summary>Lays out the campaign from <paramref name="picker"/>, leaving the boards it did not use in it.</summary>
        public Campaign Plan(BoardPicker picker, IReadOnlyList<SpecialLevel> specials, Action<float> onProgress = null)
        {
            if (picker == null)
            {
                throw new ArgumentNullException(nameof(picker));
            }

            Dictionary<int, LevelData> pinned = Pin(specials);
            var recentSizes = new List<string>(MaxSameSizeRun);
            var chapters = new List<Chapter>();
            var placements = new List<PlacedLevel>(levelsPerChapter);
            int relaxed = 0;

            for (int number = 1; number <= levelCount; number++)
            {
                int chapterIndex = (number - 1) / levelsPerChapter;
                int slot = (number - 1) % levelsPerChapter;
                string id = ChapterId(chapterIndex) + "_" + (slot + 1).ToString("000");
                FlightRole role = plan.RoleOf(number);
                PlacedLevel placed;

                if (pinned.TryGetValue(number, out LevelData picture))
                {
                    placed = new PlacedLevel(picture.WithIdentity(id, RatingOf(number, role)), 0, 0);
                }
                else if (role == FlightRole.Onboarding)
                {
                    placed = PlaceOnboarding(picker, number, id, plan.RulesFor(number, false));
                }
                else
                {
                    bool isVip = number == LastOrdinaryOfChapter(chapterIndex, pinned);
                    placed = PlaceFlight(picker, number, id, role, isVip, chapterIndex + 1, recentSizes, ref relaxed);
                }

                Remember(recentSizes, placed.Level.Grid.ToString());
                placements.Add(placed);
                onProgress?.Invoke(number / (float)levelCount);

                // A chapter reads its levels once, when built, so it is built when full.
                if (slot == levelsPerChapter - 1 || number == levelCount)
                {
                    chapters.Add(new Chapter(chapterIndex + 1, ChapterId(chapterIndex), placements));
                    placements = new List<PlacedLevel>(levelsPerChapter);
                }
            }

            return new Campaign(chapters, levelCount, relaxed);
        }

        private static Dictionary<int, LevelData> Pin(IReadOnlyList<SpecialLevel> specials)
        {
            var pinned = new Dictionary<int, LevelData>();

            if (specials != null)
            {
                foreach (SpecialLevel special in specials)
                {
                    pinned[special.LevelNumber] = special.Level;
                }
            }

            return pinned;
        }

        private static string ChapterId(int chapterIndex)
        {
            return "ch" + (chapterIndex + 1).ToString("00");
        }

        private PlacedLevel PlaceOnboarding(BoardPicker picker, int number, string id, LevelRule rules)
        {
            int limit = onboardingMinCells +
                (int)Math.Round((number - 1) * (onboardingMaxCells - onboardingMinCells) / (double)Math.Max(1, plan.OnboardingCount - 1));

            // An opening flight shows its condition on a board about this size, as easy as can be.
            if (rules != LevelRule.None &&
                picker.TryPick(new FlightTarget(limit, 1.0), new[] { rules }, (rules & LevelRule.Bridge) != 0, null, out PickedBoard opening))
            {
                LevelData shown = opening.Level.WithIdentity(id, 1);
                BoardPicker.Verify(shown);
                return new PlacedLevel(shown, opening.Trials, 1);
            }

            if (!picker.TryPickOnboarding(onboardingMinCells, limit, out PickedBoard picked))
            {
                throw new InvalidOperationException("No board small enough for onboarding flight " + number + ".");
            }

            return new PlacedLevel(picked.Level.WithIdentity(id, 1), picked.Trials, 1);
        }

        private PlacedLevel PlaceFlight(BoardPicker picker, int number, string id, FlightRole role, bool isVip, int chapterNumber,
            List<string> recentSizes, ref int relaxed)
        {
            LevelRule rules = plan.RulesFor(number, false);
            bool needsBridge = !isVip && (rules & LevelRule.Bridge) != 0;
            IReadOnlyList<LevelRule> mixes = isVip ? plan.VipMixes(number, chapterNumber) : new[] { rules };
            // A formation flight watches two halves at once, the wingman's clouds included, so it
            // is no breather even on a rest slot: it is aimed like an ordinary climb.
            FlightRole aimedAs = role == FlightRole.Rest && (rules & LevelRule.Formation) != 0 ? FlightRole.Climb : role;
            FlightTarget target = targets.For(number, aimedAs, (number - 1) % RulePlan.FlightsPerCity);

            if (!picker.TryPick(target, mixes, needsBridge, RepeatedSize(recentSizes), out PickedBoard picked))
            {
                relaxed++;

                if (!picker.TryPick(target, mixes, needsBridge, null, out picked))
                {
                    throw new InvalidOperationException("No board takes " + string.Join(" or ", mixes) + " for flight " + number + ".");
                }
            }

            var tags = new List<string>(picked.Level.Tags);

            if (isVip)
            {
                tags.Add(LevelTags.Vip);
            }
            else if (role == FlightRole.Storm)
            {
                tags.Add(LevelTags.Storm);
            }

            LevelData final = picked.Level.WithTags(tags.ToArray()).WithIdentity(id, RatingOf(number, role));
            BoardPicker.Verify(final);
            return new PlacedLevel(final, picked.Trials, target.Trials);
        }

        private int LastOrdinaryOfChapter(int chapterIndex, Dictionary<int, LevelData> pinned)
        {
            int first = chapterIndex * levelsPerChapter + 1;
            int last = Math.Min(levelCount, first + levelsPerChapter - 1);

            for (int number = last; number >= first; number--)
            {
                if (!pinned.ContainsKey(number) && plan.RoleOf(number) != FlightRole.Onboarding)
                {
                    return number;
                }
            }

            return -1;
        }

        /// <summary>The 1 to 10 rating a flight carries, from its place on the route and its part in the city.</summary>
        private int RatingOf(int number, FlightRole role)
        {
            double progress = (number - 1) / (double)Math.Max(1, levelCount - 1);
            int bump = role == FlightRole.Storm ? 2 : role == FlightRole.Rest ? -1 : 0;
            int rating = 1 + (int)Math.Round(progress * 8) + bump;
            return Math.Max(DifficultyScorer.MinDifficulty, Math.Min(DifficultyScorer.MaxDifficulty, rating));
        }

        private static string RepeatedSize(List<string> recentSizes)
        {
            if (recentSizes.Count < MaxSameSizeRun)
            {
                return null;
            }

            for (int i = 1; i < recentSizes.Count; i++)
            {
                if (recentSizes[i] != recentSizes[0])
                {
                    return null;
                }
            }

            return recentSizes[0];
        }

        private static void Remember(List<string> history, string value)
        {
            history.Add(value);

            if (history.Count > MaxSameSizeRun)
            {
                history.RemoveAt(0);
            }
        }
    }
}
