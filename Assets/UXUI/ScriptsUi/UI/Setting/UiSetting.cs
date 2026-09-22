using ASTeams.SingleLine.Unity;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Settings popup (GDD 9.1): music, sound, haptics, restore purchases, privacy settings and, while a level
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
    [Tooltip("Opens the privacy popup, where the player reviews consent and the policy.")]
    [SerializeField] private Button privacyButton;
    [SerializeField] private UiConsent consentPopup;
    [SerializeField] private Button restoreButton;

    [Header("Home")]
    [Tooltip("Shown only while a level is open; on the home screen it would lead nowhere.")]
    [SerializeField] private Button homeButton;
    [SerializeField] private RectTransform frame;
    [Tooltip("How much taller the frame grows to fit the Home button. It grows downwards so the title and close button stay put.")]
    [SerializeField, Min(0f)] private float homeRowHeight = 130f;

    private ISettingsService settings;
    private ISceneNavigator navigator;
    private IPurchaseService purchases;
    private Vector2 frameBaseSize;
    private Vector2 frameBasePosition;

    public void Initialize(ISettingsService settingsService, ISceneNavigator sceneNavigator, IPurchaseService purchaseService)
    {
        settings = settingsService;
        navigator = sceneNavigator;
        purchases = purchaseService;
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
        privacyButton.onClick.AddListener(OpenPrivacySettings);
        homeButton.onClick.AddListener(GoHome);
        restoreButton.onClick.AddListener(RestorePurchases);
    }

    private void OnDisable()
    {
        musicToggle.OnChanged -= HandleMusicChanged;
        soundToggle.OnChanged -= HandleSoundChanged;
        hapticToggle.OnChanged -= HandleHapticChanged;
        privacyButton.onClick.RemoveListener(OpenPrivacySettings);
        homeButton.onClick.RemoveListener(GoHome);
        restoreButton.onClick.RemoveListener(RestorePurchases);
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

    private void RestorePurchases()
    {
        purchases?.Restore(PurchaseFeedback.ShowRestoreResult);
    }

    private void OpenPrivacySettings()
    {
        consentPopup.Show();
    }
}
