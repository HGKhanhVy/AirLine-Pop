using ASTeams.SingleLine.Data;
using NUnit.Framework;

namespace ASTeams.SingleLine.Tests
{
    public sealed class ReplayAllowanceTests
    {
        private sealed class MemoryStore : IReplayRewardStore
        {
            public int Day { get; private set; }

            public int PaidCount { get; private set; }

            public void Save(int day, int paidCount)
            {
                Day = day;
                PaidCount = paidCount;
            }
        }

        [Test]
        public void Claim_PaysUntilDailyLimit()
        {
            var allowance = new ReplayAllowance(new MemoryStore(), 10, 3);

            Assert.That(allowance.Claim(100), Is.EqualTo(10));
            Assert.That(allowance.Claim(100), Is.EqualTo(10));
            Assert.That(allowance.Claim(100), Is.EqualTo(10));
            Assert.That(allowance.Claim(100), Is.EqualTo(0));
        }

        [Test]
        public void Claim_StartsAgainNextDay()
        {
            var store = new MemoryStore();
            var allowance = new ReplayAllowance(store, 10, 1);

            allowance.Claim(100);

            Assert.That(allowance.Claim(100), Is.EqualTo(0));
            Assert.That(allowance.Claim(101), Is.EqualTo(10));
            Assert.That(store.PaidCount, Is.EqualTo(1));
        }

        [TestCase(0, 5)]
        [TestCase(10, 0)]
        public void Claim_PaysNothingWhenTurnedOff(int coins, int perDay)
        {
            var store = new MemoryStore();
            var allowance = new ReplayAllowance(store, coins, perDay);

            Assert.That(allowance.Claim(100), Is.EqualTo(0));
            Assert.That(store.PaidCount, Is.EqualTo(0));
        }
    }
}
