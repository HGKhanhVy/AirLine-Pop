using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class HomeController : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text levelText;
    public TMP_Text coinText;

    [Header("Current Progress")]
    public int currentChapter = 1;
    public int currentLevel = 1;
    public int coins = 100;

    private void Start()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        levelText.text = "CHAPTER " + currentChapter.ToString("00") +
                          "\nLEVEL " + currentLevel.ToString("00");

        coinText.text = coins.ToString();
    }

    public void Play()
    {
        SceneManager.LoadScene("Game");
    }

    public void OpenTheme()
    {
        UiManager.Instance.ShowTheme();
    }

    public void OpenSettings()
    {
        UiManager.Instance.ShowSetting();
    }
    public void CloseSettings()
    {
        UiManager.Instance.HideSetting();
    }

    public void OpenTutorial()
    {
        UiManager.Instance.ShowTutorialHTP();
    }
}