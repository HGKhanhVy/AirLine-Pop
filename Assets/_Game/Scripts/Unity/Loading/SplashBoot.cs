using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AirlinePop.SingleLine.Unity
{
    /// <summary>
    /// The boot screen (GDD 9.1): loads Home behind the cat loading screen, gives a new
    /// save its starting coins, and asks the privacy question once before the game opens.
    /// </summary>
    public sealed class SplashBoot : MonoBehaviour
    {
        [SerializeField] private string nextSceneName = GameScenes.Home;

        [Tooltip("Asked before the first launch continues (GDD 12.3); Settings can reopen it.")]
        [SerializeField] private ConsentPopup consentPopup;

        [SerializeField] private EconomyConfigSO economy;

        [Tooltip("Least time the splash stays up. Home loads in a fraction of a second, so " +
                 "without this the splash flashes past and the player never sees it.")]
        [SerializeField, Min(0f)] private float minimumSeconds = 1.5f;

        private AsyncOperation loading;

        private void Start()
        {
            StartCoroutine(LoadRoutine());
        }

        private IEnumerator LoadRoutine()
        {
            float startedAt = Time.realtimeSinceStartup;
            loading = SceneManager.LoadSceneAsync(nextSceneName);
            loading.allowSceneActivation = false;

            while (loading.progress < 0.9f || Time.realtimeSinceStartup - startedAt < minimumSeconds)
            {
                yield return null;
            }

            FirstRunProfile.Seed(economy == null ? 0 : economy.StartingCoins);

            if (FirstRunProfile.HasConsent() || consentPopup == null)
            {
                OpenGame();
                yield break;
            }

            consentPopup.Ask(OpenGame);
        }

        private void OpenGame()
        {
            loading.allowSceneActivation = true;
        }

#if UNITY_EDITOR
        public void EditorLink(ConsentPopup linkedConsent, EconomyConfigSO linkedEconomy)
        {
            consentPopup = linkedConsent;
            economy = linkedEconomy;
        }
#endif
    }
}
