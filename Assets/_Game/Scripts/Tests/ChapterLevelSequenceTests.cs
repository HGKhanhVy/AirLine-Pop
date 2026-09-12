using ASTeams.SingleLine.Data;
using NUnit.Framework;

namespace ASTeams.SingleLine.Core.Tests
{
    public sealed class ChapterLevelSequenceTests
    {
        private static ILevelSequence Sequence()
        {
            var source = new InMemoryChapterSource();
            source.Add("ch01", LevelJsonSerializer.SerializeChapter("ch01", new[]
            {
                LevelSamples.CreateFullBoard(3, 1).WithIdentity("ch01_001", 1),
                LevelSamples.CreateFullBoard(4, 1).WithIdentity("ch01_002", 1)
            }));
            source.Add("ch02", LevelJsonSerializer.SerializeChapter("ch02", new[]
            {
                LevelSamples.CreateFullBoard(5, 1).WithIdentity("ch02_001", 1)
            }));
            return new ChapterLevelSequence(new ChapterLevelRepository(source));
        }

        [TestCase("ch01_001", "ch01_002")]
        [TestCase("ch01_002", "ch02_001")]
        [TestCase("ch02_001", null)]
        [TestCase("ch01_099", null)]
        [TestCase("invalid", null)]
        public void FollowsRepositoryOrderAndStopsAtCampaignEnd(string current, string expected)
        {
            Assert.That(Sequence().GetNext(current), Is.EqualTo(expected));
        }
    }
}
