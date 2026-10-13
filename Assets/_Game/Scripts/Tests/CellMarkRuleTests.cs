using NUnit.Framework;

namespace ASTeams.SingleLine.Core.Tests
{
    public sealed class CellMarkRuleTests
    {
        // A 3 x 3 board, every square open:
        //   0 1 2
        //   3 4 5
        //   6 7 8
        private static LevelData Board(int start, CellMark[] marks, int[] solution = null)
        {
            return new LevelData("marks", 1, new Grid(3, 3), new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 }, start, solution: solution,
                marks: marks);
        }

        [Test]
        public void ACloudOnlyLetsThePlaneFlyStraightOn()
        {
            LevelData level = Board(3, new[] { new CellMark(4, CellKind.Straight) });
            var visits = new byte[9];
            visits[3] = visits[4] = 1;

            Assert.IsTrue(PathRules.CanEnter(level, visits, 3, 4, 5));
            Assert.IsFalse(PathRules.CanEnter(level, visits, 3, 4, 1));
            Assert.IsFalse(PathRules.CanEnter(level, visits, 3, 4, 7));
        }

        [Test]
        public void ABeaconMakesThePlaneTurn()
        {
            LevelData level = Board(3, new[] { new CellMark(4, CellKind.Turn) });
            var visits = new byte[9];
            visits[3] = visits[4] = 1;

            Assert.IsFalse(PathRules.CanEnter(level, visits, 3, 4, 5));
            Assert.IsTrue(PathRules.CanEnter(level, visits, 3, 4, 1));
            Assert.IsTrue(PathRules.CanEnter(level, visits, 3, 4, 7));
        }

        [Test]
        public void AMarkDoesNotBindTheStartingSquare()
        {
            LevelData level = Board(4, new[] { new CellMark(4, CellKind.Straight) });

            Assert.AreEqual(4, PathRules.CountAvailableMoves(level, new byte[9], LevelData.NoCell, 4));
        }

        [Test]
        public void AnOverpassIsFlownTwiceAndCountsTowardTheRoute()
        {
            // 0 1 2 / 3 4 5 / 6 7 8 with an overpass in the middle: 3 4 5 across, then 1 4 7 down.
            int[] route = { 0, 3, 4, 5, 2, 1, 4, 7, 6 };
            LevelData level = Board(0, new[] { new CellMark(4, CellKind.Bridge) });

            Assert.AreEqual(10, level.PathLength);
            Assert.AreEqual(LevelRule.Bridge, level.Rules & LevelRule.Bridge);
            Assert.IsFalse(PathSequenceValidator.IsValid(level, route, new byte[9], true), "6 and 8 cannot both end the route");

            var session = new PathSession(level);

            foreach (int cell in new[] { 0, 3, 4, 5, 2, 1, 4, 7 })
            {
                Assert.AreNotEqual(MoveResult.Rejected, session.Move(cell), "step onto " + cell);
            }

            Assert.AreEqual(2, session.VisitCount(4));
        }

        [Test]
        public void AnOverpassMustBeCrossedStraight()
        {
            LevelData level = Board(0, new[] { new CellMark(4, CellKind.Bridge) });
            var session = new PathSession(level);
            session.Move(0);
            session.Move(3);
            session.Move(4);

            Assert.AreEqual(MoveResult.Rejected, session.Move(1));
            Assert.AreEqual(MoveResult.Moved, session.Move(5));
        }

        [Test]
        public void TheSolverFindsARouteOverAnOverpass()
        {
            // 5 x 3 board shaped so the only way through uses the overpass at 7.
            //   0  1  2  3  4
            //   5  6  7  8  9
            //   10 11 12 13 14
            var level = new LevelData("bridge", 1, new Grid(5, 3), new[] { 2, 5, 6, 7, 8, 9, 12 }, 5,
                marks: new[] { new CellMark(7, CellKind.Bridge) });
            var solver = new WarnsdorffSolver();
            var route = new int[level.PathLength];

            Assert.IsFalse(solver.TrySolve(level, 5, route, out _), "the overpass cannot be crossed twice without a loop back");

            // 5 6 7 8, up to 3 2, down over 7 again to 12, then 13 14 9.
            var loop = new LevelData("loop", 1, new Grid(5, 3), new[] { 2, 3, 5, 6, 7, 8, 9, 12, 13, 14 }, 5,
                marks: new[] { new CellMark(7, CellKind.Bridge) });
            route = new int[loop.PathLength];

            Assert.IsTrue(solver.TrySolve(loop, 5, route, out int length));
            Assert.AreEqual(11, length);
            Assert.IsTrue(PathSequenceValidator.IsValid(loop, route, new byte[15], true));
        }
    }
}
