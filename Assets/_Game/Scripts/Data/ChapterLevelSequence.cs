using System;
using System.Collections.Generic;

namespace ASTeams.SingleLine.Data
{
    public sealed class ChapterLevelSequence : ILevelSequence
    {
        private readonly ILevelRepository repository;

        public ChapterLevelSequence(ILevelRepository repository)
        {
            this.repository = repository;
        }

        public string GetNext(string levelId)
        {
            string chapterId = ChapterLevelRepository.GetChapterId(levelId);
            if (chapterId == null)
            {
                return null;
            }

            IReadOnlyList<string> ids = repository.GetLevelIds(chapterId);
            for (int i = 0; i < ids.Count; i++)
            {
                if (ids[i] != levelId)
                {
                    continue;
                }

                if (i + 1 < ids.Count)
                {
                    return ids[i + 1];
                }

                return FirstInNextChapter(chapterId);
            }

            return null;
        }

        private string FirstInNextChapter(string chapterId)
        {
            if (!chapterId.StartsWith("ch", StringComparison.Ordinal) ||
                !int.TryParse(chapterId.Substring(2), out int number))
            {
                return null;
            }

            IReadOnlyList<string> ids = repository.GetLevelIds("ch" + (number + 1).ToString("00"));
            return ids == null || ids.Count == 0 ? null : ids[0];
        }
    }
}
