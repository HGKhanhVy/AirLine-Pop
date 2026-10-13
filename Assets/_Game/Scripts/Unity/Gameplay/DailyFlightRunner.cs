using System.Collections.Generic;
using ASTeams.Base.Data;
using ASTeams.Base.UI;
using ASTeams.SingleLine.Core;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Runs the daily flight when Home asked for it: picks today's board, pays the day's coins
    /// on landing, and takes the player back to the airport afterwards instead of on to the
    /// next campaign flight. The campaign's own position is never touched.
    /// </summary>
    public sealed class DailyFlightRunner : MonoBehaviour
    {
        [SerializeField] private EconomyConfigSO economy;

        private DailyFlightService service;
        private ISceneNavigator navigator;

        /// <summary>True while today's daily flight is the one on the board.</summary>
        public bool IsActive { get; private set; }

        /// <summary>Takes a waiting request and hands back today's board; false when none was asked for or none ships.</summary>
        public bool TryBegin(out LevelData level)
        {
            level = null;

            if (!DailyFlightRequest.TryTake() || economy == null)
            {
                return false;
            }

            List<LevelData> flights = DailyFlightCatalog.Load();

            if (flights.Count == 0)
            {
                Debug.LogWarning("No daily flights shipped; run Tools/Single Line/Rebuild Campaign.", this);
                return false;
            }

            service = new DailyFlightService(new ProfileDailyFlightStore(UserProfileController.Instance), new LocalDayClock(),
                economy.DailyFlightCoins, economy.DailyCoinsPerDayInRow, economy.DailyMaxDaysCounted);
            navigator = new SdkSceneNavigator(UISceneController.Instance);
            level = flights[service.FlightIndexFor(flights.Count)];
            IsActive = true;
            return true;
        }

        /// <summary>Records today's landing and pays for it, before the win itself is announced.</summary>
        public void Complete()
        {
            int coins = service.Complete(out int dayStreak);
            UserProfileController profile = UserProfileController.Instance;

            if (coins > 0 && profile != null)
            {
                profile.AddCoin(coins);
                long balance = profile.userData == null ? 0 : profile.userData.coin;
                GameplayEvents.RaiseCoinsAwarded(coins, balance);
                GameplayEvents.RaiseCoinBalanceChanged(balance);
            }

            FlightBonusEvents.RaiseDailyCompleted(dayStreak);
        }

        public void ReturnHome()
        {
            navigator?.GoHome();
        }

#if UNITY_EDITOR
        public void EditorLink(EconomyConfigSO linkedEconomy)
        {
            economy = linkedEconomy;
        }
#endif
    }
}
