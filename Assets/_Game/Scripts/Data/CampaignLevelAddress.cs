using System;

namespace ASTeams.SingleLine.Data
{
    public static class CampaignLevelAddress
    {
        public const int LevelsPerChapter = 30;
        // The route map's 41 cities of ten flights each.
        public const int MaxLevelNumber = 410;

        public static string ToLevelId(int levelNumber)
        {
            int clamped = Math.Max(1, Math.Min(MaxLevelNumber, levelNumber));
            int zeroBased = clamped - 1;
            int chapter = zeroBased / LevelsPerChapter + 1;
            int slot = zeroBased % LevelsPerChapter + 1;
            return "ch" + chapter.ToString("00") + "_" + slot.ToString("000");
        }

        public static bool TryGetLevelNumber(string levelId, out int levelNumber)
        {
            levelNumber = 0;

            if (string.IsNullOrEmpty(levelId) || levelId.Length != 8 ||
                levelId[0] != 'c' || levelId[1] != 'h' || levelId[4] != '_')
            {
                return false;
            }

            if (!int.TryParse(levelId.Substring(2, 2), out int chapter) ||
                !int.TryParse(levelId.Substring(5, 3), out int slot) ||
                chapter < 1 || slot < 1 || slot > LevelsPerChapter)
            {
                return false;
            }

            levelNumber = (chapter - 1) * LevelsPerChapter + slot;
            return levelNumber <= MaxLevelNumber;
        }
    }
}
