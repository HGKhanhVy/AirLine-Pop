using ASTeams.SingleLine.Unity;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Settings popup (GDD 9.1): music, sound, haptics, the privacy link and, while a level
/// is open, the way back to the home screen. Only shows and forwards; what a switch does
/// and where it is saved belongs to the settings service.
/// </summary>
public class UiSetting : Uibase
{
    [Header("Switches")]
    [SerializeField] private SettingToggleView musicToggle;
    [SerializeField] private SettingToggleView soundToggle;
    [SerializeField] private SettingToggleView hapticToggle;

    [Header("Links")]
    [SerializeField] private Button privacyButton;
    [SerializeField] private SettingsConfigSO config;

    [Header("Home")]
    [Tooltip("Shown only while a level is open; on the home screen it would lead nowhere.")]
    [SerializeField] private Button homeButton;
    [SerializeField] private RectTransform frame;
    [Tooltip("How much taller the frame grows to fit the Home button. It grows downwards so the title and close button stay put.")]
    [SerializeField, Min(0f)] private float homeRowHeight = 130f;

    private ISettingsService settings;
    private ISceneNavigator navigator;
    private Vector2 frameBaseSize;
    private Vector2 frameBasePosition;

    public void Initialize(ISettingsService settingsService, ISceneNavigator sceneNavigator)
    {
        settings = settingsService;
        navigator = sceneNavigator;
    }

    protected override void Awake()
    {
        base.Awake();
        frameBaseSize = frame.sizeDelta;
        frameBasePosition = frame.anchoredPosition;
    }

    public override void Show()
    {
        // Activate first: the popup starts inactive, and Awake records the frame's
        // resting size only once it runs.
        base.Show();
        Refresh();
        ApplyHomeRow(navigator != null && navigator.IsInGameplay);
    }

    private void OnEnable()
    {
        musicToggle.OnChanged += HandleMusicChanged;
        soundToggle.OnChanged += HandleSoundChanged;
        hapticToggle.OnChanged += HandleHapticChanged;
        privacyButton.onClick.AddListener(OpenPrivacyPolicy);
        homeButton.onClick.AddListener(GoHome);
    }

    private void OnDisable()
    {
        musicToggle.OnChanged -= HandleMusicChanged;
        soundToggle.OnChanged -= HandleSoundChanged;
        hapticToggle.OnChanged -= HandleHapticChanged;
        privacyButton.onClick.RemoveListener(OpenPrivacyPolicy);
        homeButton.onClick.RemoveListener(GoHome);
    }

    private void Refresh()
    {
        if (settings == null)
        {
            return;
        }

        musicToggle.SetIsOn(settings.IsMusicOn);
        soundToggle.SetIsOn(settings.IsSoundOn);
        hapticToggle.SetIsOn(settings.IsHapticOn);
    }

    private void HandleMusicChanged(bool isOn)
    {
        settings?.SetMusicOn(isOn);
    }

    private void HandleSoundChanged(bool isOn)
    {
        settings?.SetSoundOn(isOn);
    }

    private void HandleHapticChanged(bool isOn)
    {
        settings?.SetHapticOn(isOn);
    }

    private void ApplyHomeRow(bool isVisible)
    {
        homeButton.gameObject.SetActive(isVisible);

        float extra = isVisible ? homeRowHeight : 0f;
        frame.sizeDelta = frameBaseSize + new Vector2(0f, extra);
        frame.anchoredPosition = frameBasePosition - new Vector2(0f, extra * 0.5f);
    }

    private void GoHome()
    {
        Hide();
        navigator?.GoHome();
    }

    private void OpenPrivacyPolicy()
    {
        if (config == null || string.IsNullOrEmpty(config.PrivacyPolicyUrl))
        {
            Debug.LogWarning("Privacy policy URL is not set in SettingsConfig.");
            return;
        }

        Application.OpenURL(config.PrivacyPolicyUrl);
    }
}
