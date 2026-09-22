using ASTeams.SingleLine.Unity;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class LoadingManager : MonoBehaviour
{
    [Header("Scene")]
    public string nextSceneName = "Home";

    [Header("Loading Icon")]
    public Image loadingIcon;
    public Sprite[] loadingFrames;

    [Header("Progress UI")]
    public Slider progressBar;
    public TMP_Text progressText;

    [Header("Consent")]
    [Tooltip("Asked before the first launch continues (GDD 12.3); the same popup is reopened from Settings.")]
    [SerializeField] private UiConsent consentPopup;

    [Header("Timing")]
    [Tooltip("Least time the logo stays on screen. Home loads in a fraction of a second, " +
             "so without this the splash flashes past and the player never sees it.")]
    [Min(0f)]
    public float minimumSeconds = 1.5f;

    private AsyncOperation loadingOperation;
    private float startedAt;

   
    private void Start()
    {
        // The consent answer is kept: GDD 12.3 asks once and remembers. Clearing the key
        // here made the panel appear on every launch, which is a debug aid, not the flow.
        StartCoroutine(LoadScene());
    }


    private IEnumerator LoadScene()
    {
        startedAt = Time.realtimeSinceStartup;

        loadingOperation =
            SceneManager.LoadSceneAsync(nextSceneName);

        loadingOperation.allowSceneActivation = false;

        while (!loadingOperation.isDone)
        {
            float loaded = Mathf.Clamp01(
                loadingOperation.progress / 0.9f
            );

            // The bar follows whichever is further behind, the real load or the minimum
            // time, so it always reads as filling rather than sitting full and waiting.
            float shown = minimumSeconds <= 0f
                ? 1f
                : Mathf.Clamp01((Time.realtimeSinceStartup - startedAt) / minimumSeconds);
            float progress = Mathf.Min(loaded, shown);

            // Loading icon
            if (loadingFrames != null &&
                loadingFrames.Length > 0)
            {
                int frameIndex = Mathf.FloorToInt(
                    progress * (loadingFrames.Length - 1)
                );

                loadingIcon.sprite =
                    loadingFrames[frameIndex];
            }

            // Progress bar
            if (progressBar != null)
            {
                progressBar.value = progress;
            }

            // Progress text
            if (progressText != null)
            {
                progressText.text =
                    Mathf.RoundToInt(progress * 100f) + "%";
            }

            // Khi tải xong
            if (progress >= 1f)
            {
                CheckConsent();
                yield break;
            }

            yield return null;
        }
    }

    private void CheckConsent()
    {
        // This is the game's only boot screen, so the values a new save needs are settled
        // here; the template does the same work inside a loading scene this game skips.
        FirstRunProfile.Seed();

        bool hasAccepted = FirstRunProfile.HasConsent();

        if (hasAccepted)
        {
            loadingOperation.allowSceneActivation = true;
        }
        else
        {
            consentPopup.Ask(ContinueAfterConsent);
        }
    }

    private void ContinueAfterConsent()
    {
        loadingOperation.allowSceneActivation = true;
    }
}