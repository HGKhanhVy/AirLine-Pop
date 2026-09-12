using System;
using System.Collections.Generic;
using System.Threading;
using ASTeams.SingleLine.Core;
using NUnit.Framework;

namespace ASTeams.SingleLine.Core.Tests
{
    public sealed class ContinuationHintServiceTests
    {
        private static LevelData Board()
        {
            return new LevelData("hint", 1, new Grid(3, 2), new[] { 0, 1, 2, 3, 4, 5 },
                fixedStart: 0, solution: new[] { 0, 1, 2, 5, 4, 3 });
        }

        private static HintResult Find(LevelData level, int[] path, int nodes = 200000)
        {
            return new ContinuationHintService(new WarnsdorffSolverFactory()).FindHintAsync(level, path, 3, nodes, 1000,
                CancellationToken.None).GetAwaiter().GetResult();
        }

        [Test]
        public void HintContinuesCurrentRouteWithoutChangingIt()
        {
            int[] path = { 0, 1, 2 };
            HintResult result = Find(Board(), path);
            Assert.That(result.BacktrackCount, Is.Zero);
            Assert.That(result.Steps, Is.EqualTo(new[] { 5, 4, 3 }));
            Assert.That(path, Is.EqualTo(new[] { 0, 1, 2 }));
        }

        [Test]
        public void WorkerOwnsSnapshotEvenWhenCallerContinuesDrawing()
        {
            var path = new List<int> { 0, 1, 2 };
            var task = new ContinuationHintService(new WarnsdorffSolverFactory())
                .FindHintAsync(Board(), path, 3, 200000, 1000, CancellationToken.None);
            path.Clear();
            path.Add(5);
            HintResult result = task.GetAwaiter().GetResult();
            Assert.That(result.BacktrackCount, Is.Zero);
            Assert.That(result.Steps, Is.EqualTo(new[] { 5, 4, 3 }));
        }

        [Test]
        public void WrongTurnReturnsMinimumUndoCountAndPlayableSteps()
        {
            // The middle cell separates the two remaining corners.
            HintResult result = Find(Board(), new[] { 0, 1, 4 });
            Assert.That(result.BacktrackCount, Is.EqualTo(1));
            Assert.That(result.Steps, Is.EqualTo(new[] { 2, 5, 4 }));
            Assert.That(result.NeedsRestart, Is.False);
        }

        [Test]
        public void ExhaustedBudgetFallsBackToValidatedStoredRoute()
        {
            HintResult result = Find(Board(), new[] { 0, 1, 4 }, 1);
            Assert.That(result.BacktrackCount, Is.EqualTo(1));
            Assert.That(result.Steps, Is.EqualTo(new[] { 2, 5, 4 }));
        }

        [Test]
        public void InvalidStoredSolutionNeverBecomesAHint()
        {
            LevelData level = Board().WithSolution(new[] { 0, 2, 1, 3, 4, 5 });
            Assert.That(Find(level, new[] { 0 }, 1).NeedsRestart, Is.True);
        }

        [Test]
        public void ExhaustedBudgetWithoutStoredRouteSuggestsRestart()
        {
            Assert.That(Find(Board().WithSolution(null), new[] { 0 }, 1).NeedsRestart, Is.True);
        }

        [Test]
        public void CancelledRequestCannotReturnAStaleHint()
        {
            using (var cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();
                Assert.Catch<OperationCanceledException>(() =>
                    new ContinuationHintService(new WarnsdorffSolverFactory()).FindHintAsync(Board(), new[] { 0 }, 3,
                        200000, 1000, cancellation.Token).GetAwaiter().GetResult());
            }
        }

        [Test]
        public void TouchingAnEarlierStartCannotResetThePath()
        {
            var session = new PathSession(Board());
            session.Move(0);
            session.Move(1);
            session.Move(4);
            Assert.That(session.Move(0), Is.EqualTo(MoveResult.Rejected));
            Assert.That(session.Length, Is.EqualTo(3));
            Assert.That(session.Head, Is.EqualTo(4));
            Assert.That(session.Move(1), Is.EqualTo(MoveResult.Backtracked));
            Assert.That(session.Move(0), Is.EqualTo(MoveResult.Backtracked));
            Assert.That(session.Length, Is.EqualTo(1));
        }

        [TestCase(new[] { 0, 2 })]
        [TestCase(new[] { 1, 2 })]
        [TestCase(new[] { 0, 1, 4, 3, 0, 1, 2 })]
        public void SolverRejectsMalformedPrefixes(int[] path)
        {
            Assert.That(new WarnsdorffSolver().TryContinue(Board(), path, new int[6], out int length), Is.False);
            Assert.That(length, Is.Zero);
        }

        [Test]
        public void SolverHonoursCancellationDuringSearch()
        {
            using (var cancellation = new CancellationTokenSource())
            {
                var solver = new WarnsdorffSolver(200000, cancellation.Token);
                cancellation.Cancel();
                Assert.Throws<OperationCanceledException>(() => solver.TryContinue(Board(), new[] { 0 }, new int[6], out int length));
            }
        }
    }
}
