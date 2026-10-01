using ASTeams.SingleLine.Core;
using NUnit.Framework;

namespace ASTeams.SingleLine.Core.Tests
{
    public sealed class DestinationScheduleTests
    {
        private static DestinationSchedule Campaign()
        {
            // 30 destinations of 10 flights: the 300 shipped levels.
            return new DestinationSchedule(30, 10);
        }

        [Test]
        public void DestinationOf_GroupsTenFlightsPerDestination()
        {
            DestinationSchedule schedule = Campaign();

            Assert.AreEqual(0, schedule.DestinationOf(1));
            Assert.AreEqual(0, schedule.DestinationOf(10));
            Assert.AreEqual(1, schedule.DestinationOf(11));
            Assert.AreEqual(29, schedule.DestinationOf(300));
        }

        [Test]
        public void DestinationOf_StaysOnTheLastDestinationPastTheCatalog()
        {
            Assert.AreEqual(29, Campaign().DestinationOf(412));
        }

        [Test]
        public void StampOf_CountsOneToTenWithinADestination()
        {
            DestinationSchedule schedule = Campaign();

            Assert.AreEqual(1, schedule.StampOf(1));
            Assert.AreEqual(10, schedule.StampOf(10));
            Assert.AreEqual(1, schedule.StampOf(11));
            Assert.AreEqual(5, schedule.StampOf(35));
        }

        [Test]
        public void CompletesPostcard_OnlyOnTheLastFlightOfADestination()
        {
            DestinationSchedule schedule = Campaign();

            Assert.IsTrue(schedule.CompletesPostcard(10));
            Assert.IsTrue(schedule.CompletesPostcard(300));
            Assert.IsFalse(schedule.CompletesPostcard(9));
            Assert.IsFalse(schedule.CompletesPostcard(11));
        }

        [Test]
        public void StampsAndPostcards_FollowTheNextFlightToPlay()
        {
            DestinationSchedule schedule = Campaign();

            // Flights 1 to 34 flown, 35 is next: destination 3 has stamps 31 to 34.
            Assert.AreEqual(10, schedule.StampsCollected(2, 35));
            Assert.AreEqual(4, schedule.StampsCollected(3, 35));
            Assert.AreEqual(0, schedule.StampsCollected(4, 35));

            Assert.IsTrue(schedule.HasPostcard(2, 35));
            Assert.IsFalse(schedule.HasPostcard(3, 35));
        }

        [Test]
        public void NewPlayer_HasNoStampsOrPostcards()
        {
            DestinationSchedule schedule = Campaign();

            Assert.AreEqual(0, schedule.StampsCollected(0, 1));
            Assert.IsFalse(schedule.HasPostcard(0, 1));
        }
    }
}
