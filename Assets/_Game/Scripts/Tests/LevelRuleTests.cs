using ASTeams.SingleLine.Import;
using NUnit.Framework;

namespace ASTeams.SingleLine.Core.Tests
{
    public sealed class LevelRuleTests
    {
        // A 3 x 2 board, every square open, starting top left:
        //   0 1 2
        //   3 4 5
        private static LevelData Board(int fixedEnd = LevelData.NoCell, WindCell[] wind = null)
        {
            return new LevelData("rules", 1, new Grid(3, 2), new[] { 0, 1, 2, 3, 4, 5 }, 0, fixedEnd,
                new[] { 0, 1, 2, 5, 4, 3 }, wind: wind);
        }

        [Test]
        public void WindOnlyLetsThePlaneLeaveTheWayItBlows()
        {
            LevelData level = Board(wind: new[] { new WindCell(1, Direction.Right) });
            var visited = new byte[6];
            visited[0] = visited[1] = 1;

            Assert.IsTrue(PathRules.CanEnter(level, visited, 0, 1, 2));
            Assert.IsFalse(PathRules.CanEnter(level, visited, 0, 1, 4));
        }

        [Test]
        public void NothingFliesOnFromTheRunway()
        {
            LevelData level = Board(fixedEnd: 2);
            var visited = new byte[6];
            visited[0] = visited[1] = visited[2] = 1;

            Assert.IsFalse(PathRules.CanEnter(level, visited, 1, 2, 5));
            Assert.AreEqual(0, PathRules.CountAvailableMoves(level, visited, 1, 2));
        }

        [Test]
        public void TheSolverHonoursRunwayAndWind()
        {
            LevelData level = Board(fixedEnd: 3, wind: new[] { new WindCell(1, Direction.Right) });
            var solver = new WarnsdorffSolver();
            var path = new int[6];

            Assert.IsTrue(solver.TrySolve(level, 0, path, out int length));
            Assert.AreEqual(6, length);
            Assert.AreEqual(3, path[5]);
            Assert.AreEqual(2, path[2]);
        }

        [Test]
        public void VariantsKeepTheSolutionFlyable()
        {
            LevelData plain = Board();
            LevelData decorated = new WindVariant().Apply(new RunwayVariant().Apply(plain));

            Assert.IsTrue(decorated.HasFixedEnd);
            Assert.AreEqual(3, decorated.FixedEnd);
            Assert.IsTrue(PathSequenceValidator.IsValid(decorated, decorated.Solution, new byte[6], true));
        }

        [Test]
        public void RulesUnlockAlongTheRouteAndSkipPlainSlots()
        {
            RulePlan plan = RulePlan.Default;

            Assert.AreEqual(LevelRule.None, plan.RulesFor(5, false), "onboarding stays plain");
            Assert.AreEqual(LevelRule.None, plan.RulesFor(11, false));
            Assert.AreEqual(LevelRule.Runway, plan.RulesFor(12, false), "a condition opens on its own");
            Assert.AreEqual(LevelRule.Runway, plan.RulesFor(13, false), "and its city practises it");
            Assert.AreEqual(LevelRule.Runway, plan.RulesFor(19, false), "the storm too");
            Assert.AreEqual(LevelRule.None, plan.RulesFor(20, false), "the landing stays plain");
            Assert.AreEqual(LevelRule.None, plan.RulesFor(22, true));
            Assert.AreEqual(LevelRule.Wind, plan.RulesFor(22, false));
            Assert.AreEqual(12, plan.FirstLevelWith(LevelRule.Runway));
            Assert.AreEqual(22, plan.FirstLevelWith(LevelRule.Wind));
            Assert.AreEqual(72, plan.FirstLevelWith(LevelRule.Bridge));
            Assert.AreEqual(FlightRole.Storm, plan.RoleOf(89));
            Assert.AreEqual(FlightRole.Rest, plan.RoleOf(84));
        }

        [Test]
        public void WindSurvivesTheJsonRoundTrip()
        {
            LevelData level = Board(fixedEnd: 3, wind: new[] { new WindCell(1, Direction.Right) });
            LevelData copy = ASTeams.SingleLine.Data.LevelJsonSerializer.FromDto(ASTeams.SingleLine.Data.LevelJsonSerializer.ToDto(level));

            Assert.AreEqual(3, copy.FixedEnd);
            Assert.IsTrue(copy.TryGetWind(1, out Direction direction));
            Assert.AreEqual(Direction.Right, direction);
            Assert.AreEqual(LevelRule.Runway | LevelRule.Wind, copy.Rules);
        }
    }
}
