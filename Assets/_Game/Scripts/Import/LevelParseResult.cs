using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// Outcome of parsing one level file pair. Carries the diagnostics the batch
    /// importer needs to report on a whole pack, not just the level itself.
    /// </summary>
    public readonly struct LevelParseResult
    {
        public LevelData Level { get; }

        public LevelParseError Error { get; }

        public PayloadForm Form { get; }

        /// <summary>
        /// True when the start recovered from the level file disagreed with the first
        /// cell of the tutorial file. 43 files in the ripped corpus do this; the
        /// tutorial wins, but the importer reports the count so a silent format change
        /// in a future data drop does not go unnoticed.
        /// </summary>
        public bool StartDisagreed { get; }

        /// <summary>True when a tutorial file supplied the solution.</summary>
        public bool HasTutorial { get; }

        public bool IsSuccess => Error == LevelParseError.None;

        private LevelParseResult(
            LevelData level,
            LevelParseError error,
            PayloadForm form,
            bool startDisagreed,
            bool hasTutorial)
        {
            Level = level;
            Error = error;
            Form = form;
            StartDisagreed = startDisagreed;
            HasTutorial = hasTutorial;
        }

        public static LevelParseResult Success(
            LevelData level,
            PayloadForm form,
            bool startDisagreed,
            bool hasTutorial)
        {
            return new LevelParseResult(level, LevelParseError.None, form, startDisagreed, hasTutorial);
        }

        public static LevelParseResult Failure(LevelParseError error)
        {
            return new LevelParseResult(null, error, PayloadForm.Path, false, false);
        }

        public override string ToString()
        {
            return IsSuccess ? Level.ToString() + " (" + Form + ")" : Error.ToString();
        }
    }
}
