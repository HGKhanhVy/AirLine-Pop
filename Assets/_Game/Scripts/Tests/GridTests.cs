using System;
using ASTeams.SingleLine.Core;
using NUnit.Framework;

namespace ASTeams.SingleLine.Core.Tests
{
    public sealed class GridTests
    {
        [Test]
        public void CellCount_IsWidthTimesHeight()
        {
            var grid = new Grid(4, 4);

            Assert.AreEqual(16, grid.CellCount);
        }

        [Test]
        public void ToIndex_UsesRowMajorOrder()
        {
            var grid = new Grid(4, 4);

            Assert.AreEqual(0, grid.ToIndex(0, 0));
            Assert.AreEqual(3, grid.ToIndex(0, 3));
            Assert.AreEqual(7, grid.ToIndex(1, 3));
            Assert.AreEqual(14, grid.ToIndex(3, 2));
        }

        [Test]
        public void ToRowAndColumn_RoundTripThroughToIndex()
        {
            var grid = new Grid(6, 5);

            for (int index = 0; index < grid.CellCount; index++)
            {
                int row = grid.ToRow(index);
                int column = grid.ToColumn(index);

                Assert.AreEqual(index, grid.ToIndex(row, column));
            }
        }

        [Test]
        public void AreAdjacent_AcceptsOrthogonalNeighbours()
        {
            var grid = new Grid(4, 4);

            Assert.IsTrue(grid.AreAdjacent(7, 3), "7 and 3 are vertical neighbours.");
            Assert.IsTrue(grid.AreAdjacent(2, 1), "2 and 1 are horizontal neighbours.");
        }

        [Test]
        public void AreAdjacent_RejectsDiagonalsAndSelf()
        {
            var grid = new Grid(4, 4);

            Assert.IsFalse(grid.AreAdjacent(0, 5), "Diagonal must never be adjacent (MOV-01).");
            Assert.IsFalse(grid.AreAdjacent(6, 6), "A cell is not adjacent to itself.");
        }

        [Test]
        public void AreAdjacent_DoesNotWrapAcrossRowEdges()
        {
            var grid = new Grid(4, 4);

            Assert.IsFalse(grid.AreAdjacent(3, 4), "Right edge of row 0 must not wrap to row 1.");
        }

        [Test]
        public void Level7_SolutionPathIsFullyConnected()
        {
            // Assets/_Game/Asset_Resources/Level Data/oneline/beginner/Toturial_7.txt
            var grid = new Grid(4, 4);
            int[] solution = { 7, 3, 2, 1, 0, 4, 8, 9, 10, 14 };

            for (int i = 0; i < solution.Length - 1; i++)
            {
                Assert.IsTrue(
                    grid.AreAdjacent(solution[i], solution[i + 1]),
                    $"Step {i}: {solution[i]} -> {solution[i + 1]} is not a legal move.");
            }
        }

        [Test]
        public void Constructor_RejectsNonPositiveSize()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Grid(0, 4));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Grid(4, -1));
        }
    }
}
