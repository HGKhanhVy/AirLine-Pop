using System.Collections.Generic;
using ASTeams.SingleLine.Core;
using ASTeams.SingleLine.Import;
using NUnit.Framework;

namespace ASTeams.SingleLine.Core.Tests
{
    public sealed class ChapterAssemblerTests
    {
        /// <summary>
        /// A synthetic pool spread evenly over the score range, with board sizes cycling
        /// so the variety rule has something to work with. Scores are what the assembler
        /// actually reads, so the levels themselves can stay simple.
        /// </summary>
        private static List<ScoredLevel> CreatePool(int count)
        {
            var pool = new List<ScoredLevel>(count);
            var sizes = new[] { 3, 4, 5, 6 };

            for (int i = 0; i < count; i++)
            {
                double score = count == 1 ? 0 : i / (double)(count - 1);
                int width = sizes[i % sizes.Length];
                LevelData level = LevelSamples.CreateFullBoard(width, 1);
                var features = new LevelFeatures(width, 2.5, 100, i % 7, true);

                pool.Add(new ScoredLevel(
                    level.WithIdentity("src_" + i.ToString("0000"), 1),
                    features,
                    score,
                    DifficultyScorer.ToDifficulty(score)));
            }

            return pool;
        }

        private static Campaign AssembleDefault()
        {
            return new ChapterAssembler().Assemble(CreatePool(834));
        }

        private static List<LevelData> Flatten(Campaign campaign)
        {
            var all = new List<LevelData>();

            foreach (Chapter chapter in campaign.Chapters)
            {
                all.AddRange(chapter.Levels);
            }

            return all;
        }

        /// <summary>
        /// The curve is checked on the fine grained score, not on the 1 to 10 band. One
        /// band spans about one chapter, so the rest and peak beats are invisible there.
        /// </summary>
        private static List<PlacedLevel> FlattenPlacements(Campaign campaign)
        {
            var all = new List<PlacedLevel>();

            foreach (Chapter chapter in campaign.Chapters)
            {
                all.AddRange(chapter.Placements);
            }

            return all;
        }

        [Test]
        public void ProducesTenChaptersOfThirty()
        {
            Campaign campaign = AssembleDefault();

            Assert.AreEqual(10, campaign.Chapters.Count);
            Assert.AreEqual(300, campaign.LevelCount);

            foreach (Chapter chapter in campaign.Chapters)
            {
                Assert.AreEqual(30, chapter.LevelCount, chapter.Id);
            }
        }

        [Test]
        public void OnboardingLimitsCannotBeRelaxedByDifficultyOrVariety()
        {
            List<ScoredLevel> pool = CreatePool(400);
            for (int i = 0; i < 40; i++)
            {
                LevelData large = LevelSamples.CreateFullBoard(4, 4);
                pool[i] = new ScoredLevel(large, new LevelFeatures(16, 2.5, 100, 0, true), i / 400.0, 1);
            }

            Campaign campaign = new ChapterAssembler().Assemble(pool);
            for (int i = 0; i < 10; i++)
            {
                Assert.That(campaign.Chapters[0].Levels[i].ActiveCellCount, Is.InRange(3, 10));
            }
        }

        [Test]
        public void MissingSmallBoardsRejectsCampaignInsteadOfShippingHardOnboarding()
        {
            var pool = new List<ScoredLevel>();
            for (int i = 0; i < 300; i++)
            {
                pool.Add(new ScoredLevel(LevelSamples.CreateFullBoard(4, 4),
                    new LevelFeatures(16, 2.5, 100, 0, true), i / 300.0, 1));
            }

            Assert.Throws<System.ArgumentException>(() => new ChapterAssembler().Assemble(pool));
        }

        [Test]
        public void IdsFollowTheGddPattern()
        {
            Campaign campaign = AssembleDefault();

            Assert.AreEqual("ch01", campaign.Chapters[0].Id);
            Assert.AreEqual("ch01_001", campaign.Chapters[0].Levels[0].Id);
            Assert.AreEqual("ch01_030", campaign.Chapters[0].Levels[29].Id);
            Assert.AreEqual("ch10_030", campaign.Chapters[9].Levels[29].Id);
        }

        [Test]
        public void NoLevelIsUsedTwice()
        {
            var seen = new HashSet<string>();

            foreach (LevelData level in Flatten(AssembleDefault()))
            {
                Assert.IsTrue(seen.Add(level.Id), "duplicate id " + level.Id);
            }
        }

        [Test]
        public void TheTrendRisesWithinEveryChapter()
        {
            // The window has to span one whole rhythm period. A shorter one catches one
            // rest in some positions and two in others, so it swings with the rhythm
            // instead of smoothing it. Ten consecutive slots always cover each rhythm
            // beat exactly once, leaving only the trend.
            const int window = 10;
            const int poolSize = 834;

            // Measured against the real corpus the moving average never steps back at
            // all, so this is an equality check with room only for floating point noise.
            const double tolerance = 1e-9;

            foreach (Chapter chapter in new ChapterAssembler().Assemble(CreatePool(poolSize)).Chapters)
            {
                IReadOnlyList<PlacedLevel> placed = chapter.Placements;
                double previous = double.MinValue;
                double first = double.NaN;
                double last = 0;

                for (int i = 0; i + window <= placed.Count; i++)
                {
                    double sum = 0;

                    for (int j = i; j < i + window; j++)
                    {
                        sum += placed[j].Score;
                    }

                    double average = sum / window;
                    Assert.GreaterOrEqual(average, previous - tolerance,
                        chapter.Id + " dipped at slot " + i);

                    if (double.IsNaN(first))
                    {
                        first = average;
                    }

                    last = average;
                    previous = average;
                }

                // Guards against the tolerance above hiding a curve that never climbs.
                Assert.Greater(last, first + 0.01,
                    chapter.Id + " never actually gets harder");
            }
        }

        [Test]
        public void EachChapterIsHarderThanTheOneBeforeIt()
        {
            // Across chapters the trend has to rise even though the moving average
            // deliberately steps back at each boundary, which is the landing that makes
            // arriving in a new chapter feel like relief.
            Campaign campaign = AssembleDefault();
            double previous = double.MinValue;

            foreach (Chapter chapter in campaign.Chapters)
            {
                double sum = 0;

                foreach (PlacedLevel placed in chapter.Placements)
                {
                    sum += placed.Score;
                }

                double mean = sum / chapter.LevelCount;
                Assert.Greater(mean, previous, chapter.Id + " is no harder than the last");
                previous = mean;
            }
        }

        [Test]
        public void EachBlockOfTenHoldsAtLeastTwoBreathers()
        {
            List<PlacedLevel> all = FlattenPlacements(AssembleDefault());

            // Onboarding grows the board step by step on purpose, so its block is left out.
            for (int start = ChapterLayout.Default.OnboardingLevelCount; start + 10 <= all.Count; start += 10)
            {
                int dips = 0;

                for (int i = start + 1; i < start + 10; i++)
                {
                    if (all[i].Score < all[i - 1].Score)
                    {
                        dips++;
                    }
                }

                Assert.GreaterOrEqual(dips, 2, "block starting at " + start + " never lets up");
            }
        }

        [Test]
        public void NeverPlacesMoreThanFourIdenticalBoardSizesInARow()
        {
            List<LevelData> all = Flatten(AssembleDefault());
            int run = 1;

            // Onboarding takes the biggest board each step allows, so it may repeat a size.
            for (int i = ChapterLayout.Default.OnboardingLevelCount + 1; i < all.Count; i++)
            {
                run = all[i].Grid.Equals(all[i - 1].Grid) ? run + 1 : 1;
                Assert.LessOrEqual(run, 4, "board size repeats too long at " + all[i].Id);
            }
        }

        [Test]
        public void ChaptersOpenBelowTheCeilingOfTheOneBefore()
        {
            Campaign campaign = AssembleDefault();

            for (int i = 1; i < campaign.Chapters.Count; i++)
            {
                double previousPeak = 0;

                foreach (PlacedLevel placed in campaign.Chapters[i - 1].Placements)
                {
                    if (placed.Score > previousPeak)
                    {
                        previousPeak = placed.Score;
                    }
                }

                Assert.Less(
                    campaign.Chapters[i].Placements[0].Score,
                    previousPeak,
                    campaign.Chapters[i].Id + " opens on a wall instead of a landing");
            }
        }

        [Test]
        public void AssemblyIsDeterministic()
        {
            List<PlacedLevel> first = FlattenPlacements(new ChapterAssembler().Assemble(CreatePool(834)));
            List<PlacedLevel> second = FlattenPlacements(new ChapterAssembler().Assemble(CreatePool(834)));

            for (int i = 0; i < first.Count; i++)
            {
                Assert.AreEqual(first[i].Score, second[i].Score, 1e-12, "slot " + i);
                Assert.AreEqual(first[i].Level.ActiveCellCount, second[i].Level.ActiveCellCount, "slot " + i);
            }
        }

        [Test]
        public void APoolThatIsTooSmall_IsRejected()
        {
            Assert.Throws<System.ArgumentException>(
                () => new ChapterAssembler().Assemble(CreatePool(299)));
        }

        [Test]
        public void AHealthyPool_NeedsNoRelaxation()
        {
            Assert.AreEqual(0, AssembleDefault().RelaxedPlacements);
        }

        [Test]
        public void LayoutTargets_RiseAcrossTheCampaign()
        {
            ChapterLayout layout = ChapterLayout.Default;

            Assert.Less(layout.GetTarget(0, 0), layout.GetTarget(9, 29));
            Assert.IsTrue(layout.IsRest(3));
            Assert.IsTrue(layout.IsRest(6));
            Assert.IsFalse(layout.IsRest(9), "slot 9 closes the block on a peak");
        }

        [Test]
        public void LayoutRestSlots_SitBelowTheirNeighbours()
        {
            ChapterLayout layout = ChapterLayout.Default;

            Assert.Less(layout.GetTarget(4, 3), layout.GetTarget(4, 2));
            Assert.Less(layout.GetTarget(4, 6), layout.GetTarget(4, 5));
            Assert.Greater(layout.GetTarget(4, 9), layout.GetTarget(4, 8));
        }
    }
}
