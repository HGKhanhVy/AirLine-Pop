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
    public GameObject consentPanel;

    private AsyncOperation loadingOperation;

   
    private void Start()
    {
        // The consent answer is kept: GDD 12.3 asks once and remembers. Clearing the key
        // here made the panel appear on every launch, which is a debug aid, not the flow.
        StartCoroutine(LoadScene());
    }


    private IEnumerator LoadScene()
    {
        loadingOperation =
            SceneManager.LoadSceneAsync(nextSceneName);

        loadingOperation.allowSceneActivation = false;

        while (!loadingOperation.isDone)
        {
            float progress = Mathf.Clamp01(
                loadingOperation.progress / 0.9f
            );

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
            // Đã đồng ý
            loadingOperation.allowSceneActivation = true;
        }
        else
        {
            // Chưa đồng ý
            consentPanel.SetActive(true);
        }
    }

    public void AcceptConsent()
    {
        FirstRunProfile.AcceptConsent();

        consentPanel.SetActive(false);

        loadingOperation.allowSceneActivation = true;
    }
}