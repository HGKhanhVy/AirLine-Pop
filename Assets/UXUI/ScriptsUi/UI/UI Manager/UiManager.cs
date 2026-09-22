using ASTeams.Base;
using ASTeams.Base.Ads;
using ASTeams.Base.Data;
using ASTeams.Base.UI;
using ASTeams.SingleLine.Unity;
using Sirenix.OdinInspector;
using UnityEngine;

public class UiManager : MonoBehaviour
{
    public static UiManager Instance { get; private set; }

    public UiSetting uiSetting;
    public UiConsent uiConsent;
    public Uibase uiNoInternet;
    public UiStore uiTheme;
    public Uibase uiTutorial;
    public TutorialHand tutorialHand;
    public TutorialUndoCanvas uiTutorialUndo;
    [SerializeField] private TutorialHandUndo tutorialHandUndo;
    [SerializeField] private StoreConfigSO storeConfig;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (Instance != this)
        {
            return;
        }

        // Start, not Awake: the SDK controllers must have run their own Awake first.
        var settings = new SdkSettingsService(AudioController.Instance, VibrationController.Instance, UserProfileController.Instance);
        settings.SyncWithProfile();

        var purchases = new SdkPurchaseService(AdsController.Instance, UserProfileController.Instance, storeConfig);

        uiSetting.Initialize(settings, new SdkSceneNavigator(UISceneController.Instance), purchases);
        uiTheme.Initialize(purchases);
        uiConsent.Initialize(new ProfileConsentService(UserProfileController.Instance));
    }


    public void ShowSetting()
    {
        uiSetting.Show();
    }

    public void HideSetting()
    {
        uiSetting.Hide();
    }
    public void ShowNoInternet()
    {
        uiNoInternet.Show();
    }

    public void HideNoInternet()
    {
        uiNoInternet.Hide();
    }
    public void ShowTheme()
    {
        uiTheme.Show();
    }

    public void HideTheme()
    {
        uiTheme.Hide();
    }
    public void ShowTutorialUndo()
    {
        uiTutorial.Hide();
        uiTutorialUndo.Show();

        tutorialHandUndo.Play();
    }
    public void HideTutorialUndo()
    {
        uiTutorialUndo.Hide();
    }
    public void ShowTutorialHTP()
    {
        uiTutorialUndo.Hide();
        uiTutorial.Show();
        tutorialHand.Play();
    }

    public void HideTutorialhtp()
    {
        tutorialHand.StopTutorial();
        uiTutorial.Hide();
    }

    
    [Button]
    private void TestShowUiSetting()
    {
        uiSetting.Show();
    }

    [Button]
    private void TestShowNoInternet()
    {
        uiNoInternet.Show();
    }
    [Button]
    private void TestShowTutorial()
    {
        uiTutorial.Show();
    }


}