namespace ASTeams.SingleLine.Data
{
    /// <summary>
    /// Where chapter files come from. The repository depends on this rather than on
    /// UnityEngine.Resources, so the same repository serves the shipping build, the
    /// editor preview and a plain unit test without any of them knowing about the
    /// others.
    ///
    /// It is also what keeps the ban on hard coded Resources paths honest: exactly one
    /// implementation knows how a chapter id becomes a location.
    /// </summary>
    public interface IChapterSource
    {
        /// <summary>
        /// Reads the chapter file for <paramref name="chapterId"/>. Returns false when
        /// the chapter does not exist, which is a normal answer rather than an error:
        /// the game asks for chapters beyond the last one to decide when to stop.
        /// </summary>
        bool TryReadChapter(string chapterId, out string json);
    }
}
