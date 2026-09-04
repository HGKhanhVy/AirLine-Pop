using System;
using System.Collections.Generic;
using ASTeams.SingleLine.Core;
using ASTeams.SingleLine.Data;
using NUnit.Framework;

namespace ASTeams.SingleLine.Core.Tests
{
    public sealed class LevelJsonSerializerTests
    {
        private static LevelData CreateRichLevel()
        {
            return new LevelData(
                "ch01_007",
                1,
                new Grid(6, 5),
                LevelSamples.Level442Cells,
                fixedStart: 7,
                fixedEnd: 4,
                solution: LevelSamples.Level442Solution,
                difficulty: 4,
                themeId: "neon",
                tags: new[] { "early", "turn" });
        }

        private static LevelData RoundTrip(LevelData level)
        {
            string json = LevelJsonSerializer.SerializeChapter("ch01", new List<LevelData> { level });
            return LevelJsonSerializer.DeserializeChapter(json)[0];
        }

        [Test]
        public void EveryFieldSurvivesARoundTrip()
        {
            LevelData original = CreateRichLevel();
            LevelData restored = RoundTrip(original);

            Assert.AreEqual(original.Id, restored.Id);
            Assert.AreEqual(original.Version, restored.Version);
            Assert.AreEqual(original.Grid, restored.Grid);
            Assert.AreEqual(original.ActiveCellCount, restored.ActiveCellCount);
            Assert.AreEqual(original.FixedStart, restored.FixedStart);
            Assert.AreEqual(original.FixedEnd, restored.FixedEnd);
            Assert.AreEqual(original.Difficulty, restored.Difficulty);
            Assert.AreEqual(original.ThemeId, restored.ThemeId);
            Assert.AreEqual(original.Solution.Count, restored.Solution.Count);
            Assert.AreEqual(original.Tags.Count, restored.Tags.Count);
        }

        [Test]
        public void TheRestoredLevelStillPlays()
        {
            // The real check is not field equality but that the board still behaves.
            LevelData restored = RoundTrip(CreateRichLevel());
            var session = new PathSession(restored);

            foreach (int cell in restored.Solution)
            {
                Assert.AreNotEqual(MoveResult.Rejected, session.Move(cell));
            }

            Assert.AreEqual(PathState.Won, session.State);
        }

        [Test]
        public void AnUnsetStartIsOmittedRatherThanWrittenAsMinusOne()
        {
            var level = new LevelData("ch01_001", 1, new Grid(3, 1), new[] { 0, 1, 2 });
            string json = LevelJsonSerializer.SerializeChapter("ch01", new List<LevelData> { level });

            Assert.IsFalse(json.Contains("fixedStart"), "the sentinel must not leak into the file");
            Assert.IsFalse(LevelJsonSerializer.DeserializeChapter(json)[0].HasFixedStart);
        }

        [Test]
        public void AChapterKeepsItsLevelOrder()
        {
            var levels = new List<LevelData>();

            for (int i = 1; i <= 5; i++)
            {
                levels.Add(new LevelData("ch02_" + i.ToString("000"), 1, new Grid(3, 1), new[] { 0, 1, 2 }));
            }

            List<LevelData> restored = LevelJsonSerializer.DeserializeChapter(
                LevelJsonSerializer.SerializeChapter("ch02", levels));

            for (int i = 0; i < levels.Count; i++)
            {
                Assert.AreEqual(levels[i].Id, restored[i].Id);
            }
        }

        [Test]
        public void AFutureFormatVersionIsRefusedRatherThanHalfRead()
        {
            string json = LevelJsonSerializer.SerializeChapter(
                "ch01", new List<LevelData> { CreateRichLevel() });

            string fromTheFuture = json.Replace(
                "\"formatVersion\": " + ChapterFile.CurrentVersion,
                "\"formatVersion\": " + (ChapterFile.CurrentVersion + 1));

            Assert.Throws<FormatException>(() => LevelJsonSerializer.DeserializeChapter(fromTheFuture));
        }

        [Test]
        public void EmptyJsonIsRejected()
        {
            Assert.Throws<ArgumentException>(() => LevelJsonSerializer.DeserializeChapter(""));
        }

        [Test]
        public void TheFileNamesItsFieldsTheWayTheGddDoes()
        {
            string json = LevelJsonSerializer.SerializeChapter(
                "ch01", new List<LevelData> { CreateRichLevel() });

            foreach (string field in new[] { "id", "version", "width", "height", "activeCells", "solution", "difficulty" })
            {
                Assert.IsTrue(json.Contains("\"" + field + "\""), "missing field " + field);
            }
        }
    }
}
