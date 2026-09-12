namespace ASTeams.SingleLine.Data
{
    /// <summary>
    /// Derives the level id straight from the numbering rule, so a scene opened on its
    /// own still plays without anyone having filled a config in. The shipped build uses
    /// the config instead; this is the floor under it, not a second source of truth.
    /// </summary>
    public sealed class CampaignAddressCatalog : ILevelCatalog
    {
        public int LevelCount => CampaignLevelAddress.MaxLevelNumber;

        public bool TryGetLevelId(int levelNumber, out string levelId)
        {
            if (levelNumber < 1 || levelNumber > CampaignLevelAddress.MaxLevelNumber)
            {
                levelId = null;
                return false;
            }

            levelId = CampaignLevelAddress.ToLevelId(levelNumber);
            return true;
        }
    }
}
