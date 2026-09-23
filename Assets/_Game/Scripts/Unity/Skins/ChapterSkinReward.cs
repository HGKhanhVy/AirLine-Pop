using ASTeams.Base.Data;
using ASTeams.Base.UI;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Hands the player their first skin for finishing the first chapter (GDD 2): the
    /// moment the game shows that a board can look different at all.
    /// </summary>
    public sealed class ChapterSkinReward : MonoBehaviour
    {
        [SerializeField] private SkinCatalogSO catalog;

        private ISkinService skins;

        private void Awake()
        {
            skins = new ProfileSkinService(catalog, UserProfileController.Instance);
        }

        private void OnEnable()
        {
            GameplayEvents.OnLevelWon += HandleLevelWon;
        }

        private void OnDisable()
        {
            GameplayEvents.OnLevelWon -= HandleLevelWon;
        }

        private void HandleLevelWon(int levelNumber, string levelId)
        {
            SkinSO reward = catalog == null ? null : catalog.ChapterOneReward;

            if (reward == null || levelNumber != catalog.ChapterOneLastLevel || skins.IsOwned(reward))
            {
                return;
            }

            skins.Grant(reward);
            UIMessageController.Instance?.ShowNoti("New skin unlocked: " + reward.DisplayName, "top");
        }
    }
}
