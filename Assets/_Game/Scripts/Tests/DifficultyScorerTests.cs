using System.Collections.Generic;
using ASTeams.SingleLine.Core;
using ASTeams.SingleLine.Import;
using NUnit.Framework;

namespace ASTeams.SingleLine.Core.Tests
{
    public sealed class DifficultyScorerTests
    {
        private static LevelData Line(string id, int length, int start)
        {
            var cells = new int[length];

            for (int i = 0; i < length; i++)
            {
                cells[i] = i;
            }

            return new LevelData(id, 1, new Grid(length, 1), cells, start);
        }

        private static DifficultyScorer CreateScorer()
        {
            return new DifficultyScorer(new WarnsdorffSolver());
        }

        [Test]
        public void PercentileRanks_SpreadDistinctValuesFromZeroToOne()
        {
            double[] ranks = DifficultyScorer.PercentileRanks(new double[] { 10, 20, 30 });

            Assert.AreEqual(0.0, ranks[0], 1e-9);
            Assert.AreEqual(0.5, ranks[1], 1e-9);
            Assert.AreEqual(1.0, ranks[2], 1e-9);
        }

        [Test]
        public void PercentileRanks_GiveTiedValuesTheSameRank()
        {
            double[] ranks = DifficultyScorer.PercentileRanks(new double[] { 5, 5, 9 });

            Assert.AreEqual(ranks[0], ranks[1], 1e-9, "equal input must not be split apart");
            Assert.Less(ranks[1], ranks[2]);
        }

        [Test]
        public void PercentileRanks_HandleASingleValue()
        {
            double[] ranks = DifficultyScorer.PercentileRanks(new double[] { 42 });

            Assert.AreEqual(1, ranks.Length);
            Assert.AreEqual(0.0, ranks[0], 1e-9);
        }

        [Test]
        public void PercentileRanks_AreNotDisturbedByInputOrder()
        {
            double[] ascending = DifficultyScorer.PercentileRanks(new double[] { 1, 2, 3, 4 });
            double[] shuffled = DifficultyScorer.PercentileRanks(new double[] { 3, 1, 4, 2 });

            Assert.AreEqual(ascending[0], shuffled[1], 1e-9);
            Assert.AreEqual(ascending[3], shuffled[2], 1e-9);
        }

        [Test]
        public void ToDifficulty_CoversOneThroughTen()
        {
            Assert.AreEqual(1, DifficultyScorer.ToDifficulty(0.0));
            Assert.AreEqual(6, DifficultyScorer.ToDifficulty(0.5));
            Assert.AreEqual(10, DifficultyScorer.ToDifficulty(1.0));
        }

        [Test]
        public void ToDifficulty_ClampsValuesOutsideTheRange()
        {
            Assert.AreEqual(1, DifficultyScorer.ToDifficulty(-0.5));
            Assert.AreEqual(10, DifficultyScorer.ToDifficulty(1.5));
        }

        [Test]
        public void EveryScore_LandsInsideTheRangeGddRequires()
        {
            var levels = new List<LevelData>
            {
                Line("a", 3, 0),
                Line("b", 6, 0),
                LevelSamples.CreateLevel442(withSolution: false),
                LevelSamples.CreateFullBoard(5, 5),
                LevelSamples.CreateFullBoard(8, 8)
            };

            foreach (ScoredLevel scored in CreateScorer().Score(levels))
            {
                Assert.GreaterOrEqual(scored.Difficulty, DifficultyScorer.MinDifficulty);
                Assert.LessOrEqual(scored.Difficulty, DifficultyScorer.MaxDifficulty);
            }
        }

        [Test]
        public void TheSmallestLevel_ScoresBelowTheLargest()
        {
            var levels = new List<LevelData>
            {
                Line("tiny", 3, 0),
                LevelSamples.CreateFullBoard(4, 4),
                LevelSamples.CreateLevel442(withSolution: false),
                LevelSamples.CreateFullBoard(9, 9)
            };

            List<ScoredLevel> scored = CreateScorer().Score(levels);

            Assert.Less(scored[0].Difficulty, scored[3].Difficulty);
        }

        [Test]
        public void ScoringIsDeterministic()
        {
            var levels = new List<LevelData>
            {
                Line("a", 4, 0),
                LevelSamples.CreateLevel442(withSolution: false),
                LevelSamples.CreateFullBoard(6, 6)
            };

            List<ScoredLevel> first = CreateScorer().Score(levels);
            List<ScoredLevel> second = CreateScorer().Score(levels);

            for (int i = 0; i < first.Count; i++)
            {
                Assert.AreEqual(first[i].Difficulty, second[i].Difficulty, "level " + i);
                Assert.AreEqual(first[i].RawScore, second[i].RawScore, 1e-12);
            }
        }

        [Test]
        public void ResultOrder_MatchesTheInputOrder()
        {
            var levels = new List<LevelData> { Line("first", 3, 0), Line("second", 5, 0) };

            List<ScoredLevel> scored = CreateScorer().Score(levels);

            Assert.AreEqual("first", scored[0].Level.Id);
            Assert.AreEqual("second", scored[1].Level.Id);
        }

        [Test]
        public void EmptyInput_ProducesNoScores()
        {
            Assert.AreEqual(0, CreateScorer().Score(new List<LevelData>()).Count);
        }
    }
}
