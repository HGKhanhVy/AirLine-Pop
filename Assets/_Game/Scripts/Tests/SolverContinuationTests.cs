using System.Collections.Generic;
using ASTeams.SingleLine.Core;
using NUnit.Framework;

namespace ASTeams.SingleLine.Core.Tests
{
    public sealed class SolverContinuationTests
    {
        private static List<int> Prefix(IReadOnlyList<int> solution, int count)
        {
            var prefix = new List<int>(count);

            for (int i = 0; i < count; i++)
            {
                prefix.Add(solution[i]);
            }

            return prefix;
        }

        /// <summary>Replays a whole path through the rules, which is the only honest check.</summary>
        private static void AssertPlays(LevelData level, int[] path, int length)
        {
            Assert.AreEqual(level.ActiveCellCount, length);

            var session = new PathSession(level);

            for (int i = 0; i < length; i++)
            {
                Assert.AreNotEqual(MoveResult.Rejected, session.Move(path[i]), "step " + i);
            }

            Assert.AreEqual(PathState.Won, session.State);
        }

        [Test]
        public void ContinuesFromAPartialPath()
        {
            LevelData level = LevelSamples.CreateLevel442();
            var solver = new WarnsdorffSolver();
            var buffer = new int[level.ActiveCellCount];

            List<int> drawn = Prefix(level.Solution, 8);

            Assert.IsTrue(solver.TryContinue(level, drawn, buffer, out int length));
            AssertPlays(level, buffer, length);
        }

        [Test]
        public void TheResultKeepsThePlayerPathAtTheFront()
        {
            LevelData level = LevelSamples.CreateLevel442();
            var solver = new WarnsdorffSolver();
            var buffer = new int[level.ActiveCellCount];

            List<int> drawn = Prefix(level.Solution, 6);
            solver.TryContinue(level, drawn, buffer, out int _);

            for (int i = 0; i < drawn.Count; i++)
            {
                Assert.AreEqual(drawn[i], buffer[i], "the drawn path must not be rearranged");
            }
        }

        [Test]
        public void TheNextCellIsAdjacentToTheHead()
        {
            LevelData level = LevelSamples.CreateLevel442();
            var solver = new WarnsdorffSolver();
            var buffer = new int[level.ActiveCellCount];

            List<int> drawn = Prefix(level.Solution, 5);
            Assert.IsTrue(solver.TryContinue(level, drawn, buffer, out int _));

            int head = drawn[drawn.Count - 1];
            int hint = buffer[drawn.Count];

            Assert.IsTrue(level.Grid.AreAdjacent(head, hint), "a hint the player cannot reach is no hint");
        }

        [Test]
        public void APathOfJustTheStartIsAWholeSolve()
        {
            LevelData level = LevelSamples.CreateLevel442();
            var solver = new WarnsdorffSolver();
            var buffer = new int[level.ActiveCellCount];

            Assert.IsTrue(solver.TryContinue(level, new List<int> { LevelSamples.Level442Start }, buffer, out int length));
            AssertPlays(level, buffer, length);
        }

        [Test]
        public void ADoomedPathHasNoContinuation()
        {
            // Walking into a corner of a line strands the rest of it.
            var level = new LevelData("line", 1, new Grid(5, 1), new[] { 0, 1, 2, 3, 4 }, fixedStart: 2);
            var solver = new WarnsdorffSolver();
            var buffer = new int[level.ActiveCellCount];

            Assert.IsFalse(solver.TryContinue(level, new List<int> { 2, 1, 0 }, buffer, out int length));
            Assert.AreEqual(0, length);
        }

        [Test]
        public void AnEmptyPathIsRefused()
        {
            LevelData level = LevelSamples.CreateLevel442();
            var solver = new WarnsdorffSolver();

            Assert.IsFalse(solver.TryContinue(level, new List<int>(), new int[level.ActiveCellCount], out int _));
        }

        [Test]
        public void APathThatRepeatsACellIsRefused()
        {
            LevelData level = LevelSamples.CreateLevel442();
            var solver = new WarnsdorffSolver();

            Assert.IsFalse(solver.TryContinue(
                level, new List<int> { 7, 8, 7 }, new int[level.ActiveCellCount], out int _));
        }

        [Test]
        public void ContinuingCostsLessThanSolvingFromScratch()
        {
            LevelData level = LevelSamples.CreateFullBoard(8, 8, fixedStart: 0);
            var solver = new WarnsdorffSolver();
            var buffer = new int[level.ActiveCellCount];

            solver.TrySolve(level, 0, buffer, out int fullLength);
            long fromScratch = solver.LastNodeCount;

            // Hand back most of that solution and ask only for the tail.
            var drawn = new List<int>();

            for (int i = 0; i < fullLength - 8; i++)
            {
                drawn.Add(buffer[i]);
            }

            Assert.IsTrue(solver.TryContinue(level, drawn, buffer, out int _));
            Assert.Less(solver.LastNodeCount, fromScratch, "a near finished board should be quick to finish");
        }
    }
}
