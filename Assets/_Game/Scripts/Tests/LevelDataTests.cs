using System;
using ASTeams.SingleLine.Core;
using NUnit.Framework;

namespace ASTeams.SingleLine.Core.Tests
{
    public sealed class LevelDataTests
    {
        [Test]
        public void ActiveCellCount_CountsDistinctCells()
        {
            var level = new LevelData("dup", 1, new Grid(3, 3), new[] { 0, 1, 1, 2 });

            Assert.AreEqual(3, level.ActiveCellCount);
            Assert.AreEqual(4, level.DeclaredActiveCells.Count);
        }

        [Test]
        public void IsActive_MatchesTheDeclaredCells()
        {
            LevelData level = LevelSamples.CreateLevel442();

            Assert.IsTrue(level.IsActive(0));
            Assert.IsTrue(level.IsActive(29));
            Assert.IsFalse(level.IsActive(3), "cell 3 is one of the five holes");
            Assert.IsFalse(level.IsActive(13));
            Assert.IsFalse(level.IsActive(-1));
            Assert.IsFalse(level.IsActive(30), "outside a 6x5 board");
        }

        [Test]
        public void Constructor_RejectsCellOutsideTheGrid()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new LevelData("bad", 1, new Grid(3, 3), new[] { 0, 9 }));
        }

        [Test]
        public void Constructor_RejectsEmptyId()
        {
            Assert.Throws<ArgumentException>(
                () => new LevelData(string.Empty, 1, new Grid(3, 3), new[] { 0 }));
        }

        [Test]
        public void MutatingTheSourceArray_DoesNotChangeTheLevel()
        {
            var cells = new[] { 0, 1, 2 };
            var level = new LevelData("copy", 1, new Grid(3, 1), cells);

            cells[0] = 2;

            Assert.AreEqual(0, level.DeclaredActiveCells[0]);
            Assert.AreEqual(3, level.ActiveCellCount);
        }

        [Test]
        public void FixedEndpoints_DefaultToUnset()
        {
            LevelData level = LevelSamples.CreateLevel442();

            Assert.IsFalse(level.HasFixedStart);
            Assert.IsFalse(level.HasFixedEnd);
            Assert.AreEqual(LevelData.NoCell, level.FixedStart);
        }
    }
}
