using ASTeams.SingleLine.Data;
using NUnit.Framework;

namespace ASTeams.SingleLine.Tests
{
    public sealed class CampaignLevelAddressTests
    {
        [TestCase(1, "ch01_001")]
        [TestCase(30, "ch01_030")]
        [TestCase(31, "ch02_001")]
        [TestCase(299, "ch10_029")]
        [TestCase(300, "ch10_030")]
        [TestCase(410, "ch14_020")]
        public void ToLevelId_MapsCampaignBoundary(int levelNumber, string expected)
        {
            Assert.That(CampaignLevelAddress.ToLevelId(levelNumber), Is.EqualTo(expected));
        }

        [TestCase("ch01_001", 1)]
        [TestCase("ch01_030", 30)]
        [TestCase("ch02_001", 31)]
        [TestCase("ch10_030", 300)]
        [TestCase("ch14_020", 410)]
        public void TryGetLevelNumber_MapsValidId(string levelId, int expected)
        {
            bool parsed = CampaignLevelAddress.TryGetLevelNumber(levelId, out int actual);

            Assert.That(parsed, Is.True);
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase("")]
        [TestCase("ch1_001")]
        [TestCase("ch01_000")]
        [TestCase("ch01_031")]
        [TestCase("ch14_021")]
        [TestCase("ch15_001")]
        public void TryGetLevelNumber_RejectsInvalidId(string levelId)
        {
            Assert.That(CampaignLevelAddress.TryGetLevelNumber(levelId, out _), Is.False);
        }
    }
}
