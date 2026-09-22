using System;
using ASTeams.SingleLine.Unity;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Privacy popup (GDD 12.3): asked once before the game starts, and reopened from the
/// settings screen so the player can change the answer any time. The personalized ads
/// switch starts off on a first answer, since consent may not be pre-ticked.
/// </summary>
public class UiConsent : Uibase
{
    [SerializeField] private SettingToggleView personalizedAdsToggle;
    [SerializeField] private Button privacyPolicyButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button acceptAllButton;
    [SerializeField] private SettingsConfigSO config;

    private IConsentService consent;
    private Action onAnswered;
    private bool allowsPersonalizedAds;

    public void Initialize(IConsentService consentService)
    {
        consent = consentService;
    }

    /// <summary>Opens the popup and calls back once the player has saved an answer.</summary>
    public void Ask(Action answered)
    {
        onAnswered = answered;
        Show();
    }

    public override void Show()
    {
        base.Show();
        allowsPersonalizedAds = consent != null && consent.HasAnswered && consent.AllowsPersonalizedAds;
        personalizedAdsToggle.SetIsOn(allowsPersonalizedAds);
    }

    private void OnEnable()
    {
        personalizedAdsToggle.OnChanged += HandleToggleChanged;
        privacyPolicyButton.onClick.AddListener(OpenPrivacyPolicy);
        saveButton.onClick.AddListener(SaveChoice);
        acceptAllButton.onClick.AddListener(AcceptAll);
    }

    private void OnDisable()
    {
        personalizedAdsToggle.OnChanged -= HandleToggleChanged;
        privacyPolicyButton.onClick.RemoveListener(OpenPrivacyPolicy);
        saveButton.onClick.RemoveListener(SaveChoice);
        acceptAllButton.onClick.RemoveListener(AcceptAll);
    }

    private void HandleToggleChanged(bool isOn)
    {
        allowsPersonalizedAds = isOn;
    }

    private void SaveChoice()
    {
        Answer(allowsPersonalizedAds);
    }

    private void AcceptAll()
    {
        personalizedAdsToggle.SetIsOn(true);
        Answer(true);
    }

    private void Answer(bool allowsPersonalized)
    {
        consent?.Save(allowsPersonalized);
        Hide();

        Action answered = onAnswered;
        onAnswered = null;
        answered?.Invoke();
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
