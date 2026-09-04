using System.Collections.Generic;
using ASTeams.SingleLine.Core;
using ASTeams.SingleLine.Import;
using NUnit.Framework;

namespace ASTeams.SingleLine.Core.Tests
{
    public sealed class LevelPreparerTests
    {
        private static LevelPreparer CreatePreparer()
        {
            return new LevelPreparer(new LevelValidator(new WarnsdorffSolver()));
        }

        private static void AssertSolutionPlays(LevelData level)
        {
            Assert.IsTrue(level.HasSolution, level.Id + " has no solution");

            var session = new PathSession(level);

            foreach (int cell in level.Solution)
            {
                Assert.AreNotEqual(MoveResult.Rejected, session.Move(cell), level.Id + " has an illegal solution");
            }

            Assert.AreEqual(PathState.Won, session.State, level.Id + " does not finish");
        }

        [Test]
        public void ALevelWithNoSolutionGetsOneFromTheSolver()
        {
            LevelData bare = LevelSamples.CreateLevel442(withSolution: false);

            PreparedPool pool = CreatePreparer().Prepare(new List<LevelData> { bare });

            Assert.AreEqual(1, pool.Levels.Count);
            Assert.AreEqual(1, pool.SolvedCount);
            Assert.AreEqual(0, pool.RepairedCount);
            AssertSolutionPlays(pool.Levels[0]);
        }

        [Test]
        public void ABrokenSolutionIsReplaced()
        {
            var broken = (int[])LevelSamples.Level442Solution.Clone();
            broken[3] = LevelSamples.Level442Solution[20];
            broken[20] = LevelSamples.Level442Solution[3];

            var level = new LevelData(
                "broken", 1, new Grid(6, 5), LevelSamples.Level442Cells, solution: broken, difficulty: 4);

            PreparedPool pool = CreatePreparer().Prepare(new List<LevelData> { level });

            Assert.AreEqual(1, pool.RepairedCount);
            AssertSolutionPlays(pool.Levels[0]);
        }

        [Test]
        public void AGoodSolutionIsLeftExactlyAsItWas()
        {
            LevelData original = LevelSamples.CreateLevel442();

            PreparedPool pool = CreatePreparer().Prepare(new List<LevelData> { original });

            Assert.AreEqual(0, pool.SolvedCount);
            Assert.AreEqual(0, pool.RepairedCount);
            Assert.AreSame(original, pool.Levels[0], "a level that needs nothing must not be rebuilt");
        }

        [Test]
        public void AnUnsolvableLevelIsDroppedAndNamed()
        {
            // A plus shape has four arms and only two path ends, so it cannot be covered.
            var impossible = new LevelData("plus", 1, new Grid(3, 3), new[] { 1, 3, 4, 5, 7 });

            PreparedPool pool = CreatePreparer().Prepare(new List<LevelData> { impossible });

            Assert.AreEqual(0, pool.Levels.Count);
            Assert.AreEqual(1, pool.DroppedIds.Count);
            Assert.AreEqual("plus", pool.DroppedIds[0]);
        }

        [Test]
        public void ALevelPinnedToAStartThatStrandsTheBoardIsDropped()
        {
            var level = new LevelData("middle", 1, new Grid(3, 1), new[] { 0, 1, 2 }, fixedStart: 1);

            PreparedPool pool = CreatePreparer().Prepare(new List<LevelData> { level });

            Assert.AreEqual(0, pool.Levels.Count);
            Assert.AreEqual(1, pool.DroppedIds.Count);
        }

        [Test]
        public void EveryKeptLevelCarriesAPlayableSolution()
        {
            var levels = new List<LevelData>
            {
                LevelSamples.CreateLevel442(),
                LevelSamples.CreateLevel442(withSolution: false),
                LevelSamples.CreateFullBoard(4, 4),
                LevelSamples.CreateFullBoard(5, 4, fixedStart: 0),
                new LevelData("plus", 1, new Grid(3, 3), new[] { 1, 3, 4, 5, 7 })
            };

            PreparedPool pool = CreatePreparer().Prepare(levels);

            Assert.AreEqual(4, pool.Levels.Count);

            foreach (LevelData level in pool.Levels)
            {
                AssertSolutionPlays(level);
            }
        }

        [Test]
        public void OrderIsPreserved()
        {
            var levels = new List<LevelData>
            {
                LevelSamples.CreateFullBoard(3, 3).WithIdentity("first", 1),
                LevelSamples.CreateFullBoard(4, 3).WithIdentity("second", 1)
            };

            PreparedPool pool = CreatePreparer().Prepare(levels);

            Assert.AreEqual("first", pool.Levels[0].Id);
            Assert.AreEqual("second", pool.Levels[1].Id);
        }

        [Test]
        public void EmptyInputProducesAnEmptyPool()
        {
            PreparedPool pool = CreatePreparer().Prepare(new List<LevelData>());

            Assert.AreEqual(0, pool.Levels.Count);
            Assert.AreEqual(0, pool.DroppedIds.Count);
        }
    }
}
