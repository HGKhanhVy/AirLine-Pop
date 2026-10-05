using System.Collections.Generic;
using System.IO;
using ASTeams.SingleLine.Data;
using ASTeams.SingleLine.Import;
using NUnit.Framework;
using UnityEngine;

namespace ASTeams.SingleLine.Core.Tests
{
    public sealed class ShippedCampaignTests
    {
        // One level per gate on the route map: chapters of thirty, the last one cut short.
        private static readonly ChapterLayout Layout = ChapterLayout.FullRoute;

        /// <summary>
        /// Onboarding grows the board from the smallest the pool holds, a three cell row.
        /// Checking the shape rather than the source id keeps the test meaningful after an
        /// export, which renames every level to its campaign id.
        /// </summary>
        [Test]
        public void TheCampaignOpensOnTheSmallestReferenceBoard()
        {
            List<LevelData> first = ReadChapter("ch01");

            Assert.That(first[0].Grid.Width, Is.EqualTo(3));
            Assert.That(first[0].Grid.Height, Is.EqualTo(1));
            Assert.That(first[0].ActiveCellCount, Is.EqualTo(3));
        }

        /// <summary>The onboarding flights never shrink: each board is at least as big as the last.</summary>
        [Test]
        public void TheOpeningLevelsGrowStepByStep()
        {
            List<LevelData> first = ReadChapter("ch01");

            for (int i = 0; i < Layout.OnboardingLevelCount; i++)
            {
                Assert.That(first[i].ActiveCellCount, Is.InRange(Layout.OnboardingMinCells, Layout.OnboardingMaxCells), first[i].Id);

                if (i > 0)
                {
                    Assert.That(first[i].ActiveCellCount, Is.GreaterThanOrEqualTo(first[i - 1].ActiveCellCount), first[i].Id);
                }
            }
        }

        [Test]
        public void EveryReleaseLevelIsUniqueValidAndSolvable()
        {
            var ids = new HashSet<string>();
            var validator = new LevelValidator(new WarnsdorffSolver());
            int count = 0;

            foreach (string file in ChapterFiles())
            {
                List<LevelData> levels = LevelJsonSerializer.DeserializeChapter(File.ReadAllText(file));
                Assert.That(levels.Count, Is.InRange(1, Layout.LevelsPerChapter), file);

                for (int i = 0; i < levels.Count; i++)
                {
                    LevelData level = levels[i];
                    Assert.That(ids.Add(level.Id), Is.True, "Duplicate id: " + level.Id);
                    Assert.That(validator.Validate(level).IsValid, Is.True, level.Id);
                    Assert.That(level.HasSolution, Is.True, level.Id);
                    count++;
                }
            }

            Assert.That(count, Is.EqualTo(Layout.TotalLevels));
        }

        /// <summary>
        /// Replaying the stored solution through the rules catches a level that survived
        /// export but no longer plays, which field equality would miss.
        /// </summary>
        [Test]
        public void EveryReleaseLevelReplaysToAWin()
        {
            foreach (string file in ChapterFiles())
            {
                foreach (LevelData level in LevelJsonSerializer.DeserializeChapter(File.ReadAllText(file)))
                {
                    var session = new PathSession(level);

                    foreach (int cell in level.Solution)
                    {
                        Assert.That(session.Move(cell), Is.Not.EqualTo(MoveResult.Rejected), level.Id);
                    }

                    Assert.That(session.State, Is.EqualTo(PathState.Won), level.Id);
                }
            }
        }

        private static string[] ChapterFiles()
        {
            string folder = Path.Combine(Application.dataPath, "_Game/Resources/Levels");
            string[] files = Directory.GetFiles(folder, "ch*.json");
            Assert.That(files.Length, Is.EqualTo(Layout.ChapterCount));
            return files;
        }

        private static List<LevelData> ReadChapter(string chapterId)
        {
            string path = Path.Combine(Application.dataPath, "_Game/Resources/Levels", chapterId + ".json");
            return LevelJsonSerializer.DeserializeChapter(File.ReadAllText(path));
        }
    }
}
