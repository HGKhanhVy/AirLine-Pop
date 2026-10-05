namespace ASTeams.SingleLine.Core
{
    /// <summary>The tags a level can carry and how to read them.</summary>
    public static class LevelTags
    {
        /// <summary>A hand-drawn picture board; the tag after it names the picture.</summary>
        public const string Special = "special";

        /// <summary>A night flight, see <see cref="LevelRule.Night"/>.</summary>
        public const string Night = "night";

        /// <summary>A formation flight, see <see cref="LevelRule.Formation"/>.</summary>
        public const string Formation = "formation";

        /// <summary>The VIP flight that closes a chapter, mixing the conditions learnt so far.</summary>
        public const string Vip = "vip";

        public static bool Has(LevelData level, string tag)
        {
            if (level == null)
            {
                return false;
            }

            for (int i = 0; i < level.Tags.Count; i++)
            {
                if (level.Tags[i] == tag)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The picture a special board shows, such as "plane", or null for an ordinary board.</summary>
        public static string SpecialName(LevelData level)
        {
            if (level == null)
            {
                return null;
            }

            for (int i = 0; i + 1 < level.Tags.Count; i++)
            {
                if (level.Tags[i] == Special)
                {
                    return level.Tags[i + 1];
                }
            }

            return null;
        }
    }
}
