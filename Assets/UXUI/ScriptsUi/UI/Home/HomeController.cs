using ASTeams.Base.Data;
using ASTeams.Base.UI;
using ASTeams.SingleLine.Unity;
using UnityEngine;
using TMPro;

public class HomeController : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text levelText;
    public TMP_Text coinText;

    [Header("Fallback progress")]
    [Tooltip("Shown only when no save is loaded, which happens when this scene is played on its own.")]
    public int currentLevel = 1;
    public int coins = 100;

    private ISceneNavigator navigator;
    private bool isLeaving;

    private void Awake()
    {
        navigator = new SdkSceneNavigator(UISceneController.Instance);
    }

    private void OnEnable()
    {
        GameplayEvents.OnCoinBalanceChanged += HandleCoinBalanceChanged;
    }

    private void OnDisable()
    {
        GameplayEvents.OnCoinBalanceChanged -= HandleCoinBalanceChanged;
    }

    private void Start()
    {
        ReadSave();
        UpdateUI();
    }

    /// <summary>Coins spent in a popup over this screen, such as buying a skin.</summary>
    private void HandleCoinBalanceChanged(long balance)
    {
        coins = (int)balance;
        coinText.text = coins.ToString();
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

        // The same absolute number the gameplay HUD shows, so both screens agree.
        currentLevel = Mathf.Max(1, profile.LEVEL);
        coins = (int)profile.userData.coin;
    }

    private void UpdateUI()
    {
        levelText.text = "LEVEL " + currentLevel;
        coinText.text = coins.ToString();
    }

    public void Play()
    {
        // A second tap during the transition would start a second load.
        if (isLeaving)
        {
            return;
        }

        isLeaving = true;
        navigator.GoToGameplay();
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
