using ASTeams.Base.Data;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class HomeController : MonoBehaviour
{
    /// <summary>Levels in one chapter, matching the exported campaign files.</summary>
    private const int LevelsPerChapter = 30;

    /// <summary>
    /// Straight to the board. The managers live from the splash screen on, so the
    /// template's own loading scene would only be a second loading bar over the same work.
    /// </summary>
    private const string GameplayScene = "Gameplay";

    [Header("UI")]
    public TMP_Text levelText;
    public TMP_Text coinText;

    [Header("Fallback progress")]
    [Tooltip("Shown only when no save is loaded, which happens when this scene is played on its own.")]
    public int currentChapter = 1;
    public int currentLevel = 1;
    public int coins = 100;

    private void Start()
    {
        ReadSave();
        UpdateUI();
    }

    /// <summary>
    /// The home screen shows where the player actually is, so it reads the same profile
    /// gameplay writes on every win instead of the numbers typed into the Inspector.
    /// </summary>
    private void ReadSave()
    {
        UserProfileController profile = UserProfileController.Instance;

        if (profile == null || profile.userData == null)
        {
            return;
        }

        int level = Mathf.Max(1, profile.LEVEL);
        currentChapter = ((level - 1) / LevelsPerChapter) + 1;
        currentLevel = ((level - 1) % LevelsPerChapter) + 1;
        coins = (int)profile.userData.coin;
    }

    private void UpdateUI()
    {
        levelText.text = "CHAPTER " + currentChapter.ToString("00") +
                          "\nLEVEL " + currentLevel.ToString("00");

        coinText.text = coins.ToString();
    }

    public void Play()
    {
        SceneManager.LoadScene(GameplayScene);
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
