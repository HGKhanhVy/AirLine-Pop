using System;
using System.Collections.Generic;
using AirlinePop.SingleLine.Import;
using NUnit.Framework;

namespace AirlinePop.SingleLine.Core.Tests
{
    public sealed class GeneratedCampaignTests
    {
        [Test]
        public void GeneratedBoardsAreSolvedByTheirOwnRoute()
        {
            var generator = new BoardGenerator(new BoardGeneratorSettings(6, 7, 15, 42, 1.0, 1.5, 0, 2, 0.3));
            var random = new Random(7);
            int boards = 0;

            for (int i = 0; i < 200; i++)
            {
                LevelData level = generator.TryGenerate("g" + i, random);

                if (level == null)
                {
                    continue;
                }

                boards++;
                Assert.IsTrue(PathSequenceValidator.IsValid(level, level.Solution, new byte[level.Grid.CellCount], true), level.Id);
                Assert.IsTrue(new LevelValidator(new WarnsdorffSolver()).Validate(level).IsValid, level.Id);
            }

            Assert.Greater(boards, 50);
        }

        [Test]
        public void OverpassesSitInsideTheBoardAndNeverTouch()
        {
            var generator = new BoardGenerator(new BoardGeneratorSettings(6, 7, 15, 42, 1.0, 1.5, 1, 2, 0.4));
            var random = new Random(11);
            int bridged = 0;

            for (int i = 0; i < 300; i++)
            {
                LevelData level = generator.TryGenerate("x" + i, random);

                if (level == null)
                {
                    continue;
                }

                bridged++;
                Assert.GreaterOrEqual(level.BridgeCount, 1);

                foreach (CellMark mark in level.Marks)
                {
                    for (int direction = 0; direction < Grid.NeighborCount; direction++)
                    {
                        Assert.IsTrue(level.Grid.TryGetNeighbor(mark.Cell, (Direction)direction, out int neighbour));
                        Assert.IsTrue(level.IsActive(neighbour));
                        Assert.AreNotEqual(CellKind.Bridge, level.KindOf(neighbour));
                    }
                }
            }

            Assert.Greater(bridged, 10);
        }

        [Test]
        public void MarksFollowTheSolution()
        {
            var generator = new BoardGenerator(new BoardGeneratorSettings(6, 6, 20, 36, 1.0, 1.5));
            var random = new Random(3);

            foreach (CellKind kind in new[] { CellKind.Straight, CellKind.Beacon })
            {
                var variant = new MarkVariant(kind);
                int marked = 0;

                for (int i = 0; i < 40; i++)
                {
                    LevelData level = generator.TryGenerate("m" + i, random);

                    if (level == null)
                    {
                        continue;
                    }

                    LevelData decorated = variant.Apply(level);

                    if (decorated.Marks.Count > 0)
                    {
                        marked++;
                    }

                    Assert.IsTrue(PathSequenceValidator.IsValid(decorated, decorated.Solution, new byte[36], true), decorated.Id);
                }

                Assert.Greater(marked, 10, kind.ToString());
            }
        }

        [Test]
        public void FormationGivesTheWingmanGroundOfItsOwn()
        {
            var generator = new BoardGenerator(new BoardGeneratorSettings(4, 6, 12, 24, 1.0, 1.5));
            var random = new Random(9);
            var formation = new FormationVariant();
            int built = 0;

            for (int i = 0; i < 80; i++)
            {
                LevelData board = generator.TryGenerate("w" + i, random);

                if (board == null)
                {
                    continue;
                }

                LevelData level = formation.Apply(board);

                if (!level.IsFormation)
                {
                    continue;
                }

                built++;
                Assert.IsTrue(PathSequenceValidator.IsValid(level, level.Solution, new byte[level.Grid.CellCount], true), level.Id);

                bool differs = level.WingSideCellCount != level.PlayerSideCellCount;

                for (int cell = 0; cell < level.Grid.CellCount && !differs; cell++)
                {
                    if (level.IsActive(cell) && !level.IsOnWingSide(cell) && !level.IsActive(level.MirrorOf(cell)))
                    {
                        differs = true;
                    }
                }

                Assert.IsTrue(differs, level.Id + " is a plain mirror");
                Assert.IsTrue(new WarnsdorffSolver().TrySolve(level, level.FixedStart, new int[level.PathLength], out _), level.Id);
            }

            Assert.Greater(built, 10);
        }

        [Test]
        public void AnOpenCorridorIsSolvedWithoutAWrongTurn()
        {
            LevelData corridor = new LevelData("corridor", 1, new Grid(6, 1), new[] { 0, 1, 2, 3, 4, 5 }, 0);

            Assert.AreEqual(1.0, new ThoughtfulPlayer().MeasureTrials(corridor), 1e-9);
        }

        [Test]
        public void TargetsClimbAndStormsStandOut()
        {
            FlightTargets targets = FlightTargets.Default;
            FlightTarget early = targets.For(20, FlightRole.Climb, 4);
            FlightTarget late = targets.For(300, FlightRole.Climb, 4);
            FlightTarget storm = targets.For(300, FlightRole.Storm, 8);
            FlightTarget rest = targets.For(300, FlightRole.Rest, 3);

            Assert.Greater(late.Trials, early.Trials);
            Assert.Greater(late.Cells, early.Cells);
            Assert.Greater(storm.Trials, late.Trials);
            Assert.Less(rest.Trials, late.Trials);
        }

        [Test]
        public void ThePlannerFillsEveryFlightWithAFlyableBoard()
        {
            var pool = new List<LevelData>();
            var random = new Random(5);
            var sizes = new[] { (3, 3), (4, 4), (5, 5), (5, 6), (6, 6) };

            foreach ((int width, int height) in sizes)
            {
                var plain = new BoardGenerator(new BoardGeneratorSettings(width, height, Math.Max(3, width * height / 2), width * height));

                for (int i = 0; i < 60; i++)
                {
                    LevelData level = plain.TryGenerate("p" + width + height + i, random);

                    if (level != null)
                    {
                        pool.Add(level);
                    }
                }
            }

            var bridges = new BoardGenerator(new BoardGeneratorSettings(6, 6, 18, 36, 1.0, 1.5, 1, 2, 0.4));

            for (int i = 0; i < 200; i++)
            {
                LevelData level = bridges.TryGenerate("b" + i, random);

                if (level != null)
                {
                    pool.Add(level);
                }
            }

            var picker = new BoardPicker(BoardPicker.DefaultVariants, new ThoughtfulPlayer(3, 4000), 4);
            picker.Add(pool);
            var planner = new CampaignPlanner(RulePlan.Default, FlightTargets.Default, 90, 30, 3, 12);
            Campaign campaign = planner.Plan(picker, null);

            Assert.AreEqual(3, campaign.Chapters.Count);
            int number = 0;

            foreach (Chapter chapter in campaign.Chapters)
            {
                foreach (LevelData level in chapter.Levels)
                {
                    number++;
                    LevelRule wanted = RulePlan.Default.RulesFor(number, false);

                    if (!level.IsVip)
                    {
                        Assert.AreEqual(wanted, level.Rules & ~(LevelRule.Vip | LevelRule.Storm), "flight " + number);
                    }

                    Assert.AreEqual(RulePlan.Default.RoleOf(number) == FlightRole.Storm && !level.IsVip, level.IsStorm, "flight " + number);
                    Assert.IsTrue(PathSequenceValidator.IsValid(level, level.Solution, new byte[level.Grid.CellCount], true), level.Id);
                }
            }

            List<LevelData> days = new DailyFlightPlanner(7).Plan(picker);

            Assert.AreEqual(7, days.Count);
            Assert.AreEqual("daily_001", days[0].Id);
        }
    }
}
