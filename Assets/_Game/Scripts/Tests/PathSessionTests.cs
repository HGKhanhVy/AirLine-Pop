using ASTeams.SingleLine.Core;
using NUnit.Framework;

namespace ASTeams.SingleLine.Core.Tests
{
    public sealed class PathSessionTests
    {
        /// <summary>A three cell line started in the middle: one step and the path is stuck.</summary>
        private static PathSession CreateLineStartedInTheMiddle()
        {
            var level = new LevelData("line", 1, new Grid(3, 1), new[] { 0, 1, 2 });
            var session = new PathSession(level);
            session.Move(1);
            return session;
        }

        [Test]
        public void NewSession_IsReadyAndEmpty()
        {
            var session = new PathSession(LevelSamples.CreateLevel442());

            Assert.AreEqual(PathState.Ready, session.State);
            Assert.AreEqual(0, session.Length);
            Assert.AreEqual(LevelData.NoCell, session.Head);
        }

        [Test]
        public void Move_OnAnEmptyPath_Starts()
        {
            var session = new PathSession(LevelSamples.CreateLevel442());

            Assert.AreEqual(MoveResult.Started, session.Move(LevelSamples.Level442Start));
            Assert.AreEqual(PathState.Drawing, session.State);
            Assert.AreEqual(1, session.Length);
            Assert.AreEqual(LevelSamples.Level442Start, session.Head);
        }

        [Test]
        public void Move_ToAHole_IsRejectedAndChangesNothing()
        {
            var session = new PathSession(LevelSamples.CreateLevel442());
            session.Move(9);

            Assert.AreEqual(MoveResult.Rejected, session.Move(3));
            Assert.AreEqual(1, session.Length, "MOV-06 forbids resetting on illegal input");
            Assert.AreEqual(9, session.Head);
        }

        [Test]
        public void Move_ToANonAdjacentCell_IsRejected()
        {
            var session = new PathSession(LevelSamples.CreateLevel442());
            session.Move(7);

            Assert.AreEqual(MoveResult.Rejected, session.Move(29));
            Assert.AreEqual(1, session.Length);
        }

        [Test]
        public void Move_ToTheHeadItself_IsANoOp()
        {
            var session = new PathSession(LevelSamples.CreateLevel442());
            session.Move(7);

            Assert.AreEqual(MoveResult.Rejected, session.Move(7));
            Assert.AreEqual(1, session.Length);
            Assert.AreEqual(PathState.Drawing, session.State, "re-touching the head must not disturb the path");
        }

        [Test]
        public void Move_BackToThePreviousCell_Backtracks()
        {
            var session = new PathSession(LevelSamples.CreateLevel442());
            session.Move(7);
            session.Move(8);

            Assert.AreEqual(MoveResult.Backtracked, session.Move(7));
            Assert.AreEqual(1, session.Length);
            Assert.AreEqual(7, session.Head);
            Assert.IsFalse(session.IsVisited(8));
        }

        [Test]
        public void Move_ToAnEarlierCellThatIsNotThePrevious_IsRejected()
        {
            var session = new PathSession(LevelSamples.CreateFullBoard(3, 3));
            session.Move(0);
            session.Move(1);
            session.Move(2);
            session.Move(5);
            session.Move(4);

            // 1 is adjacent to the head at 4 and is visited, but it is not the immediate
            // predecessor (5 is), so MOV-02 forbids it rather than treating it as a backtrack.
            Assert.AreEqual(MoveResult.Rejected, session.Move(1));
            Assert.AreEqual(5, session.Length);
            Assert.AreEqual(MoveResult.Backtracked, session.Move(5));
        }

        [Test]
        public void Undo_RemovesTheLastCell()
        {
            var session = new PathSession(LevelSamples.CreateLevel442());
            session.Move(7);
            session.Move(8);

            Assert.IsTrue(session.Undo());
            Assert.AreEqual(1, session.Length);
            Assert.AreEqual(7, session.Head);
        }

        [Test]
        public void Undo_OnAnEmptyPath_ReturnsFalse()
        {
            var session = new PathSession(LevelSamples.CreateLevel442());

            Assert.IsFalse(session.Undo());
            Assert.AreEqual(PathState.Ready, session.State);
        }

        [Test]
        public void Restart_ClearsThePathWithoutTouchingTheLevel()
        {
            var session = new PathSession(LevelSamples.CreateLevel442());
            session.Move(7);
            session.Move(8);
            session.Move(2);

            session.Restart();

            Assert.AreEqual(0, session.Length);
            Assert.AreEqual(PathState.Ready, session.State);
            Assert.IsFalse(session.IsVisited(7));
            Assert.AreEqual(25, session.Level.ActiveCellCount);
        }

        [Test]
        public void ReplayingTheShippedSolution_Wins()
        {
            var session = new PathSession(LevelSamples.CreateLevel442());
            int[] solution = LevelSamples.Level442Solution;

            for (int step = 0; step < solution.Length - 1; step++)
            {
                MoveResult result = session.Move(solution[step]);
                Assert.AreNotEqual(MoveResult.Rejected, result, "step " + step + " was rejected");
            }

            Assert.AreEqual(MoveResult.Completed, session.Move(solution[solution.Length - 1]));
            Assert.AreEqual(PathState.Won, session.State);
            Assert.AreEqual(25, session.Length);
        }

        [Test]
        public void Move_AfterWinning_IsRejected()
        {
            var level = new LevelData("line", 1, new Grid(3, 1), new[] { 0, 1, 2 });
            var session = new PathSession(level);
            session.Move(0);
            session.Move(1);
            session.Move(2);

            Assert.AreEqual(PathState.Won, session.State);
            Assert.AreEqual(MoveResult.Rejected, session.Move(1));
            Assert.IsFalse(session.Undo(), "a won level is terminal until it is restarted");
        }

        [Test]
        public void DeadEnd_EntersStuckWithoutEndingTheLevel()
        {
            PathSession session = CreateLineStartedInTheMiddle();

            session.Move(0);

            Assert.AreEqual(PathState.Stuck, session.State);
            Assert.AreEqual(2, session.Length, "a stuck board is not reset");
        }

        [Test]
        public void BacktrackingOutOfStuck_ReturnsToDrawing()
        {
            PathSession session = CreateLineStartedInTheMiddle();
            session.Move(0);

            Assert.IsTrue(session.Undo());
            Assert.AreEqual(PathState.Drawing, session.State);
        }

        [Test]
        public void Move_RespectsAPinnedStart()
        {
            var session = new PathSession(LevelSamples.CreateFullBoard(3, 3, fixedStart: 4));

            Assert.AreEqual(MoveResult.Rejected, session.Move(0));
            Assert.AreEqual(MoveResult.Started, session.Move(4));
        }

        [Test]
        public void Events_ReportEveryVisitAndStateChange()
        {
            var session = new PathSession(LevelSamples.CreateLevel442());
            int visited = 0;
            int unvisited = 0;
            int stateChanges = 0;

            session.OnCellVisited += _ => visited++;
            session.OnCellUnvisited += _ => unvisited++;
            session.OnStateChanged += (_, _) => stateChanges++;

            session.Move(7);
            session.Move(8);
            session.Move(7);

            Assert.AreEqual(2, visited);
            Assert.AreEqual(1, unvisited);
            Assert.AreEqual(1, stateChanges, "Ready to Drawing only; the rest stayed in Drawing");
        }

        [Test]
        public void GetCell_ReturnsTheDrawnOrder()
        {
            var session = new PathSession(LevelSamples.CreateLevel442());
            session.Move(7);
            session.Move(8);

            Assert.AreEqual(7, session.GetCell(0));
            Assert.AreEqual(8, session.GetCell(1));
        }
    }
}
