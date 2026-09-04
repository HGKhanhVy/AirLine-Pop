using ASTeams.SingleLine.Core;
using ASTeams.SingleLine.Import;
using NUnit.Framework;

namespace ASTeams.SingleLine.Core.Tests
{
    /// <summary>
    /// Every raw string below is copied verbatim from the ripped corpus unless the test
    /// name says otherwise, so a regression here means the importer would really break
    /// on shipped data.
    /// </summary>
    public sealed class LevelTextParserTests
    {
        // medium/Level_442.txt, shown in the reference game as level 441.
        private const string Level442 =
            "6,5,0,1,2,4,6,7,8,9,10,12,14,15,16,17,18,20,21,22,23,24,25,26,27,28,29,7";

        private const string Tutorial442 =
            "7,8,2,1,0,6,12,18,24,25,26,27,28,29,23,17,16,22,21,20,14,15,9,10,4";

        // beginner/Level_75.txt, one of only three files that store no start at all.
        private const string Level75 =
            "6,6,0,1,4,5,6,7,10,11,12,13,14,15,16,17,18,19,20,23,24,27,28,29,30,31,32,33,34,35";

        private const string Tutorial75 =
            "35,29,23,17,11,5,4,10,16,15,14,20,19,13,7,1,0,6,12,18,24,30,31,32,33,27,28,34";

        private static LevelTextParser CreateParser()
        {
            return new LevelTextParser();
        }

        [Test]
        public void SetWithStart_IsTheCommonForm()
        {
            LevelParseResult result = CreateParser().Parse("m442", Level442, null);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(PayloadForm.SetWithStart, result.Form);
            Assert.AreEqual(25, result.Level.ActiveCellCount);
            Assert.AreEqual(7, result.Level.FixedStart);
            Assert.AreEqual(new Grid(6, 5), result.Level.Grid);
        }

        [Test]
        public void SetWithStart_IsNotMistakenForAPath()
        {
            // The trailing start (7) is smaller than the last cell (29), so a classifier
            // that tests monotonicity across the whole payload would fall through to
            // Path and take cell 0 as the start.
            LevelParseResult result = CreateParser().Parse("m442", Level442, null);

            Assert.AreNotEqual(PayloadForm.Path, result.Form);
            Assert.AreNotEqual(0, result.Level.FixedStart);
        }

        [Test]
        public void SetWithStart_DropsTheTrailingStartFromTheCellList()
        {
            // medium/Level_2.txt: five cells on a 3x2 board, cell 0 is a hole.
            LevelParseResult result = CreateParser().Parse("m2", "3,2,1,2,3,4,5,3", null);

            Assert.AreEqual(PayloadForm.SetWithStart, result.Form);
            Assert.AreEqual(5, result.Level.DeclaredActiveCells.Count);
            Assert.AreEqual(3, result.Level.FixedStart);
            Assert.IsFalse(result.Level.IsActive(0));
        }

        [Test]
        public void SetOnly_LeavesTheStartUnsetWithoutATutorial()
        {
            LevelParseResult result = CreateParser().Parse("b75", Level75, null);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(PayloadForm.SetOnly, result.Form);
            Assert.AreEqual(28, result.Level.ActiveCellCount);
            Assert.IsFalse(result.Level.HasFixedStart);
        }

        [Test]
        public void SetOnly_TakesItsStartFromTheTutorial()
        {
            LevelParseResult result = CreateParser().Parse("b75", Level75, Tutorial75);

            Assert.AreEqual(35, result.Level.FixedStart);
            Assert.IsTrue(result.HasTutorial);
            Assert.IsFalse(result.StartDisagreed, "there was no stored start to disagree with");
        }

        [Test]
        public void Path_TakesTheFirstEntryAsTheStart()
        {
            LevelParseResult result = CreateParser().Parse("synthetic", "3,2,3,4,5,2,1,0", null);

            Assert.AreEqual(PayloadForm.Path, result.Form);
            Assert.AreEqual(3, result.Level.FixedStart);
            Assert.AreEqual(6, result.Level.ActiveCellCount);
        }

        [Test]
        public void Tutorial_SuppliesTheSolutionAndWinsOnTheStart()
        {
            LevelParseResult result = CreateParser().Parse("m442", Level442, Tutorial442);

            Assert.IsTrue(result.HasTutorial);
            Assert.AreEqual(25, result.Level.Solution.Count);
            Assert.AreEqual(7, result.Level.Solution[0]);
            Assert.AreEqual(4, result.Level.Solution[24]);
            Assert.IsFalse(result.StartDisagreed, "this file happens to agree with its tutorial");
        }

        [Test]
        public void StartDisagreed_IsReportedWhenTheTwoFilesConflict()
        {
            // Same board, but a tutorial that walks it starting from cell 4 instead of 7.
            const string conflicting = "4,8,2,1,0,6,12,18,24,25,26,27,28,29,23,17,16,22,21,20,14,15,9,10,7";

            LevelParseResult result = CreateParser().Parse("m442", Level442, conflicting);

            Assert.IsTrue(result.StartDisagreed);
            Assert.AreEqual(4, result.Level.FixedStart, "the tutorial wins");
        }

        [Test]
        public void ParsedLevel442_SatisfiesTheRules()
        {
            LevelParseResult result = CreateParser().Parse("m442", Level442, Tutorial442);
            var validator = new LevelValidator(new WarnsdorffSolver());

            LevelValidationResult validation = validator.Validate(result.Level);

            Assert.IsTrue(validation.IsValid, "unexpected issues: " + string.Join(", ", validation.Issues));
        }

        [Test]
        public void CellOutsideTheDeclaredGrid_IsReported()
        {
            LevelParseResult result = CreateParser().Parse("bad", "3,3,0,1,9", null);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(LevelParseError.CellOutsideGrid, result.Error);
        }

        [Test]
        public void NonNumericToken_IsReported()
        {
            LevelParseResult result = CreateParser().Parse("bad", "6,5,x,1,2", null);

            Assert.AreEqual(LevelParseError.NonNumericToken, result.Error);
        }

        [Test]
        public void EmptyFile_IsReported()
        {
            Assert.AreEqual(LevelParseError.EmptyFile, CreateParser().Parse("bad", "   ", null).Error);
            Assert.AreEqual(LevelParseError.EmptyFile, CreateParser().Parse("bad", null, null).Error);
        }

        [Test]
        public void TooFewValues_IsReported()
        {
            Assert.AreEqual(LevelParseError.TooFewValues, CreateParser().Parse("bad", "6,5", null).Error);
        }

        [Test]
        public void InvalidGridSize_IsReported()
        {
            Assert.AreEqual(LevelParseError.InvalidGridSize, CreateParser().Parse("bad", "0,5,1", null).Error);
            Assert.AreEqual(LevelParseError.InvalidGridSize, CreateParser().Parse("bad", "6,-2,1", null).Error);
        }

        [Test]
        public void SeparatorsAndTrailingWhitespace_AreTolerated()
        {
            LevelParseResult result = CreateParser().Parse("m2", " 3, 2,\r\n1 2\t3,4,5,3\n", null);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(5, result.Level.ActiveCellCount);
            Assert.AreEqual(3, result.Level.FixedStart);
        }

        [Test]
        public void ReusingOneParser_KeepsResultsIndependent()
        {
            LevelTextParser parser = CreateParser();

            LevelParseResult first = parser.Parse("m442", Level442, Tutorial442);
            LevelParseResult second = parser.Parse("m2", "3,2,1,2,3,4,5,3", null);
            LevelParseResult third = parser.Parse("m442-again", Level442, Tutorial442);

            Assert.AreEqual(25, first.Level.ActiveCellCount);
            Assert.AreEqual(5, second.Level.ActiveCellCount);
            Assert.AreEqual(0, second.Level.Solution.Count, "the previous tutorial must not leak in");
            Assert.AreEqual(25, third.Level.ActiveCellCount);
            Assert.AreEqual(25, third.Level.Solution.Count);
        }
    }
}
