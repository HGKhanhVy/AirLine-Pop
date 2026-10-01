using ASTeams.SingleLine.Core;
using NUnit.Framework;

namespace ASTeams.SingleLine.Core.Tests
{
    public sealed class RegularArrivalsTests
    {
        [Test]
        public void StarterIsDueFromTheFirstFlight()
        {
            Assert.IsTrue(RegularArrivals.IsDue(0, 1));
        }

        [Test]
        public void CatArrivesOnceItsFlightHasBeenFlown()
        {
            // Arrives after flight 6: flights 1-6 flown means flight 7 is next.
            Assert.IsFalse(RegularArrivals.IsDue(6, 6));
            Assert.IsTrue(RegularArrivals.IsDue(6, 7));
        }
    }
}
