using System.Collections.Generic;
using ASTeams.SingleLine.Import;
using NUnit.Framework;

namespace ASTeams.SingleLine.Core.Tests
{
    public sealed class SpecialLevelTests
    {
        private const string Plane = "# level: 5\n# name: plane\n.S#.\n####\n.##.\n";

        private static List<ScoredLevel> CreatePool(int count)
        {
            var pool = new List<ScoredLevel>(count);
            var sizes = new[] { 3, 4, 5, 6 };

            for (int i = 0; i < count; i++)
            {
                int width = sizes[i % sizes.Length];
                LevelData level = LevelSamples.CreateFullBoard(width, 1).WithIdentity("src_" + i.ToString("0000"), 1);
                pool.Add(new ScoredLevel(level, new LevelFeatures(width, 2.5, 100, 0, true), i / (double)(count - 1), 1));
            }

            return pool;
        }

        [Test]
        public void ParserReadsCellsStartAndSettings()
        {
            SpecialLevel special = ShapeLevelParser.Parse(Plane);

            Assert.AreEqual(5, special.LevelNumber);
            Assert.AreEqual(4, special.Level.Grid.Width);
            Assert.AreEqual(3, special.Level.Grid.Height);
            Assert.AreEqual(8, special.Level.ActiveCellCount);
            Assert.AreEqual(1, special.Level.FixedStart);
            Assert.AreEqual("plane", LevelTags.SpecialName(special.Level));
        }

        [Test]
        public void ParserRejectsABoardWithoutAStart()
        {
            Assert.Throws<System.FormatException>(() => ShapeLevelParser.Parse("# level: 5\n# name: x\n####\n"));
        }

        [Test]
        public void OrdinaryLevelsHaveNoSpecialName()
        {
            Assert.IsNull(LevelTags.SpecialName(LevelSamples.CreateFullBoard(3, 1)));
        }

        [Test]
        public void SpecialLevelTakesItsSlotAndThePoolFillsTheRest()
        {
            var layout = new ChapterLayout(2, 10, 0.2, ChapterLayout.DefaultRhythm);
            SpecialLevel special = ShapeLevelParser.Parse(Plane);
            var assembler = new ChapterAssembler(layout, new[] { special });

            Campaign campaign = assembler.Assemble(CreatePool(19));

            Assert.AreEqual(20, campaign.LevelCount);
            LevelData placed = campaign.Chapters[0].Levels[4];
            Assert.AreEqual("ch01_005", placed.Id);
            Assert.AreEqual("plane", LevelTags.SpecialName(placed));
        }

        [Test]
        public void CapShortensOnlyTheLastChapter()
        {
            var layout = new ChapterLayout(3, 10, 0.2, ChapterLayout.DefaultRhythm, levelCap: 25);

            Campaign campaign = new ChapterAssembler(layout).Assemble(CreatePool(30));

            Assert.AreEqual(25, campaign.LevelCount);
            Assert.AreEqual(3, campaign.Chapters.Count);
            Assert.AreEqual(5, campaign.Chapters[2].LevelCount);
        }

        [Test]
        public void CapCannotDropAWholeChapter()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => new ChapterLayout(3, 10, 0.2, ChapterLayout.DefaultRhythm, levelCap: 20));
        }

        [Test]
        public void OnboardingBoardsGrowStepByStep()
        {
            ChapterLayout layout = ChapterLayout.FullRoute;

            Assert.AreEqual(3, layout.OnboardingCellLimit(0));
            Assert.AreEqual(10, layout.OnboardingCellLimit(layout.OnboardingLevelCount - 1));
            Assert.AreEqual(410, layout.TotalLevels);
        }
    }
}
