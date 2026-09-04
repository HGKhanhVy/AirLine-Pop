namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// One level as it sits on disk, already read into memory. The pipeline takes text
    /// rather than paths so it stays engine free and can be driven from a unit test, an
    /// editor window or a command line tool without any of them changing.
    /// </summary>
    public readonly struct RawLevelFile
    {
        /// <summary>Stable source identifier, conventionally pack and file name.</summary>
        public string Id { get; }

        public string LevelText { get; }

        /// <summary>Contents of the matching tutorial file, or null when the pack has none.</summary>
        public string TutorialText { get; }

        public RawLevelFile(string id, string levelText, string tutorialText)
        {
            Id = id;
            LevelText = levelText;
            TutorialText = tutorialText;
        }
    }
}
