using ASTeams.Base.Data;
using ASTeams.Base.Gameplay;
using ASTeams.Base.Level;
using TMPro;
using UnityEngine;

public class UILevel : MonoBehaviour
{
    private const string LevelPrefix = "Level";

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI levelTypeText;

    private LevelService _levelService;

    private void OnEnable()
    {
        BindLevelService();
        UserProfileController.Instance.OnUserChanged.AddListener(HandleUserChanged);
        RefreshUI();
    }

    private void OnDisable()
    {
        UnbindLevelService();

        if (UserProfileController.Instance != null)
        {
            UserProfileController.Instance.OnUserChanged.RemoveListener(HandleUserChanged);
        }
    }

    private void HandleUserChanged(UserData _)
    {
        RefreshUI();
    }

    private void HandleLevelLoaded(int levelNumber, LevelConfig levelConfig)
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        RefreshLevelText();
        RefreshLevelTypeText();
    }

    private void RefreshLevelText()
    {
        if (levelText == null)
        {
            return;
        }

        int level = ResolveLevelNumber();
        levelText.text = $"{LevelPrefix} {level}";
    }

    private void RefreshLevelTypeText()
    {
        if (levelTypeText == null)
        {
            return;
        }

        LevelConfig levelConfig = ResolveLevelConfig();
        levelTypeText.text = levelConfig != null ? FormatLevelType(levelConfig.type) : string.Empty;
    }

    private int ResolveLevelNumber()
    {
        if (_levelService != null)
        {
            return _levelService.CurrentLevelNumber;
        }

        return UserProfileController.Instance.LEVEL;
    }

    private LevelConfig ResolveLevelConfig()
    {
        BindLevelService();
        return _levelService != null ? _levelService.CurrentLevel : null;
    }

    private void BindLevelService()
    {
        if (_levelService != null)
        {
            return;
        }

        if (GameController.Instance == null || GameController.Instance.Services == null)
        {
            return;
        }

        if (GameController.Instance.Services.TryGet(out LevelService levelService))
        {
            _levelService = levelService;
            _levelService.OnLevelLoaded += HandleLevelLoaded;
        }
    }

    private void UnbindLevelService()
    {
        if (_levelService == null)
        {
            return;
        }

        _levelService.OnLevelLoaded -= HandleLevelLoaded;
        _levelService = null;
    }

    private static string FormatLevelType(LevleType levelType)
    {
        switch (levelType)
        {
            case LevleType.SuperHard:
                return "SHard";
            default:
                return levelType.ToString();
        }
    }
}