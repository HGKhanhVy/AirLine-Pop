using ASTeams.SingleLine.Core;
using ASTeams.SingleLine.Import;
using NUnit.Framework;

namespace ASTeams.SingleLine.Core.Tests
{
    public sealed class GreedyPlayerTests
    {
        private static LevelData Line(int length, int start)
        {
            var cells = new int[length];

            for (int i = 0; i < length; i++)
            {
                cells[i] = i;
            }

            return new LevelData("line", 1, new Grid(length, 1), cells, start);
        }

        [Test]
        public void AForcedCorridor_DefeatsNobody()
        {
            // Every move along a line is the only legal move, so no strategy can go wrong.
            LevelData level = Line(6, 0);

            Assert.AreEqual(0, new GreedyPlayer().CountFailures(level));
        }

        [Test]
        public void ACorridorEnteredInTheMiddle_DefeatsEverybody()
        {
            // Whichever way the path leaves the middle, the other arm is stranded.
            LevelData level = Line(3, 1);

            Assert.AreEqual(GreedyPlayer.StrategyCount, new GreedyPlayer().CountFailures(level));
        }

        [Test]
        public void EachStrategy_IsDeterministic()
        {
            LevelData level = LevelSamples.CreateLevel442();
            var player = new GreedyPlayer();

            for (int i = 0; i < GreedyPlayer.StrategyCount; i++)
            {
                var strategy = (GreedyStrategy)i;
                Assert.AreEqual(
                    player.TryPlay(level, strategy),
                    player.TryPlay(level, strategy),
                    strategy + " changed its mind between runs");
            }
        }

        [Test]
        public void FailureCount_StaysInsideItsRange()
        {
            int failures = new GreedyPlayer().CountFailures(LevelSamples.CreateLevel442());

            Assert.GreaterOrEqual(failures, 0);
            Assert.LessOrEqual(failures, GreedyPlayer.StrategyCount);
        }

        [Test]
        public void AFullBoardFromACorner_IsWalkableByAStubbornPlayer()
        {
            // Sweeping a full rectangle row by row is exactly what a fixed direction
            // preference does, so at least one of the four must get through.
            LevelData level = LevelSamples.CreateFullBoard(6, 4, fixedStart: 0);
            var player = new GreedyPlayer();

            bool anyWon = false;

            for (int i = 0; i < GreedyPlayer.StrategyCount; i++)
            {
                anyWon |= player.TryPlay(level, (GreedyStrategy)i);
            }

            Assert.IsTrue(anyWon);
        }

        [Test]
        public void PlayingDoesNotDependOnTheLevelCarryingASolution()
        {
            var player = new GreedyPlayer();

            Assert.AreEqual(
                player.CountFailures(LevelSamples.CreateLevel442(withSolution: true)),
                player.CountFailures(LevelSamples.CreateLevel442(withSolution: false)));
        }
    }
}
