using System.Collections.Generic;
using ASTeams.SingleLine.Core;
using NUnit.Framework;

namespace ASTeams.SingleLine.Core.Tests
{
    public sealed class PassengerPlannerTests
    {
        private static LevelData Level442WithStart()
        {
            return new LevelData("medium_442", 1, new Grid(6, 5), LevelSamples.Level442Cells,
                fixedStart: LevelSamples.Level442Start);
        }

        [Test]
        public void Plan_IsTheSameForTheSameLevel()
        {
            PassengerPlan first = PassengerPlanner.Plan(Level442WithStart());
            PassengerPlan second = PassengerPlanner.Plan(Level442WithStart());

            CollectionAssert.AreEqual(first.Cells, second.Cells);
            Assert.AreEqual(first.Seed, second.Seed);
        }

        [Test]
        public void Plan_NeverSeatsACatOnTheStart()
        {
            PassengerPlan plan = PassengerPlanner.Plan(Level442WithStart());

            Assert.IsFalse(plan.Contains(LevelSamples.Level442Start));
        }

        [Test]
        public void Plan_SeatsDistinctActiveSquares()
        {
            LevelData level = Level442WithStart();
            PassengerPlan plan = PassengerPlanner.Plan(level);
            var seen = new HashSet<int>();

            foreach (int cell in plan.Cells)
            {
                Assert.IsTrue(level.IsActive(cell), "Cat seated on a hole: " + cell);
                Assert.IsTrue(seen.Add(cell), "Two cats on square " + cell);
            }
        }

        [Test]
        public void Plan_ScalesWithTheBoardWithinBounds()
        {
            // 25 squares at one cat per seven gives three.
            Assert.AreEqual(3, PassengerPlanner.Plan(Level442WithStart()).Count);

            var big = new int[90];
            for (int i = 0; i < big.Length; i++)
            {
                big[i] = i;
            }

            var bigLevel = new LevelData("big", 1, new Grid(10, 9), big, fixedStart: 0);
            Assert.AreEqual(PassengerPlanner.MaxPassengers, PassengerPlanner.Plan(bigLevel).Count);
        }

        [Test]
        public void Plan_OnATinyBoardSeatsNoMoreCatsThanFreeSquares()
        {
            var tiny = new LevelData("tiny", 1, new Grid(2, 1), new[] { 0, 1 }, fixedStart: 0);

            PassengerPlan plan = PassengerPlanner.Plan(tiny);

            Assert.AreEqual(1, plan.Count);
            Assert.AreEqual(1, plan.Cells[0]);
        }

        [Test]
        public void Plan_KeepsCatsApartFromEachOther()
        {
            PassengerPlan plan = PassengerPlanner.Plan(Level442WithStart());
            var grid = new Grid(6, 5);

            for (int a = 0; a < plan.Count; a++)
            {
                for (int b = a + 1; b < plan.Count; b++)
                {
                    Assert.IsFalse(grid.AreAdjacent(plan.Cells[a], plan.Cells[b]),
                        "Cats on neighbouring squares " + plan.Cells[a] + " and " + plan.Cells[b]);
                }
            }
        }
    }
}
