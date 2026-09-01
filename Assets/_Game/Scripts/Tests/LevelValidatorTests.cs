using System.Collections.Generic;
using ASTeams.SingleLine.Core;
using NUnit.Framework;

namespace ASTeams.SingleLine.Core.Tests
{
    public sealed class LevelValidatorTests
    {
        private static LevelValidator CreateValidator()
        {
            return new LevelValidator(new WarnsdorffSolver());
        }

        private static bool HasIssue(LevelValidationResult result, LevelIssueCode code)
        {
            IReadOnlyList<LevelIssue> issues = result.Issues;

            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].Code == code)
                {
                    return true;
                }
            }

            return false;
        }

        [Test]
        public void RippedLevel442_PassesWithItsShippedSolution()
        {
            LevelValidationResult result = CreateValidator().Validate(LevelSamples.CreateLevel442());

            Assert.IsTrue(result.IsValid, "unexpected issues: " + string.Join(", ", result.Issues));
            Assert.AreEqual(25, result.Solution.Count);
            Assert.AreEqual(0, result.SolverNodeCount, "a valid stored solution must not cost a search");
        }

        [Test]
        public void LevelWithoutAStoredSolution_GetsOneFromTheSolver()
        {
            LevelValidationResult result = CreateValidator()
                .Validate(LevelSamples.CreateLevel442(withSolution: false));

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(25, result.Solution.Count);
            Assert.Greater(result.SolverNodeCount, 0);
        }

        [Test]
        public void BrokenStoredSolution_IsReportedAndReplaced()
        {
            // Swap two entries so the path jumps across the board mid way.
            var broken = (int[])LevelSamples.Level442Solution.Clone();
            broken[3] = LevelSamples.Level442Solution[20];
            broken[20] = LevelSamples.Level442Solution[3];

            var level = new LevelData(
                "broken",
                1,
                new Grid(6, 5),
                LevelSamples.Level442Cells,
                solution: broken,
                difficulty: 4);

            LevelValidationResult result = CreateValidator().Validate(level);

            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(HasIssue(result, LevelIssueCode.SolutionNotContinuous));
            Assert.AreEqual(25, result.Solution.Count, "the validator hands back a repaired solution");
        }

        [Test]
        public void ShortStoredSolution_IsReportedAsWrongLength()
        {
            var level = new LevelData(
                "short",
                1,
                new Grid(3, 1),
                new[] { 0, 1, 2 },
                solution: new[] { 0, 1 });

            LevelValidationResult result = CreateValidator().Validate(level);

            Assert.IsTrue(HasIssue(result, LevelIssueCode.SolutionWrongLength));
        }

        [Test]
        public void DisconnectedBoard_IsReportedWithoutSpendingSolverBudget()
        {
            var level = new LevelData("split", 1, new Grid(3, 3), new[] { 0, 8 });

            LevelValidationResult result = CreateValidator().Validate(level);

            Assert.IsTrue(HasIssue(result, LevelIssueCode.DisconnectedBoard));
            Assert.AreEqual(0, result.Solution.Count);
            Assert.AreEqual(0, result.SolverNodeCount);
        }

        [Test]
        public void DuplicatedActiveCell_IsReported()
        {
            var level = new LevelData("dup", 1, new Grid(3, 1), new[] { 0, 1, 1, 2 });

            LevelValidationResult result = CreateValidator().Validate(level);

            Assert.IsTrue(HasIssue(result, LevelIssueCode.DuplicateActiveCell));
        }

        [Test]
        public void EmptyBoard_IsReported()
        {
            var level = new LevelData("empty", 1, new Grid(3, 3), new int[0]);

            LevelValidationResult result = CreateValidator().Validate(level);

            Assert.IsTrue(HasIssue(result, LevelIssueCode.EmptyBoard));
            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public void DifficultyOutsideOneToTen_IsReported()
        {
            var level = new LevelData("hard", 1, new Grid(3, 1), new[] { 0, 1, 2 }, difficulty: 42);

            LevelValidationResult result = CreateValidator().Validate(level);

            Assert.IsTrue(HasIssue(result, LevelIssueCode.DifficultyOutOfRange));
        }

        [Test]
        public void FixedStartOnAHole_IsReported()
        {
            var level = new LevelData(
                "badstart",
                1,
                new Grid(6, 5),
                LevelSamples.Level442Cells,
                fixedStart: 3,
                difficulty: 4);

            LevelValidationResult result = CreateValidator().Validate(level);

            Assert.IsTrue(HasIssue(result, LevelIssueCode.FixedStartNotActive));
        }

        [Test]
        public void UnsolvableButConnectedBoard_IsReportedAsNoSolution()
        {
            // A plus shape: whichever arm the path starts in, it strands the others.
            var level = new LevelData("plus", 1, new Grid(3, 3), new[] { 1, 3, 4, 5, 7 });

            LevelValidationResult result = CreateValidator().Validate(level);

            Assert.IsTrue(HasIssue(result, LevelIssueCode.NoSolutionFound));
            Assert.IsFalse(HasIssue(result, LevelIssueCode.SolverBudgetExceeded));
        }
    }
}
