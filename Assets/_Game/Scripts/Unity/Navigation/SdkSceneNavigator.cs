using ASTeams.Base.UI;
using UnityEngine.SceneManagement;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Scene changes through the SDK's scene controller, which covers the screen, loads
    /// in the background and uncovers once the new scene has settled, so the player never
    /// sees a frozen frame or a blank one. Falls back to a plain load when the controller
    /// is missing, which is the case for a scene played on its own in the Editor.
    /// </summary>
    public sealed class SdkSceneNavigator : ISceneNavigator
    {
        private readonly UISceneController sceneController;

        public SdkSceneNavigator(UISceneController sceneController)
        {
            this.sceneController = sceneController;
        }

        public bool IsInGameplay => SceneManager.GetActiveScene().name == SceneName.Gameplay;

        public void GoHome()
        {
            Load(SceneName.Home);
        }

        public void GoToGameplay()
        {
            Load(SceneName.Gameplay);
        }

        private void Load(string sceneName)
        {
            if (sceneController != null)
            {
                sceneController.ChangeScene(sceneName);
                return;
            }

            SceneManager.LoadScene(sceneName);
        }
    }
}
