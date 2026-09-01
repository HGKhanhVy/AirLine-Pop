using ASTeams.SingleLine.Core;
using NUnit.Framework;

namespace ASTeams.SingleLine.Core.Tests
{
    public sealed class WarnsdorffSolverTests
    {
        /// <summary>
        /// Replays a candidate solution through the rules themselves. Checking the
        /// solver against PathSession rather than against a hard coded path keeps the
        /// test honest about what "solved" means.
        /// </summary>
        private static void AssertSolves(LevelData level, int[] path, int length)
        {
            Assert.AreEqual(level.ActiveCellCount, length, "solution must cover every active cell");

            var session = new PathSession(level);

            for (int step = 0; step < length; step++)
            {
                Assert.AreNotEqual(
                    MoveResult.Rejected,
                    session.Move(path[step]),
                    "step " + step + " of the solution is not a legal move");
            }

            Assert.AreEqual(PathState.Won, session.State);
        }

        [Test]
        public void Solves_TheRippedLevel442_FromItsShippedStart()
        {
            LevelData level = LevelSamples.CreateLevel442(withSolution: false);
            var solver = new WarnsdorffSolver();
            var buffer = new int[level.ActiveCellCount];

            Assert.IsTrue(solver.TrySolve(level, LevelSamples.Level442Start, buffer, out int length));
            Assert.AreEqual(LevelSamples.Level442Start, buffer[0]);
            AssertSolves(level, buffer, length);
        }

        [Test]
        public void Solves_AFullSixBySixBoard()
        {
            LevelData level = LevelSamples.CreateFullBoard(6, 6);
            var solver = new WarnsdorffSolver();
            var buffer = new int[level.ActiveCellCount];

            Assert.IsTrue(solver.TrySolve(level, 0, buffer, out int length));
            AssertSolves(level, buffer, length);
        }

        [Test]
        public void Solves_ATenByTenBoardWellInsideTheNodeBudget()
        {
            LevelData level = LevelSamples.CreateFullBoard(10, 10);
            var solver = new WarnsdorffSolver();
            var buffer = new int[level.ActiveCellCount];

            Assert.IsTrue(solver.TrySolve(level, 0, buffer, out int length));
            AssertSolves(level, buffer, length);
            Assert.IsFalse(solver.LastRunHitBudget);
        }

        [Test]
        public void Fails_WhenTheStartStrandsPartOfTheBoard()
        {
            // A three cell line entered in the middle can only ever cover two cells.
            var level = new LevelData("line", 1, new Grid(3, 1), new[] { 0, 1, 2 });
            var solver = new WarnsdorffSolver();
            var buffer = new int[level.ActiveCellCount];

            Assert.IsFalse(solver.TrySolve(level, 1, buffer, out int length));
            Assert.AreEqual(0, length);
            Assert.IsFalse(solver.LastRunHitBudget, "this is a real proof of impossibility, not a timeout");
        }

        [Test]
        public void Fails_OnADisconnectedBoard()
        {
            var level = new LevelData("split", 1, new Grid(3, 3), new[] { 0, 8 });
            var solver = new WarnsdorffSolver();
            var buffer = new int[level.ActiveCellCount];

            Assert.IsFalse(solver.TrySolve(level, 0, buffer, out int _));
        }

        [Test]
        public void Respects_APinnedEnd()
        {
            var cells = new[] { 0, 1, 2 };
            var mustEndAtTwo = new LevelData("end2", 1, new Grid(3, 1), cells, fixedEnd: 2);
            var mustEndAtOne = new LevelData("end1", 1, new Grid(3, 1), cells, fixedEnd: 1);
            var solver = new WarnsdorffSolver();
            var buffer = new int[3];

            Assert.IsTrue(solver.TrySolve(mustEndAtTwo, 0, buffer, out int length));
            Assert.AreEqual(2, buffer[length - 1]);

            Assert.IsFalse(solver.TrySolve(mustEndAtOne, 0, buffer, out int _),
                "a line walked from one end can only finish at the other end");
        }

        [Test]
        public void TrySolveAny_FindsAWorkableStart()
        {
            LevelData level = LevelSamples.CreateLevel442(withSolution: false);
            var solver = new WarnsdorffSolver();
            var buffer = new int[level.ActiveCellCount];

            Assert.IsTrue(solver.TrySolveAny(level, buffer, out int length, out int startCell));
            Assert.IsTrue(level.IsActive(startCell));
            Assert.AreEqual(startCell, buffer[0]);
            AssertSolves(level, buffer, length);
        }

        [Test]
        public void TrySolveAny_UsesThePinnedStartWhenThereIsOne()
        {
            LevelData level = LevelSamples.CreateFullBoard(4, 4, fixedStart: 5);
            var solver = new WarnsdorffSolver();
            var buffer = new int[level.ActiveCellCount];

            Assert.IsTrue(solver.TrySolveAny(level, buffer, out int length, out int startCell));
            Assert.AreEqual(5, startCell);
            AssertSolves(level, buffer, length);
        }

        [Test]
        public void ReusesItsBuffersAcrossConsecutiveSolves()
        {
            var solver = new WarnsdorffSolver();
            LevelData big = LevelSamples.CreateFullBoard(8, 8);
            LevelData small = LevelSamples.CreateFullBoard(4, 4);
            var buffer = new int[big.ActiveCellCount];

            Assert.IsTrue(solver.TrySolve(big, 0, buffer, out int bigLength));
            AssertSolves(big, buffer, bigLength);

            Assert.IsTrue(solver.TrySolve(small, 0, buffer, out int smallLength));
            AssertSolves(small, buffer, smallLength);
        }

        [Test]
        public void ExhaustedBudget_IsReportedSeparatelyFromImpossibility()
        {
            LevelData level = LevelSamples.CreateFullBoard(10, 10);
            var solver = new WarnsdorffSolver(nodeBudget: 5);
            var buffer = new int[level.ActiveCellCount];

            Assert.IsFalse(solver.TrySolve(level, 0, buffer, out int _));
            Assert.IsTrue(solver.LastRunHitBudget);
        }
    }
}
