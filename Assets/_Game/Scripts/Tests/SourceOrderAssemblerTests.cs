using System.Collections.Generic;
using ASTeams.SingleLine.Core;
using ASTeams.SingleLine.Import;
using NUnit.Framework;

namespace ASTeams.SingleLine.Core.Tests
{
    public sealed class SourceOrderAssemblerTests
    {
        /// <summary>Two chapters of three, so twelve levels are more than enough.</summary>
        private static ChapterLayout SmallLayout()
        {
            return new ChapterLayout(2, 3, 0.2, ChapterLayout.DefaultRhythm);
        }

        /// <summary>
        /// Scores descend as the source order ascends, so any assembler that sorted by
        /// difficulty would produce the exact reverse of the expected answer.
        /// </summary>
        private static List<ScoredLevel> CreatePool(params string[] sourceIds)
        {
            var pool = new List<ScoredLevel>(sourceIds.Length);

            for (int i = 0; i < sourceIds.Length; i++)
            {
                double score = 1.0 - (i / (double)sourceIds.Length);
                // One row per entry, each a cell wider than the last, so a board's size
                // names the source file it came from and any reordering is visible.
                LevelData level = LevelSamples.CreateFullBoard(3 + i, 1);
                var features = new LevelFeatures(3 + i, 2.5, 100, i % 7, true);

                pool.Add(new ScoredLevel(
                    level.WithIdentity(sourceIds[i], 1),
                    features,
                    score,
                    DifficultyScorer.ToDifficulty(score)));
            }

            return pool;
        }

        private static string[] PackRun(string pack, int count)
        {
            var ids = new string[count];

            for (int i = 0; i < count; i++)
            {
                ids[i] = pack + "/Level_" + (i + 1);
            }

            return ids;
        }

        [Test]
        public void LevelsKeepTheNumberingOfTheirSourceFiles()
        {
            List<ScoredLevel> pool = CreatePool(PackRun("beginner", 6));
            Campaign campaign = new SourceOrderAssembler(SmallLayout()).Assemble(pool);

            Assert.That(campaign.Chapters[0].Levels[0].ActiveCellCount, Is.EqualTo(3), "Level_1 first");
            Assert.That(campaign.Chapters[0].Levels[1].ActiveCellCount, Is.EqualTo(4), "Level_2 second");
            Assert.That(campaign.Chapters[1].Levels[0].ActiveCellCount, Is.EqualTo(6), "Level_4 opens chapter two");
        }

        /// <summary>
        /// File names are text, so a plain string sort puts Level_10 before Level_2 and
        /// silently reorders every pack past nine.
        /// </summary>
        [Test]
        public void FileNumbersSortNumericallyNotAlphabetically()
        {
            var ids = new List<string>();

            for (int i = 1; i <= 12; i++)
            {
                ids.Add("beginner/Level_" + i);
            }

            List<ScoredLevel> pool = CreatePool(ids.ToArray());
            Campaign campaign = new SourceOrderAssembler(SmallLayout()).Assemble(pool);

            // Board widths rise with the source index, so the width names the file.
            Assert.That(campaign.Chapters[0].Levels[0].ActiveCellCount, Is.EqualTo(3), "Level_1");
            Assert.That(campaign.Chapters[0].Levels[1].ActiveCellCount, Is.EqualTo(4), "Level_2, not Level_10 (12 cells)");
        }

        [Test]
        public void PacksFollowTheConfiguredOrder()
        {
            var ids = new List<string>();
            ids.AddRange(PackRun("medium", 3));
            ids.AddRange(PackRun("beginner", 3));

            var order = new LevelPackOrder("beginner", "medium");
            Campaign campaign = new SourceOrderAssembler(SmallLayout(), order).Assemble(CreatePool(ids.ToArray()));

            // The beginner entries were built later, so they carry the wider boards.
            Assert.That(campaign.Chapters[0].Levels[0].ActiveCellCount, Is.EqualTo(6), "beginner/Level_1 first");
            Assert.That(campaign.Chapters[1].Levels[0].ActiveCellCount, Is.EqualTo(3), "medium/Level_1 after beginner");
        }

        [Test]
        public void AnUnknownPackShipsAfterEveryKnownOne()
        {
            var ids = new List<string>();
            ids.AddRange(PackRun("mystery", 3));
            ids.AddRange(PackRun("beginner", 3));

            var order = new LevelPackOrder("beginner");
            Campaign campaign = new SourceOrderAssembler(SmallLayout(), order).Assemble(CreatePool(ids.ToArray()));

            Assert.That(campaign.Chapters[0].Levels[0].ActiveCellCount, Is.EqualTo(6), "beginner runs first");
            Assert.That(campaign.Chapters[1].Levels[0].ActiveCellCount, Is.EqualTo(3), "mystery runs last");
        }

        [Test]
        public void ChaptersAreNumberedAndFilledToTheLayout()
        {
            Campaign campaign = new SourceOrderAssembler(SmallLayout()).Assemble(CreatePool(PackRun("beginner", 9)));

            Assert.That(campaign.Chapters.Count, Is.EqualTo(2));
            Assert.That(campaign.LevelCount, Is.EqualTo(6));
            Assert.That(campaign.RelaxedPlacements, Is.EqualTo(0), "source order never bends a curve");
            Assert.That(campaign.Chapters[0].Id, Is.EqualTo("ch01"));
            Assert.That(campaign.Chapters[0].Levels[0].Id, Is.EqualTo("ch01_001"));
            Assert.That(campaign.Chapters[1].Levels[2].Id, Is.EqualTo("ch02_003"));
        }

        [Test]
        public void TheOrderDoesNotDependOnHowTheFilesArrived()
        {
            List<ScoredLevel> forwards = CreatePool(PackRun("beginner", 8));
            var backwards = new List<ScoredLevel>(forwards);
            backwards.Reverse();

            Campaign first = new SourceOrderAssembler(SmallLayout()).Assemble(forwards);
            Campaign second = new SourceOrderAssembler(SmallLayout()).Assemble(backwards);

            for (int c = 0; c < first.Chapters.Count; c++)
            {
                for (int i = 0; i < first.Chapters[c].Levels.Count; i++)
                {
                    Assert.That(
                        second.Chapters[c].Levels[i].ActiveCellCount,
                        Is.EqualTo(first.Chapters[c].Levels[i].ActiveCellCount),
                        "slot " + i + " of chapter " + c);
                }
            }
        }

        [Test]
        public void APoolTooSmallForTheLayoutIsRejectedWithACountInTheMessage()
        {
            var assembler = new SourceOrderAssembler(SmallLayout());

            var error = Assert.Throws<System.ArgumentException>(
                () => assembler.Assemble(CreatePool(PackRun("beginner", 5))));

            Assert.That(error.Message, Does.Contain("6"));
        }
    }
}
