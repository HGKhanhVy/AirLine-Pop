using UnityEngine.SceneManagement;

namespace AirlinePop.SingleLine.Unity
{
    /// <summary>
    /// Scene changes behind the scene cover. Falls back to a plain load when there is no
    /// cover, which is the case for a scene played on its own in the Editor.
    /// </summary>
    public sealed class SceneNavigator : ISceneNavigator
    {
        private readonly SceneTransition transition;

        public SceneNavigator(SceneTransition transition)
        {
            this.transition = transition;
        }

        public bool IsInGameplay => SceneManager.GetActiveScene().name == GameScenes.Gameplay;

        public void GoHome()
        {
            Load(GameScenes.Home);
        }

        public void GoToGameplay()
        {
            Load(GameScenes.Gameplay);
        }

        private void Load(string sceneName)
        {
            if (transition != null)
            {
                transition.ChangeScene(sceneName);
                return;
            }

            SceneManager.LoadScene(sceneName);
        }
    }
}
