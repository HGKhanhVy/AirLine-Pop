using UnityEngine;

public class TutorialSwitcher : MonoBehaviour
{
    [SerializeField] private Uibase tutorialHTP;
    [SerializeField] private Uibase tutorialUndo;


    public void OpenUndo()
    {
        tutorialHTP.HideQuietly();
        tutorialUndo.Show();
    }


    public void OpenHTP()
    {
        tutorialUndo.HideQuietly();
        tutorialHTP.Show();
    }
}