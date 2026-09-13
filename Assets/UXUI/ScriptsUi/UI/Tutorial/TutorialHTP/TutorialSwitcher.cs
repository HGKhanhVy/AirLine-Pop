using UnityEngine;

public class TutorialSwitcher : MonoBehaviour
{
    [SerializeField] private Uibase tutorialHTP;
    [SerializeField] private Uibase tutorialUndo;


    public void OpenUndo()
    {
        tutorialHTP.Hide();
        tutorialUndo.Show();
    }


    public void OpenHTP()
    {
        tutorialUndo.Hide();
        tutorialHTP.Show();
    }
}