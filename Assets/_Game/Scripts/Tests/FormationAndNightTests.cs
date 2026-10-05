using ASTeams.SingleLine.Import;
using NUnit.Framework;

namespace ASTeams.SingleLine.Core.Tests
{
    public sealed class FormationAndNightTests
    {
        // A 2 x 2 board, every square open, starting top left:
        //   0 1
        //   2 3
        private static LevelData Square()
        {
            return new LevelData("square", 1, new Grid(2, 2), new[] { 0, 1, 2, 3 }, 0, LevelData.NoCell, new[] { 0, 1, 3, 2 });
        }

        // The same board in formation, 4 x 2, the player's half on the left:
        //   0 1 | 2 3
        //   4 5 | 6 7
        private static LevelData Formation()
        {
            return new FormationVariant().Apply(Square());
        }

        [Test]
        public void FormationSetsTheBoardBesideItsMirror()
        {
            LevelData level = Formation();

            Assert.IsTrue(level.IsFormation);
            Assert.AreEqual(4, level.Grid.Width);
            Assert.AreEqual(8, level.ActiveCellCount);
            Assert.AreEqual(4, level.PathLength);
            Assert.AreEqual(3, level.MirrorOf(0));
            Assert.AreEqual(6, level.MirrorOf(5));
            CollectionAssert.AreEqual(new[] { 0, 1, 5, 4 }, level.Solution);
        }

        [Test]
        public void TheWingmanCoversTheMirrorSquare()
        {
            var session = new PathSession(Formation());

            Assert.AreEqual(MoveResult.Started, session.Move(0));
            Assert.IsTrue(session.IsVisited(3));
            Assert.AreEqual(2, session.CoveredCount);

            Assert.AreEqual(MoveResult.Moved, session.Move(1));
            Assert.IsTrue(session.IsVisited(2));

            session.Undo();
            Assert.IsFalse(session.IsVisited(2));
        }

        [Test]
        public void ThePlanesCannotCrossTheMiddle()
        {
            var session = new PathSession(Formation());
            session.Move(0);
            session.Move(1);

            // Square 2 is where the wingman already is.
            Assert.AreEqual(MoveResult.Rejected, session.Move(2));
        }

        [Test]
        public void FlyingTheHalfWinsTheWholeBoard()
        {
            var session = new PathSession(Formation());

            foreach (int cell in new[] { 0, 1, 5, 4 })
            {
                session.Move(cell);
            }

            Assert.AreEqual(PathState.Won, session.State);
            Assert.AreEqual(8, session.CoveredCount);
        }

        [Test]
        public void TheSolverAndValidatorUnderstandFormation()
        {
            LevelData level = Formation();
            var path = new int[level.ActiveCellCount];

            Assert.IsTrue(new WarnsdorffSolver().TrySolve(level, 0, path, out int length));
            Assert.AreEqual(4, length);
            Assert.IsTrue(new LevelValidator(new WarnsdorffSolver()).Validate(level).IsValid);
        }

        [Test]
        public void FormationSkipsBoardsWithOtherConditions()
        {
            LevelData withRunway = new RunwayVariant().Apply(Square());

            Assert.AreSame(withRunway, new FormationVariant().Apply(withRunway));
        }

        [Test]
        public void NightMarksOnlyShortRoutes()
        {
            Assert.IsTrue(new NightVariant().Apply(Square()).IsNight);
            Assert.IsFalse(new NightVariant(3).Apply(Square()).IsNight);
            Assert.AreEqual(LevelRule.Night, new NightVariant().Apply(Square()).Rules);
        }

        [Test]
        public void TheNewConditionsSurviveTheJsonRoundTrip()
        {
            LevelData level = new NightVariant().Apply(Formation());
            LevelData copy = ASTeams.SingleLine.Data.LevelJsonSerializer.FromDto(ASTeams.SingleLine.Data.LevelJsonSerializer.ToDto(level));

            Assert.AreEqual(LevelRule.Night | LevelRule.Formation, copy.Rules);
        }

        [Test]
        public void NightAndFormationTakeRestsInAlternateCities()
        {
            RulePlan plan = RulePlan.Default;

            Assert.AreEqual(67, plan.FirstLevelWith(LevelRule.Night));
            Assert.AreEqual(154, plan.FirstLevelWith(LevelRule.Formation));
            Assert.AreEqual(LevelRule.None, plan.RulesFor(77, false));
            Assert.AreEqual(LevelRule.None, plan.RulesFor(67, true));
        }

        [Test]
        public void TheLastStretchPairsTheRests()
        {
            RulePlan plan = RulePlan.Default;

            Assert.AreEqual(LevelRule.Night, plan.RulesFor(267, false));
            Assert.AreEqual(LevelRule.Night | LevelRule.Runway, plan.RulesFor(287, false));
            Assert.AreEqual(LevelRule.Formation | LevelRule.Night, plan.RulesFor(294, false));
        }

        [Test]
        public void VipMixesOnlyUseConditionsAlreadyMet()
        {
            RulePlan plan = RulePlan.Default;

            CollectionAssert.AreEqual(new[] { LevelRule.Runway }, plan.VipMixes(29, 1));
            CollectionAssert.Contains(plan.VipMixes(89, 3), LevelRule.Runway | LevelRule.Night);
            CollectionAssert.DoesNotContain(plan.VipMixes(400, 14), LevelRule.Formation | LevelRule.Runway);
            CollectionAssert.Contains(plan.VipMixes(400, 14), LevelRule.Formation | LevelRule.Night);
        }
    }
}
