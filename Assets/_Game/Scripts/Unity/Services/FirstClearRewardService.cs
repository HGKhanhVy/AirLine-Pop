using System;
using ASTeams.Base.Data;
using ASTeams.SingleLine.Data;
using ASTeams.Template;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// GDD 6: a level pays its coins the first time it is finished and nothing on a replay.
    /// The amount comes from <see cref="EconomyConfigSO"/>, so balancing never touches code.
    ///
    /// Written straight into the profile, which saves on the spot: the coins are a
    /// checkpoint the moment the level is won, not a promise kept for the win screen.
    /// </summary>
    public sealed class FirstClearRewardService : ILevelRewardService
    {
        private readonly UserProfileController profile;
        private readonly GameResultHandleService resultHandler;
        private readonly EconomyConfigSO economy;

        public FirstClearRewardService(UserProfileController profile, GameResultHandleService resultHandler, EconomyConfigSO economy)
        {
            this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
            this.economy = economy ?? throw new ArgumentNullException(nameof(economy));
            this.resultHandler = resultHandler;
        }

        public long Balance => profile.userData == null ? 0 : profile.userData.coin;

        public int AwardLevelReward(bool isFirstClear)
        {
            int reward = isFirstClear ? economy.FirstClearCoins : 0;

            if (reward <= 0)
            {
                return 0;
            }

            profile.AddCoin(reward);

            // The template's own win flow would pay again unless told this one already did.
            if (resultHandler != null)
            {
                resultHandler.MarkWinCoinsGranted();
            }

            return reward;
        }
    }
}
