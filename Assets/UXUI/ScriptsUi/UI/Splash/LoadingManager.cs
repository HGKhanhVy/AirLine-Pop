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
        PlayerPrefs.DeleteKey("HasAcceptedConsent");
        PlayerPrefs.Save();
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
        bool hasAccepted =
            PlayerPrefs.GetInt("HasAcceptedConsent", 0) == 1;

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
        PlayerPrefs.SetInt("HasAcceptedConsent", 1);
        PlayerPrefs.Save();

        consentPanel.SetActive(false);

        loadingOperation.allowSceneActivation = true;
    }
}