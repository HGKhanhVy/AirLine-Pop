using ASTeams.Base.Data;
using ASTeams.SingleLine.Core;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Pays what a new landing earns on top of its coins: a city's storm flight pays double.
    /// Replays earn nothing extra: only a first clear is raised, so this listens for that alone.
    /// </summary>
    public sealed class WinBonusPayer : MonoBehaviour
    {
        [SerializeField] private EconomyConfigSO economy;

        private bool isStormFlight;

        private void OnEnable()
        {
            GameplayEvents.OnLevelRules += HandleLevelRules;
            GameplayEvents.OnFirstClear += HandleFirstClear;
        }

        private void OnDisable()
        {
            GameplayEvents.OnLevelRules -= HandleLevelRules;
            GameplayEvents.OnFirstClear -= HandleFirstClear;
        }

        private void HandleLevelRules(LevelRule rules)
        {
            isStormFlight = (rules & LevelRule.Storm) != 0;
        }

        private void HandleFirstClear(int levelNumber)
        {
            UserProfileController profile = UserProfileController.Instance;

            if (!isStormFlight || economy == null || profile == null || economy.StormBonusCoins <= 0)
            {
                return;
            }

            int bonus = economy.StormBonusCoins;
            profile.AddCoin(bonus);
            FlightBonusEvents.RaiseBonusLine(Localization.Format("win.stormBonus", bonus));

            long balance = profile.userData == null ? 0 : profile.userData.coin;
            GameplayEvents.RaiseCoinsAwarded(bonus, balance);
            GameplayEvents.RaiseCoinBalanceChanged(balance);
        }

#if UNITY_EDITOR
        public void EditorLink(EconomyConfigSO linkedEconomy)
        {
            economy = linkedEconomy;
        }
#endif
    }
}
