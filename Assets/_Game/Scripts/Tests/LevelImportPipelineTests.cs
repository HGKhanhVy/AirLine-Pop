using System.Collections.Generic;
using System.Text;
using ASTeams.SingleLine.Core;
using ASTeams.SingleLine.Import;
using NUnit.Framework;

namespace ASTeams.SingleLine.Core.Tests
{
    public sealed class LevelImportPipelineTests
    {
        /// <summary>Two chapters of three, so a handful of boards is enough to fill a campaign.</summary>
        private static ChapterLayout SmallLayout()
        {
            return new ChapterLayout(2, 3, 0.2, ChapterLayout.DefaultRhythm);
        }

        /// <summary>
        /// A full rectangle written in the most common source form: cells ascending, then
        /// the start repeated. Every full rectangle can be walked from a corner, so these
        /// are all solvable and all distinct once the sizes differ.
        /// </summary>
        private static RawLevelFile Rectangle(string id, int width, int height)
        {
            var text = new StringBuilder();
            text.Append(width).Append(',').Append(height);

            for (int i = 0; i < width * height; i++)
            {
                text.Append(',').Append(i);
            }

            text.Append(",0");
            return new RawLevelFile(id, text.ToString(), null);
        }

        private static List<RawLevelFile> CreateSource()
        {
            var files = new List<RawLevelFile>();
            int index = 0;

            for (int length = 3; length <= 12; length++)
            {
                files.Add(Rectangle("pack/Level_" + (++index).ToString("000"), length, 1));
            }

            for (int width = 2; width <= 8; width++)
            {
                files.Add(Rectangle("pack/Level_" + (++index).ToString("000"), width, 2));
            }

            for (int width = 3; width <= 6; width++)
            {
                files.Add(Rectangle("pack/Level_" + (++index).ToString("000"), width, 3));
            }

            return files;
        }

        private static ImportReport Run(List<RawLevelFile> files)
        {
            return new LevelImportPipeline(SmallLayout(), WarnsdorffSolver.DefaultNodeBudget).Run(files);
        }

        [Test]
        public void RunsEndToEndAndProducesACampaign()
        {
            ImportReport report = Run(CreateSource());

            Assert.IsTrue(report.HasCampaign, report.CampaignError);
            Assert.AreEqual(2, report.Campaign.Chapters.Count);
            Assert.AreEqual(6, report.Campaign.LevelCount);
            Assert.AreEqual(21, report.Parsed.Count);
        }

        [Test]
        public void EveryExportedLevelCarriesAPlayableSolution()
        {
            ImportReport report = Run(CreateSource());

            foreach (Chapter chapter in report.Campaign.Chapters)
            {
                foreach (LevelData level in chapter.Levels)
                {
                    Assert.IsTrue(level.HasSolution, level.Id);

                    var session = new PathSession(level);

                    foreach (int cell in level.Solution)
                    {
                        Assert.AreNotEqual(MoveResult.Rejected, session.Move(cell), level.Id);
                    }

                    Assert.AreEqual(PathState.Won, session.State, level.Id);
                }
            }
        }

        [Test]
        public void SourcesWithoutATutorialAreCounted()
        {
            ImportReport report = Run(CreateSource());

            Assert.AreEqual(21, report.WithoutTutorial);
            Assert.AreEqual(21, report.Pool.SolvedCount, "all of them needed a solver written solution");
        }

        [Test]
        public void DuplicateBoardsCollapseIntoOne()
        {
            List<RawLevelFile> files = CreateSource();
            files.Add(Rectangle("other/Level_001", 4, 3));
            files.Add(Rectangle("other/Level_002", 3, 4));

            ImportReport report = Run(files);

            Assert.AreEqual(23, report.Parsed.Count);
            Assert.AreEqual(21, report.UniqueBoardCount, "a 4x3 and a 3x4 are the same board turned");
            Assert.AreEqual(2, report.DuplicateCount);
        }

        [Test]
        public void ABrokenFileIsReportedAndDoesNotStopTheRun()
        {
            List<RawLevelFile> files = CreateSource();
            files.Add(new RawLevelFile("pack/Level_999", "3,3,0,1,99", null));

            ImportReport report = Run(files);

            Assert.AreEqual(1, report.ParseFailures.Count);
            Assert.IsTrue(report.ParseFailures[0].Contains("CellOutsideGrid"));
            Assert.IsTrue(report.HasCampaign, "one bad file must not sink the import");
        }

        [Test]
        public void InputOrderDoesNotChangeTheResult()
        {
            List<RawLevelFile> forwards = CreateSource();
            var backwards = new List<RawLevelFile>(forwards);
            backwards.Reverse();

            ImportReport first = Run(forwards);
            ImportReport second = Run(backwards);

            for (int c = 0; c < first.Campaign.Chapters.Count; c++)
            {
                IReadOnlyList<LevelData> a = first.Campaign.Chapters[c].Levels;
                IReadOnlyList<LevelData> b = second.Campaign.Chapters[c].Levels;

                for (int i = 0; i < a.Count; i++)
                {
                    Assert.AreEqual(a[i].Id, b[i].Id, "slot " + i + " of chapter " + c);
                    Assert.AreEqual(a[i].ActiveCellCount, b[i].ActiveCellCount);
                }
            }
        }

        [Test]
        public void APoolTooSmallForTheLayoutIsReportedNotThrown()
        {
            var files = new List<RawLevelFile>
            {
                Rectangle("pack/Level_001", 3, 1),
                Rectangle("pack/Level_002", 4, 1)
            };

            ImportReport report = Run(files);

            Assert.IsFalse(report.HasCampaign);
            Assert.IsNotNull(report.CampaignError);
            Assert.IsTrue(report.CampaignError.Contains("6"), "the message should say how many were needed");
        }

        [Test]
        public void ProgressIsReportedFromStartToFinish()
        {
            var steps = new List<string>();
            float last = -1f;

            new LevelImportPipeline(SmallLayout(), WarnsdorffSolver.DefaultNodeBudget)
                .Run(CreateSource(), (step, progress) =>
                {
                    steps.Add(step);
                    Assert.GreaterOrEqual(progress, last, "progress went backwards");
                    last = progress;
                });

            Assert.Greater(steps.Count, 1);
            Assert.AreEqual(1f, last, 1e-6);
        }

        [Test]
        public void EmptyInputProducesAnEmptyReportRatherThanAnException()
        {
            ImportReport report = Run(new List<RawLevelFile>());

            Assert.AreEqual(0, report.Parsed.Count);
            Assert.IsFalse(report.HasCampaign);
        }
    }
}
