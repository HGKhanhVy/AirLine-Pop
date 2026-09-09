using UnityEngine;
using UnityEngine.SceneManagement;

public class ConsentManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject consentPanel;

    [Header("Scene")]
    public string nextSceneName = "Home";

    private const string ConsentKey = "HasAcceptedConsent";

    private void Start()
    {
        CheckConsent();
    }

    private void CheckConsent()
    {
        bool hasAccepted =
            PlayerPrefs.GetInt(ConsentKey, 0) == 1;

        if (hasAccepted)
        {
            // Đã đồng ý từ trước
            consentPanel.SetActive(false);

            LoadNextScene();
        }
        else
        {
            // Chưa đồng ý
            consentPanel.SetActive(true);
        }
    }

    public void AcceptConsent()
    {
        // Lưu trạng thái đồng ý
        PlayerPrefs.SetInt(ConsentKey, 1);
        PlayerPrefs.Save();

        // Ẩn bảng Consent
        consentPanel.SetActive(false);

        // Sang Scene tiếp theo
        LoadNextScene();
    }

    private void LoadNextScene()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}