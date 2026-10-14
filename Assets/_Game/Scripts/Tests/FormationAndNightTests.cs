using AirlinePop.SingleLine.Import;
using NUnit.Framework;

namespace AirlinePop.SingleLine.Core.Tests
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

        // A formation board, 6 x 2. The player's half on the left is full; the wingman's half on
        // the right has only three squares, so the wingman has to wait its way across:
        //   0 1 2 |  .  .  5
        //   6 7 8 |  . 10 11
        // From 0, a route like 0 6 7 8 2 1 takes the wingman over 5, 11 and 10; 0 1 2 8 7 6 leaves 10 behind.
        private static readonly int[] Route = { 0, 6, 7, 8, 2, 1 };

        private static LevelData Formation(int[] rightHalf = null)
        {
            var cells = new System.Collections.Generic.List<int> { 0, 1, 2, 6, 7, 8 };
            cells.AddRange(rightHalf ?? new[] { 5, 10, 11 });
            return new LevelData("formation", 1, new Grid(6, 2), cells.ToArray(), 0, LevelData.NoCell, Route,
                tags: new[] { LevelTags.Formation });
        }

        [Test]
        public void TheHalvesAreCountedApart()
        {
            LevelData level = Formation();

            Assert.IsTrue(level.IsFormation);
            Assert.AreEqual(6, level.PlayerSideCellCount);
            Assert.AreEqual(3, level.WingSideCellCount);
            Assert.AreEqual(6, level.PathLength);
            Assert.AreEqual(5, Wingman.StartFor(level, 0));
        }

        [Test]
        public void TheWingmanWaitsWhereItsHalfIsBlocked()
        {
            var session = new PathSession(Formation());

            Assert.AreEqual(MoveResult.Started, session.Move(0));
            Assert.AreEqual(5, session.WingHead);
            Assert.AreEqual(2, session.CoveredCount);

            // The player flies right, so the wingman would fly left onto 4: a hole, so it waits.
            Assert.AreEqual(MoveResult.Moved, session.Move(1));
            Assert.AreEqual(5, session.WingHead);
            Assert.AreEqual(3, session.CoveredCount);
        }

        [Test]
        public void UndoTakesTheWingmanBackToo()
        {
            var session = new PathSession(Formation());

            foreach (int cell in new[] { 0, 6, 7 })
            {
                session.Move(cell);
            }

            Assert.AreEqual(10, session.WingHead);
            session.Undo();
            Assert.AreEqual(11, session.WingHead);
            Assert.IsFalse(session.IsVisited(10));
        }

        [Test]
        public void FlyingBothHalvesWins()
        {
            var session = new PathSession(Formation());

            foreach (int cell in Route)
            {
                session.Move(cell);
            }

            Assert.AreEqual(PathState.Won, session.State);
            Assert.AreEqual(9, session.CoveredCount);
        }

        [Test]
        public void CoveringOnlyYourHalfIsNotAWin()
        {
            var session = new PathSession(Formation());

            foreach (int cell in new[] { 0, 1, 2, 8, 7, 6 })
            {
                session.Move(cell);
            }

            Assert.AreEqual(PathState.Stuck, session.State, "square 10 is left for the wingman");
        }

        [Test]
        public void ThePlayerNeverFliesOntoTheWingmansHalf()
        {
            var session = new PathSession(Formation(new[] { 3, 5, 9, 10, 11 }));
            session.Move(0);
            session.Move(1);
            session.Move(2);

            Assert.AreEqual(MoveResult.Rejected, session.Move(3));
        }

        [Test]
        public void TheSolverAndValidatorUnderstandFormation()
        {
            LevelData level = Formation();
            var path = new int[level.PathLength];

            Assert.IsTrue(new WarnsdorffSolver().TrySolve(level, 0, path, out int length));
            Assert.AreEqual(6, length);
            Assert.IsTrue(PathSequenceValidator.IsValid(level, path, new byte[12], true));
            Assert.IsTrue(new LevelValidator(new WarnsdorffSolver()).Validate(level).IsValid);
            Assert.IsFalse(PathSequenceValidator.IsValid(level, new[] { 0, 1, 2, 8, 7, 6 }, new byte[12], true));
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
            Assert.IsTrue(level.IsNight);
            LevelData copy = AirlinePop.SingleLine.Data.LevelJsonSerializer.FromDto(AirlinePop.SingleLine.Data.LevelJsonSerializer.ToDto(level));

            Assert.AreEqual(LevelRule.Night | LevelRule.Formation, copy.Rules);
        }

        [Test]
        public void NightAndFormationTakeRestsInAlternateCities()
        {
            RulePlan plan = RulePlan.Default;

            Assert.AreEqual(18, plan.FirstLevelWith(LevelRule.Night));
            Assert.AreEqual(22, plan.FirstLevelWith(LevelRule.Formation));
            Assert.AreEqual(LevelRule.Night, plan.RulesFor(27, false));
            Assert.AreEqual(LevelRule.None, plan.RulesFor(17, false));
            Assert.AreEqual(LevelRule.Formation, plan.RulesFor(34, false));
            Assert.AreEqual(LevelRule.None, plan.RulesFor(24, false));
            Assert.AreEqual(LevelRule.None, plan.RulesFor(18, true));
        }

        [Test]
        public void TheLastStretchPairsTheRests()
        {
            RulePlan plan = RulePlan.Default;

            Assert.AreEqual(LevelRule.Night, plan.RulesFor(67, false));
            Assert.AreEqual(LevelRule.Night | LevelRule.Runway, plan.RulesFor(87, false));
            Assert.AreEqual(LevelRule.Formation | LevelRule.Night, plan.RulesFor(94, false));
        }

        [Test]
        public void VipMixesOnlyUseConditionsAlreadyMet()
        {
            RulePlan plan = RulePlan.Default;

            CollectionAssert.Contains(plan.VipMixes(9, 1), LevelRule.Runway | LevelRule.Wind);
            CollectionAssert.DoesNotContain(plan.VipMixes(9, 1), LevelRule.Beacon, "the beacon only opens on flight 12");
            CollectionAssert.Contains(plan.VipMixes(89, 3), LevelRule.Runway | LevelRule.Night);
            CollectionAssert.DoesNotContain(plan.VipMixes(400, 14), LevelRule.Formation | LevelRule.Runway);
            CollectionAssert.Contains(plan.VipMixes(400, 14), LevelRule.Formation | LevelRule.Night);
        }
    }
}
