using ASTeams.SingleLine.Core;
using NUnit.Framework;

namespace ASTeams.SingleLine.Core.Tests
{
    public sealed class RouteRegionsTests
    {
        private static RouteRegions Sample()
        {
            return new RouteRegions(new[] { "vietnam", "vietnam", "vietnam", "japan", "japan", "korea" });
        }

        [Test]
        public void PageCount_IsOnePagePerRunOfOneCountry()
        {
            Assert.AreEqual(3, Sample().PageCount);
        }

        [Test]
        public void Pages_StartAndSpanTheirDestinations()
        {
            RouteRegions regions = Sample();

            Assert.AreEqual(0, regions.FirstDestination(0));
            Assert.AreEqual(3, regions.DestinationCount(0));
            Assert.AreEqual(3, regions.FirstDestination(1));
            Assert.AreEqual(2, regions.DestinationCount(1));
            Assert.AreEqual(5, regions.FirstDestination(2));
            Assert.AreEqual(1, regions.DestinationCount(2));
        }

        [Test]
        public void PageOf_FindsTheCountryOfADestination()
        {
            RouteRegions regions = Sample();

            Assert.AreEqual(0, regions.PageOf(2));
            Assert.AreEqual(1, regions.PageOf(3));
            Assert.AreEqual(2, regions.PageOf(5));
        }

        [Test]
        public void PageOf_StaysOnTheLastPagePastTheCatalog()
        {
            Assert.AreEqual(2, Sample().PageOf(40));
        }

        [Test]
        public void LargestPage_IsTheMostDestinationsOnOnePage()
        {
            Assert.AreEqual(3, Sample().LargestPage);
        }

        [Test]
        public void EmptyCatalog_HasNoPages()
        {
            RouteRegions regions = new RouteRegions(new string[0]);

            Assert.AreEqual(0, regions.PageCount);
            Assert.AreEqual(0, regions.PageOf(3));
        }
    }
}
