using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Opens one of the shared popups. They live with the global UI that survives scene
    /// loads, so a screen in any scene can reach them from here.
    /// </summary>
    public sealed class OpenScreenButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private UiScreen screen;

        private void Reset()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            button.onClick.AddListener(Open);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(Open);
        }

        private void Open()
        {
            UiManager ui = UiManager.Instance;

            if (ui == null)
            {
                return;
            }

            switch (screen)
            {
                case UiScreen.Settings:
                    ui.ShowSetting();
                    break;
                case UiScreen.Store:
                    ui.ShowTheme();
                    break;
                case UiScreen.HowToPlay:
                    ui.ShowTutorialHTP();
                    break;
            }
        }
    }
}
