using ASTeams.Base;
using Sirenix.OdinInspector;
using UnityEngine;

public class UiManager : MonoBehaviour
{
    public static UiManager Instance { get; private set; }

    public Uibase uiSetting;
    public Uibase uiNoInternet;
    public Uibase uiTheme;
    public Uibase uiTutorial;
    public TutorialHand tutorialHand;
    public TutorialUndoCanvas uiTutorialUndo;
    [SerializeField] private TutorialHandUndo tutorialHandUndo;
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