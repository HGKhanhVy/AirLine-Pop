using AirlinePop.SingleLine.Data;

namespace AirlinePop.SingleLine.Unity
{
    /// <summary>
    /// Reads the campaign order out of the <see cref="LevelOrderSO"/>. An empty slot or a
    /// level without an id is reported as missing rather than guessed at.
    /// </summary>
    public sealed class LevelConfigCatalog : ILevelCatalog
    {
        private readonly LevelOrderSO config;

        public LevelConfigCatalog(LevelOrderSO config)
        {
            this.config = config;
        }

        public int LevelCount => config == null ? 0 : config.LevelCount;

        public bool TryGetLevelId(int levelNumber, out string levelId)
        {
            levelId = null;
            SingleLineLevelSO definition = config == null ? null : config.GetLevel(levelNumber - 1);

            if (definition == null || string.IsNullOrEmpty(definition.LevelId))
            {
                return false;
            }

            levelId = definition.LevelId;
            return true;
        }
    }
}
