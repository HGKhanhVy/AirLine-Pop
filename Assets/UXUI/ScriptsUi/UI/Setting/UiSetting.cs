using ASTeams.SingleLine.Unity;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Settings popup (GDD 9.1): music, sound, haptics and the privacy link. Only shows and
/// forwards; what a switch does and where it is saved belongs to the settings service.
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

    private ISettingsService settings;

    public void Initialize(ISettingsService settingsService)
    {
        settings = settingsService;
    }

    public override void Show()
    {
        Refresh();
        base.Show();
    }

    private void OnEnable()
    {
        musicToggle.OnChanged += HandleMusicChanged;
        soundToggle.OnChanged += HandleSoundChanged;
        hapticToggle.OnChanged += HandleHapticChanged;
        privacyButton.onClick.AddListener(OpenPrivacyPolicy);
    }

    private void OnDisable()
    {
        musicToggle.OnChanged -= HandleMusicChanged;
        soundToggle.OnChanged -= HandleSoundChanged;
        hapticToggle.OnChanged -= HandleHapticChanged;
        privacyButton.onClick.RemoveListener(OpenPrivacyPolicy);
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
