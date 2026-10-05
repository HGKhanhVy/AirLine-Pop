using ASTeams.SingleLine.Core;
using NUnit.Framework;

namespace ASTeams.SingleLine.Core.Tests
{
    public sealed class RouteLayoutTests
    {
        private static readonly RouteLayoutSettings Settings =
            new RouteLayoutSettings(1080f, 180f, 220f, 340f, 170f, 300f, 230f, 50f, 220f, 70f);

        // Two short countries: 3 cities, then 2, with 10 levels each.
        private static RouteLayout Sample()
        {
            return new RouteLayout(Settings, new[] { 30, 20 }, 10);
        }

        [Test]
        public void LevelCount_IsEveryLevelOfEveryChapter()
        {
            Assert.AreEqual(50, Sample().LevelCount);
        }

        [Test]
        public void Levels_SitAtTheirSpacingAlongTheRoute()
        {
            RouteLayout layout = Sample();

            // At least the spacing: a tight turn may slide a level a little further on.
            Assert.GreaterOrEqual(layout.LevelDistance(2) - layout.LevelDistance(1), 179.5f);
            Assert.GreaterOrEqual(layout.LevelDistance(10) - layout.LevelDistance(9), 219.5f);
            Assert.GreaterOrEqual(layout.LevelDistance(11) - layout.LevelDistance(10), 339.5f);
        }

        [Test]
        public void Levels_NeverCrowdEachOther()
        {
            RouteLayout layout = Sample();

            for (int level = 1; level < 30; level++)
            {
                float apart = RouteVector.Distance(layout.LevelPosition(level), layout.LevelPosition(level + 1));
                Assert.GreaterOrEqual(apart, 0.85f * 180f - 0.5f, "levels " + level + " and " + (level + 1));
            }
        }

        [Test]
        public void ChapterBreak_IsALongLegWithNoLevels()
        {
            RouteLayout layout = Sample();

            float leg = layout.LevelDistance(31) - layout.LevelDistance(30);
            Assert.Greater(leg, 4f * 340f);
        }

        [Test]
        public void ChapterMarks_OnePerChapter()
        {
            Assert.AreEqual(2, Sample().ChapterMarks.Count);
        }

        [Test]
        public void Route_ClimbsAllTheWay()
        {
            RouteLayout layout = Sample();

            for (int i = 1; i < layout.Curve.Count; i++)
            {
                Assert.GreaterOrEqual(layout.Curve[i].Y, layout.Curve[i - 1].Y - 0.01f, "curve point " + i);
            }
        }

        [Test]
        public void Route_StaysOnTheMap()
        {
            RouteLayout layout = Sample();

            for (int i = 0; i < layout.Curve.Count; i++)
            {
                Assert.That(layout.Curve[i].X, Is.InRange(0f, 1080f), "curve point " + i);
            }
        }

        [Test]
        public void PointAt_ALevelsDistance_IsThatLevel()
        {
            RouteLayout layout = Sample();
            RouteVector level = layout.LevelPosition(17);
            RouteVector point = layout.PointAt(layout.LevelDistance(17));

            Assert.AreEqual(level.X, point.X, 0.5f);
            Assert.AreEqual(level.Y, point.Y, 0.5f);
        }

        [Test]
        public void PathBetween_RunsFromOneLevelToTheNext()
        {
            RouteLayout layout = Sample();
            var path = new RouteVector[64];

            int count = layout.PathBetween(layout.LevelDistance(4), layout.LevelDistance(5), path);

            Assert.GreaterOrEqual(count, 2);
            Assert.AreEqual(layout.LevelPosition(4).Y, path[0].Y, 0.5f);
            Assert.AreEqual(layout.LevelPosition(5).Y, path[count - 1].Y, 0.5f);
        }

        [Test]
        public void FirstLevelAtOrAbove_FindsTheLevelsInView()
        {
            RouteLayout layout = Sample();
            float y = layout.LevelPosition(12).Y;

            Assert.AreEqual(12, layout.FirstLevelAtOrAbove(y));
            Assert.AreEqual(13, layout.FirstLevelAtOrAbove(y + 1f));
            Assert.AreEqual(1, layout.FirstLevelAtOrAbove(-1000f));
            Assert.AreEqual(51, layout.FirstLevelAtOrAbove(layout.Height + 1000f));
        }

        [Test]
        public void Hubs_OnePerCity_JustAfterItsLastLevel()
        {
            RouteLayout layout = Sample();

            Assert.AreEqual(5, layout.HubCount);
            Assert.Greater(layout.HubPosition(0).Y, layout.LevelPosition(10).Y);
            Assert.Less(layout.HubPosition(0).Y, layout.LevelPosition(11).Y);
        }

        [Test]
        public void Hubs_TheLastCityStillHasRoomForItsAirport()
        {
            RouteLayout layout = Sample();

            Assert.Greater(layout.HubPosition(4).Y, layout.LevelPosition(50).Y);
        }

        [Test]
        public void FirstHubAtOrAbove_FindsTheAirportsInView()
        {
            RouteLayout layout = Sample();

            Assert.AreEqual(2, layout.FirstHubAtOrAbove(layout.HubPosition(2).Y));
            Assert.AreEqual(0, layout.FirstHubAtOrAbove(-1000f));
            Assert.AreEqual(5, layout.FirstHubAtOrAbove(layout.Height + 1000f));
        }

        [Test]
        public void IsCity_EveryTenthLevel()
        {
            RouteLayout layout = Sample();

            Assert.IsTrue(layout.IsCity(10));
            Assert.IsFalse(layout.IsCity(11));
        }

        [Test]
        public void LevelPosition_ClampsOutsideTheCampaign()
        {
            RouteLayout layout = Sample();

            Assert.AreEqual(layout.LevelPosition(50).Y, layout.LevelPosition(99).Y);
            Assert.AreEqual(layout.LevelPosition(1).Y, layout.LevelPosition(0).Y);
        }
    }
}
