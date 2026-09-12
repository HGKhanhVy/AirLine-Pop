using ASTeams.Base.Level;
using ASTeams.SingleLine.Data;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Reads the campaign order out of the team's <see cref="LevelConfigSO"/>, so the
    /// list a designer edits in the Inspector is the list the game plays.
    ///
    /// Entries that hold something other than a Single Line level are skipped rather
    /// than guessed at: the config is shared with the rest of the template, and a slot
    /// filled in by another feature must not silently become a board here.
    /// </summary>
    public sealed class LevelConfigCatalog : ILevelCatalog
    {
        private readonly LevelConfigSO config;

        public LevelConfigCatalog(LevelConfigSO config)
        {
            this.config = config;
        }

        public int LevelCount => config == null ? 0 : config.LevelCount;

        public bool TryGetLevelId(int levelNumber, out string levelId)
        {
            levelId = null;

            if (config == null || levelNumber < 1 || levelNumber > config.LevelCount)
            {
                return false;
            }

            LevelConfig entry = config.GetLevelByIndex(levelNumber - 1);

            if (entry == null || !(entry.levelDefinition is SingleLineLevelSO definition) ||
                string.IsNullOrEmpty(definition.LevelId))
            {
                return false;
            }

            levelId = definition.LevelId;
            return true;
        }
    }
}
