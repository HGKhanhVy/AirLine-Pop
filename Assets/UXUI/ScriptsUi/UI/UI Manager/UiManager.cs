using ASTeams.Base;
using Sirenix.OdinInspector;
using UnityEngine;

public class UiManager : MonoBehaviour
{
    public static UiManager Instance { get; private set; }

    public Uibase uiSetting;
    public Uibase uiNoInternet;
    public Uibase uiTheme;
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

    [Button]
    private void testShowUiSetting()
    {
        uiSetting.Show();
    }

    [Button]
    private void TestShowNoInternet()
    {
        uiNoInternet.Show();
    }
}