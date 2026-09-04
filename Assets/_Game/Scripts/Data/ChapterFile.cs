using Newtonsoft.Json;

namespace ASTeams.SingleLine.Data
{
    /// <summary>
    /// One exported chapter file. A chapter is the unit of loading: thirty levels is a
    /// few kilobytes, small enough to read in one go and large enough that the player
    /// never waits again inside a chapter.
    /// </summary>
    public sealed class ChapterFile
    {
        /// <summary>
        /// Bumped whenever the file layout changes so a build can refuse data it does not
        /// understand instead of half reading it.
        /// </summary>
        public const int CurrentVersion = 1;

        [JsonProperty("formatVersion")]
        public int FormatVersion { get; set; }

        [JsonProperty("chapterId")]
        public string ChapterId { get; set; }

        [JsonProperty("levels")]
        public LevelDto[] Levels { get; set; }
    }
}
