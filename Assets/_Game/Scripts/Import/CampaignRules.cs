using System;
using System.Collections.Generic;
using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// Lays the extra conditions of a <see cref="RulePlan"/> onto an assembled campaign,
    /// and turns the last ordinary flight of each chapter into its VIP flight. Every
    /// changed level is replayed against the new rules before it is kept, so a variant
    /// that broke a level would fail the build instead of shipping.
    /// </summary>
    public sealed class CampaignRules
    {
        private readonly RulePlan plan;
        private readonly IReadOnlyList<ILevelVariant> variants;

        public CampaignRules(RulePlan plan, IReadOnlyList<ILevelVariant> variants)
        {
            this.plan = plan ?? throw new ArgumentNullException(nameof(plan));
            this.variants = variants ?? throw new ArgumentNullException(nameof(variants));
        }

        /// <summary>
        /// Formation goes first because it rebuilds the board the others are laid onto; night
        /// goes last because it only marks the level.
        /// </summary>
        public static CampaignRules Default => new CampaignRules(RulePlan.Default,
            new ILevelVariant[] { new FormationVariant(), new WindVariant(), new RunwayVariant(), new NightVariant() });

        public Campaign Apply(Campaign campaign)
        {
            var chapters = new List<Chapter>(campaign.Chapters.Count);
            int number = 0;

            foreach (Chapter chapter in campaign.Chapters)
            {
                var source = new List<PlacedLevel>(chapter.Placements);
                var placements = new List<PlacedLevel>(source.Count);
                int vipIndex = LastOrdinary(chapter);
                int first = number + 1;
                FitBoardsToRules(source, first, vipIndex);

                for (int i = 0; i < source.Count; i++)
                {
                    PlacedLevel placed = source[i];
                    number++;
                    LevelData level = i == vipIndex
                        ? MakeVip(placed.Level, number, chapter.Number)
                        : Decorate(placed.Level, plan.RulesFor(number, IsSpecial(placed)));
                    placements.Add(new PlacedLevel(level, placed.Score, placed.Target));
                }

                chapters.Add(new Chapter(chapter.Number, chapter.Id, placements));
            }

            return new Campaign(chapters, campaign.LevelCount, campaign.RelaxedPlacements);
        }

        /// <summary>
        /// Night and formation flights only suit small boards. When the board drawn for such a
        /// slot is too big, it trades places with the smallest plain board of the same city that
        /// does suit, so the flight keeps its condition at the cost of a small change in pace.
        /// </summary>
        private void FitBoardsToRules(List<PlacedLevel> placements, int firstNumber, int vipIndex)
        {
            for (int i = 0; i < placements.Count; i++)
            {
                int number = firstNumber + i;
                LevelRule rules = plan.RulesFor(number, IsSpecial(placements[i]));
                LevelRule picky = rules & (LevelRule.Night | LevelRule.Formation);

                if (picky == LevelRule.None || Takes(placements[i].Level, picky))
                {
                    continue;
                }

                int best = -1;
                int city = (number - 1) / 10;

                for (int j = 0; j < placements.Count; j++)
                {
                    int other = firstNumber + j;

                    if (j == i || j == vipIndex || (other - 1) / 10 != city || IsSpecial(placements[j]) ||
                        plan.RulesFor(other, false) != LevelRule.None || !Takes(placements[j].Level, picky))
                    {
                        continue;
                    }

                    if (best < 0 || placements[j].Level.ActiveCellCount < placements[best].Level.ActiveCellCount)
                    {
                        best = j;
                    }
                }

                if (best >= 0)
                {
                    PlacedLevel swap = placements[i];
                    placements[i] = placements[best];
                    placements[best] = swap;
                }
            }
        }

        /// <summary>True when every variant for these rules takes the board.</summary>
        private bool Takes(LevelData level, LevelRule rules)
        {
            LevelData result = level;

            for (int i = 0; i < variants.Count; i++)
            {
                if ((rules & variants[i].Rule) != 0)
                {
                    result = variants[i].Apply(result);
                }
            }

            return (result.Rules & rules) == rules;
        }

        private static bool IsSpecial(PlacedLevel placed)
        {
            return LevelTags.SpecialName(placed.Level) != null;
        }

        /// <summary>Index of the chapter's last flight that is not a picture board, or -1.</summary>
        private static int LastOrdinary(Chapter chapter)
        {
            for (int i = chapter.Placements.Count - 1; i >= 0; i--)
            {
                if (LevelTags.SpecialName(chapter.Placements[i].Level) == null)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>The first mix of known conditions the board can take in full, tagged as the VIP flight.</summary>
        private LevelData MakeVip(LevelData level, int number, int chapterNumber)
        {
            LevelData chosen = level;

            foreach (LevelRule mix in plan.VipMixes(number, chapterNumber))
            {
                LevelData decorated = Decorate(level, mix);

                if ((decorated.Rules & mix) == mix)
                {
                    chosen = decorated;
                    break;
                }
            }

            return chosen.WithTags(TagList.With(chosen.Tags, LevelTags.Vip));
        }

        private LevelData Decorate(LevelData level, LevelRule rules)
        {
            if (rules == LevelRule.None)
            {
                return level;
            }

            LevelData result = level;

            for (int i = 0; i < variants.Count; i++)
            {
                if ((rules & variants[i].Rule) != 0)
                {
                    result = variants[i].Apply(result);
                }
            }

            if (!PathSequenceValidator.IsValid(result, result.Solution, new bool[result.Grid.CellCount], true))
            {
                throw new InvalidOperationException("Rules " + rules + " broke the solution of " + level.Id + ".");
            }

            return result;
        }
    }
}
