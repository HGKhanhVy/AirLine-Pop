using System;
using System.Collections.Generic;
using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// Turns the reference game's <c>Level_N.txt</c> and <c>Toturial_N.txt</c> pair into
    /// a <see cref="LevelData"/>. Takes text rather than file paths so it stays engine
    /// free and testable; reading files is the editor tool's job.
    ///
    /// The instance reuses its token buffers across calls, so importing a thousand
    /// files does not allocate a thousand lists.
    /// </summary>
    public sealed class LevelTextParser
    {
        private static readonly char[] Separators = { ',', ' ', '\t', '\r', '\n' };

        private readonly List<int> levelValues = new List<int>(128);
        private readonly List<int> tutorialValues = new List<int>(128);

        /// <summary>
        /// Parses one level. <paramref name="tutorialText"/> may be null when the pack
        /// ships no solution; the solver fills that gap later.
        /// </summary>
        public LevelParseResult Parse(string levelId, string levelText, string tutorialText)
        {
            if (string.IsNullOrEmpty(levelId))
            {
                throw new ArgumentException("Level id must not be empty.", nameof(levelId));
            }

            if (!TryReadNumbers(levelText, levelValues, out LevelParseError error))
            {
                return LevelParseResult.Failure(error);
            }

            if (levelValues.Count < 3)
            {
                return LevelParseResult.Failure(LevelParseError.TooFewValues);
            }

            int width = levelValues[0];
            int height = levelValues[1];

            if (width <= 0 || height <= 0)
            {
                return LevelParseResult.Failure(LevelParseError.InvalidGridSize);
            }

            int payloadStart = 2;
            int payloadCount = levelValues.Count - payloadStart;

            if (payloadCount <= 0)
            {
                return LevelParseResult.Failure(LevelParseError.NoActiveCells);
            }

            PayloadForm form = ClassifyPayload(levelValues, payloadStart, payloadCount);
            int[] activeCells = ExtractActiveCells(levelValues, payloadStart, payloadCount, form);
            int start = ExtractStart(levelValues, payloadStart, payloadCount, form);

            int[] solution = null;
            int end = LevelData.NoCell;
            bool hasTutorial = false;
            bool startDisagreed = false;

            if (!string.IsNullOrEmpty(tutorialText))
            {
                if (!TryReadNumbers(tutorialText, tutorialValues, out error))
                {
                    return LevelParseResult.Failure(error);
                }

                if (tutorialValues.Count > 0)
                {
                    solution = tutorialValues.ToArray();
                    hasTutorial = true;

                    // The tutorial is the trustworthy source: several level files store a
                    // different but equally valid walk over the same cells.
                    startDisagreed = start != LevelData.NoCell && start != solution[0];
                    start = solution[0];

                    // Where the author's own walk finishes is the level's goal square. Only
                    // a shipped tutorial says that; a solution the solver had to invent ends
                    // wherever its search happened to land, and pinning the goal there would
                    // impose a rule nobody designed.
                    end = solution[solution.Length - 1];
                }
            }

            LevelData level;

            try
            {
                level = new LevelData(levelId, 1, new Grid(width, height), activeCells, start, end, solution);
            }
            catch (ArgumentOutOfRangeException)
            {
                // A cell outside the declared grid always means a corrupt header.
                return LevelParseResult.Failure(LevelParseError.CellOutsideGrid);
            }

            if (level.ActiveCellCount == 0)
            {
                return LevelParseResult.Failure(LevelParseError.NoActiveCells);
            }

            return LevelParseResult.Success(level, form, startDisagreed, hasTutorial);
        }

        /// <summary>
        /// Order matters. A payload that ascends all the way through stores no start at
        /// all, so testing the full range has to come first; only then is it safe to ask
        /// whether the payload ascends apart from a trailing repeat of one of its own
        /// entries. Checking monotonicity over the whole payload for the SetWithStart
        /// case would misread every one of the 662 files that use it, because the
        /// trailing start is almost always smaller than the last cell.
        /// </summary>
        public static PayloadForm ClassifyPayload(IReadOnlyList<int> values, int offset, int count)
        {
            if (IsAscending(values, offset, count))
            {
                return PayloadForm.SetOnly;
            }

            if (count >= 2 && IsAscending(values, offset, count - 1))
            {
                int last = values[offset + count - 1];

                for (int i = 0; i < count - 1; i++)
                {
                    if (values[offset + i] == last)
                    {
                        return PayloadForm.SetWithStart;
                    }
                }
            }

            return PayloadForm.Path;
        }

        private static bool IsAscending(IReadOnlyList<int> values, int offset, int count)
        {
            for (int i = 1; i < count; i++)
            {
                if (values[offset + i] <= values[offset + i - 1])
                {
                    return false;
                }
            }

            return true;
        }

        private static int[] ExtractActiveCells(List<int> values, int offset, int count, PayloadForm form)
        {
            int cellCount = form == PayloadForm.SetWithStart ? count - 1 : count;
            var cells = new int[cellCount];

            for (int i = 0; i < cellCount; i++)
            {
                cells[i] = values[offset + i];
            }

            return cells;
        }

        private static int ExtractStart(List<int> values, int offset, int count, PayloadForm form)
        {
            switch (form)
            {
                case PayloadForm.SetWithStart:
                    return values[offset + count - 1];

                case PayloadForm.Path:
                    return values[offset];

                default:
                    return LevelData.NoCell;
            }
        }

        private static bool TryReadNumbers(string text, List<int> destination, out LevelParseError error)
        {
            destination.Clear();

            if (string.IsNullOrEmpty(text))
            {
                error = LevelParseError.EmptyFile;
                return false;
            }

            string[] tokens = text.Split(Separators, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < tokens.Length; i++)
            {
                if (!int.TryParse(tokens[i], out int value))
                {
                    error = LevelParseError.NonNumericToken;
                    return false;
                }

                destination.Add(value);
            }

            if (destination.Count == 0)
            {
                error = LevelParseError.EmptyFile;
                return false;
            }

            error = LevelParseError.None;
            return true;
        }
    }
}
