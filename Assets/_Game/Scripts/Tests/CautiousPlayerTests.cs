using ASTeams.SingleLine.Import;
using NUnit.Framework;

namespace ASTeams.SingleLine.Core.Tests
{
    public sealed class CautiousPlayerTests
    {
        [Test]
        public void ACorridorTakesOneStepASquare()
        {
            var corridor = new LevelData("corridor", 1, new Grid(5, 1), new[] { 0, 1, 2, 3, 4 }, 0);

            Assert.AreEqual(5, new CautiousPlayer().MeasureSteps(corridor));
        }

        [Test]
        public void AnOpenBoardTakesTrialAndErrorButAlwaysTheSameAmount()
        {
            LevelData open = LevelSamples.CreateFullBoard(5, 5);
            var player = new CautiousPlayer();

            long first = player.MeasureSteps(open);

            Assert.GreaterOrEqual(first, 25);
            Assert.AreEqual(first, player.MeasureSteps(open));
        }

        [Test]
        public void TheBudgetCapsAHopelessSearch()
        {
            LevelData open = LevelSamples.CreateFullBoard(8, 8);

            Assert.LessOrEqual(new CautiousPlayer(3, 100).MeasureSteps(open), 100);
        }
    }
}
