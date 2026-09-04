using System;
using System.Collections.Generic;

namespace ASTeams.SingleLine.Data
{
    /// <summary>
    /// Chapter files held in memory. Used by tests and by the editor preview, which has
    /// the freshly assembled campaign in hand and no reason to write it to disk first.
    /// </summary>
    public sealed class InMemoryChapterSource : IChapterSource
    {
        private readonly Dictionary<string, string> chapters = new Dictionary<string, string>();

        public void Add(string chapterId, string json)
        {
            if (string.IsNullOrEmpty(chapterId))
            {
                throw new ArgumentException("Chapter id must not be empty.", nameof(chapterId));
            }

            chapters[chapterId] = json;
        }

        public bool TryReadChapter(string chapterId, out string json)
        {
            return chapters.TryGetValue(chapterId, out json);
        }
    }
}
