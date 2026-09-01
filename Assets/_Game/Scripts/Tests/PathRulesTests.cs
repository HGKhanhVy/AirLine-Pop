using ASTeams.SingleLine.Core;
using NUnit.Framework;

namespace ASTeams.SingleLine.Core.Tests
{
    public sealed class PathRulesTests
    {
        private static bool[] NoneVisited(LevelData level)
        {
            return new bool[level.Grid.CellCount];
        }

        [Test]
        public void CanStart_AllowsAnyActiveCellWhenNoStartIsPinned()
        {
            LevelData level = LevelSamples.CreateLevel442();

            Assert.IsTrue(PathRules.CanStart(level, 7));
            Assert.IsTrue(PathRules.CanStart(level, 29));
            Assert.IsFalse(PathRules.CanStart(level, 3), "cell 3 is a hole");
        }

        [Test]
        public void CanStart_RespectsAPinnedStart()
        {
            LevelData level = LevelSamples.CreateFullBoard(3, 3, fixedStart: 4);

            Assert.IsTrue(PathRules.CanStart(level, 4));
            Assert.IsFalse(PathRules.CanStart(level, 0));
        }

        [Test]
        public void CanEnter_RejectsDiagonalSteps()
        {
            LevelData level = LevelSamples.CreateFullBoard(3, 3);
            bool[] visited = NoneVisited(level);

            Assert.IsTrue(PathRules.CanEnter(level, visited, 4, 1), "up is orthogonal");
            Assert.IsFalse(PathRules.CanEnter(level, visited, 4, 0), "0 sits diagonally from 4");
            Assert.IsFalse(PathRules.CanEnter(level, visited, 4, 8));
        }

        [Test]
        public void CanEnter_RejectsWrapAroundAcrossARowEdge()
        {
            LevelData level = LevelSamples.CreateFullBoard(6, 5);
            bool[] visited = NoneVisited(level);

            // 11 is the last column of row 1 and 12 the first column of row 2: their
            // indices differ by one but they are on opposite edges of the board.
            Assert.IsFalse(PathRules.CanEnter(level, visited, 11, 12));
        }

        [Test]
        public void CanEnter_RejectsAVisitedCell()
        {
            LevelData level = LevelSamples.CreateFullBoard(3, 3);
            bool[] visited = NoneVisited(level);
            visited[1] = true;

            Assert.IsFalse(PathRules.CanEnter(level, visited, 4, 1));
        }

        [Test]
        public void CanEnter_RejectsAHole()
        {
            LevelData level = LevelSamples.CreateLevel442();
            bool[] visited = NoneVisited(level);

            Assert.IsFalse(PathRules.CanEnter(level, visited, 9, 3), "3 is a hole above 9");
            Assert.IsTrue(PathRules.CanEnter(level, visited, 9, 15));
        }

        [Test]
        public void CountAvailableMoves_IgnoresHolesAndVisitedCells()
        {
            LevelData level = LevelSamples.CreateLevel442();
            bool[] visited = NoneVisited(level);

            // Cell 7 has neighbours 1 (up), 8 (right), 13 (down, a hole) and 6 (left).
            Assert.AreEqual(3, PathRules.CountAvailableMoves(level, visited, 7));

            visited[8] = true;
            Assert.AreEqual(2, PathRules.CountAvailableMoves(level, visited, 7));
        }

        [Test]
        public void IsComplete_NeedsEveryActiveCell()
        {
            LevelData level = LevelSamples.CreateLevel442();

            Assert.IsFalse(PathRules.IsComplete(level, 24, 4));
            Assert.IsTrue(PathRules.IsComplete(level, 25, 4));
        }

        [Test]
        public void IsComplete_RespectsAPinnedEnd()
        {
            var level = new LevelData("pinned", 1, new Grid(3, 1), new[] { 0, 1, 2 }, fixedEnd: 2);

            Assert.IsFalse(PathRules.IsComplete(level, 3, 0));
            Assert.IsTrue(PathRules.IsComplete(level, 3, 2));
        }
    }
}
