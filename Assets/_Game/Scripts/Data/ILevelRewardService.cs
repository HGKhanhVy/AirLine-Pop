namespace ASTeams.SingleLine.Data
{
    /// <summary>
    /// Pays out what finishing a level is worth.
    ///
    /// Gameplay decides only the moment; how much a level pays, which currency it lands
    /// in and where that is stored all belong to the implementation. Keeping it behind an
    /// interface is what lets the board be tested without a save file.
    /// </summary>
    public interface ILevelRewardService
    {
        /// <summary>
        /// Grants the reward for one completed level and returns how much was granted.
        /// Zero means nothing was paid, which is normal when no economy is configured.
        /// </summary>
        int AwardLevelReward();

        /// <summary>Balance after the last award, for a screen that wants to show it.</summary>
        long Balance { get; }
    }
}
