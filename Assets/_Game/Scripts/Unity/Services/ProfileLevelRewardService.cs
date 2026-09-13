using System;
using ASTeams.Base;
using ASTeams.Base.Data;
using ASTeams.SingleLine.Data;
using ASTeams.Template;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Pays the level reward into the player's profile, using the amount the team's
    /// <see cref="GameConfig"/> already defines so gameplay never invents an economy.
    ///
    /// <see cref="UserProfileController.AddCoin"/> writes through to storage on the spot,
    /// which is what makes the win a real checkpoint rather than a promise kept in memory.
    ///
    /// The template's own <see cref="GameResultHandleService"/> pays the same reward when
    /// it is enabled. Only one of the two may pay, so this tells the service the coins are
    /// already granted; the service exposes that call for exactly this case.
    /// </summary>
    public sealed class ProfileLevelRewardService : ILevelRewardService
    {
        private readonly UserProfileController profile;
        private readonly GameResultHandleService resultHandler;
        private readonly int firstClearBonus;

        public ProfileLevelRewardService(UserProfileController profile, GameResultHandleService resultHandler, int firstClearBonus)
        {
            this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
            this.resultHandler = resultHandler;
            this.firstClearBonus = firstClearBonus < 0 ? 0 : firstClearBonus;
        }

        // The profile exposes no coin property, only the data object it saves.
        public long Balance => profile.userData == null ? 0 : profile.userData.coin;

        public int AwardLevelReward(bool isFirstClear)
        {
            // GDD 8.1: every win pays, and the first time a level is finished pays a bonus
            // on top. Replaying a level already beaten pays the base only.
            int reward = RewardPerLevel + (isFirstClear ? firstClearBonus : 0);

            if (reward <= 0)
            {
                return 0;
            }

            profile.AddCoin(reward);

            if (resultHandler != null)
            {
                resultHandler.MarkWinCoinsGranted();
            }

            return reward;
        }

        private static int RewardPerLevel
        {
            get
            {
                ConfigController config = ConfigController.Instance;
                return config == null || config.GameConfig == null ? 0 : config.GameConfig.levelReward;
            }
        }
    }
}
