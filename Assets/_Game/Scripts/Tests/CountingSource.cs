using ASTeams.SingleLine.Data;

namespace ASTeams.SingleLine.Core.Tests
{
    internal sealed class CountingSource : IChapterSource
    {
        private readonly InMemoryChapterSource inner = new InMemoryChapterSource();

        public int ReadCount { get; private set; }

        public void Add(string chapterId, string json)
        {
            inner.Add(chapterId, json);
        }

        public bool TryReadChapter(string chapterId, out string json)
        {
            ReadCount++;
            return inner.TryReadChapter(chapterId, out json);
        }
    }
}
