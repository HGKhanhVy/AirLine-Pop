using ASTeams.Base;
using ASTeams.Base.Data;
using ASTeams.Base.UI;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The boot screen: holds the logo while the services start, then opens the home
    /// screen.
    ///
    /// The template ships <c>LoadingController</c> for this scene, but it always opens
    /// Gameplay, and this game goes to Home first so the player picks their moment. Rather
    /// than edit the SDK, this stands in its place on the same scene and keeps the rest of
    /// its job: wait for the game to finish starting, and give a brand new save the values
    /// the template's own systems expect to find.
    /// </summary>
    public sealed class BootLoadingController : MonoBehaviour
    {
        [SerializeField] private Image progressBar;
        [SerializeField, Min(0.1f)] private float duration = 1f;

        [Tooltip("Where to go once the game has started. Home lets the player choose when " +
                 "to play; the board is one tap further on.")]
        [SerializeField] private string nextScene = SceneName.Home;

        private bool hasMoved;

        private void Start()
        {
            GameManager gameManager = GameManager.Instance;
            gameManager.OnInited.AddListener(HandleInited);

            if (progressBar != null)
            {
                progressBar.fillAmount = 0f;
                progressBar.DOFillAmount(1f, duration);
            }

            if (gameManager.IsInitialized)
            {
                HandleInited();
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnInited.RemoveListener(HandleInited);
            }
        }

        private async void HandleInited()
        {
            if (hasMoved)
            {
                return;
            }

            hasMoved = true;

            await UniTask.DelayFrame(1);
            SeedFirstRun();

            GameDataHelper.PlayType = "loading";
            UISceneController.Instance.ChangeScene(nextScene);
        }

        /// <summary>
        /// A save that has never been played needs the numbers the template's services read
        /// through the profile's parameter blob. The profile itself already fills in coins
        /// and level when it creates a user; these are the rest.
        /// </summary>
        private static void SeedFirstRun()
        {
            UserProfileController profile = UserProfileController.Instance;

            if (profile == null || profile.GetParam<bool>("firstTime"))
            {
                return;
            }

            GameConfig config = ConfigController.Instance == null ? null : ConfigController.Instance.GameConfig;

            profile.SetParam("firstTime", true);
            profile.SetParam("currentLevel", 1);
            profile.SetParam("adsTicket", 0);
            profile.SetParam("winStreak", 0);
            profile.SetParam("openedWinStreakCount", 0);

            if (config != null)
            {
                profile.SetParam("timeToAddLife", config.refillLifeTime);
                profile.SetParam("coin", config.startCoin);
                profile.SetParam("life", config.maxLife);
            }

            profile.SetParam("booster_1", 3);
            profile.SetParam("booster_2", 3);
            profile.SetParam("booster_3", 3);
            profile.SetParam("booster_1_tutorial", false);
            profile.SetParam("booster_2_tutorial", false);
            profile.SetParam("booster_3_tutorial", false);
        }
    }
}
