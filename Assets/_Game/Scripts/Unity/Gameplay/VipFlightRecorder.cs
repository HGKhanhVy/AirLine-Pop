using ASTeams.Base.Data;
using ASTeams.SingleLine.Core;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>Notes each VIP flight the player lands, for the gold seal on its stamp and the VIP guest's visit.</summary>
    public sealed class VipFlightRecorder : MonoBehaviour
    {
        private IVipFlightStore store;
        private IDayClock clock;
        private bool isVip;

        private void OnEnable()
        {
            store ??= new ProfileVipFlightStore(UserProfileController.Instance);
            clock ??= new LocalDayClock();
            GameplayEvents.OnLevelRules += HandleLevelRules;
            GameplayEvents.OnLevelWon += HandleLevelWon;
        }

        private void OnDisable()
        {
            GameplayEvents.OnLevelRules -= HandleLevelRules;
            GameplayEvents.OnLevelWon -= HandleLevelWon;
        }

        private void HandleLevelRules(LevelRule rules)
        {
            isVip = (rules & LevelRule.Vip) != 0;
        }

        private void HandleLevelWon(int levelNumber, string levelId)
        {
            if (isVip)
            {
                store.MarkFlown(levelNumber, clock.Today);
            }
        }
    }
}
