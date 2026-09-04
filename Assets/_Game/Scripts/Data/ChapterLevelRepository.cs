using System;
using System.Collections.Generic;
using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Data
{
    /// <summary>
    /// Loads levels a chapter at a time and keeps what it has read.
    ///
    /// A chapter is a few kilobytes, so reading one is cheap and reading all ten up front
    /// would be waste; caching after the first read means a player moving through a
    /// chapter pays nothing between levels, which is what the 300 ms board load budget in
    /// GDD 15.4 is really about.
    /// </summary>
    public sealed class ChapterLevelRepository : ILevelRepository
    {
        private static readonly string[] NoLevels = new string[0];

        private readonly IChapterSource source;
        private readonly Dictionary<string, LevelData> levelsById = new Dictionary<string, LevelData>();
        private readonly Dictionary<string, List<string>> idsByChapter = new Dictionary<string, List<string>>();
        private readonly HashSet<string> missingChapters = new HashSet<string>();

        public ChapterLevelRepository(IChapterSource source)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public LevelData Get(string levelId)
        {
            if (!TryGet(levelId, out LevelData level))
            {
                throw new KeyNotFoundException("No level with id " + levelId + ".");
            }

            return level;
        }

        public bool TryGet(string levelId, out LevelData level)
        {
            level = null;

            if (string.IsNullOrEmpty(levelId))
            {
                return false;
            }

            if (levelsById.TryGetValue(levelId, out level))
            {
                return true;
            }

            string chapterId = GetChapterId(levelId);

            if (chapterId == null || !TryPreloadChapter(chapterId))
            {
                return false;
            }

            return levelsById.TryGetValue(levelId, out level);
        }

        public IReadOnlyList<string> GetLevelIds(string chapterId)
        {
            if (string.IsNullOrEmpty(chapterId))
            {
                return NoLevels;
            }

            if (idsByChapter.TryGetValue(chapterId, out List<string> ids))
            {
                return ids;
            }

            return TryPreloadChapter(chapterId) ? idsByChapter[chapterId] : NoLevels;
        }

        public bool TryPreloadChapter(string chapterId)
        {
            if (string.IsNullOrEmpty(chapterId))
            {
                return false;
            }

            if (idsByChapter.ContainsKey(chapterId))
            {
                return true;
            }

            // A chapter that was already looked for and not found is remembered, so
            // walking off the end of the campaign does not hit the file system on every
            // frame that asks what comes next.
            if (missingChapters.Contains(chapterId))
            {
                return false;
            }

            if (!source.TryReadChapter(chapterId, out string json))
            {
                missingChapters.Add(chapterId);
                return false;
            }

            List<LevelData> levels = LevelJsonSerializer.DeserializeChapter(json);
            var ids = new List<string>(levels.Count);

            for (int i = 0; i < levels.Count; i++)
            {
                LevelData level = levels[i];
                levelsById[level.Id] = level;
                ids.Add(level.Id);
            }

            idsByChapter.Add(chapterId, ids);
            return true;
        }

        /// <summary>
        /// Level ids are chapter id, an underscore, then the slot, as in ch01_007, so the
        /// chapter a level belongs to is derivable and no index file is needed.
        /// </summary>
        public static string GetChapterId(string levelId)
        {
            if (string.IsNullOrEmpty(levelId))
            {
                return null;
            }

            int separator = levelId.LastIndexOf('_');
            return separator <= 0 ? null : levelId.Substring(0, separator);
        }
    }
}
