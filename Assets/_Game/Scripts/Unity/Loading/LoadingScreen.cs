using ASTeams.Base.UI;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The fun screen on the scene change overlay: follows the SDK's scene controller and
    /// drives its parts only while a change is under way.
    /// </summary>
    public sealed class LoadingScreen : MonoBehaviour
    {
        [SerializeField] private UISceneController sceneController;
        [SerializeField] private LoadingAnimation[] parts = new LoadingAnimation[0];

        private void OnEnable()
        {
            sceneController.TransitionStarted += HandleStarted;
            sceneController.SceneReady += HandleReady;
            sceneController.TransitionFinished += HandleFinished;
        }

        private void OnDisable()
        {
            sceneController.TransitionStarted -= HandleStarted;
            sceneController.SceneReady -= HandleReady;
            sceneController.TransitionFinished -= HandleFinished;
            HandleFinished();
        }

        private void HandleStarted()
        {
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i].Stop();
                parts[i].Play();
            }
        }

        private void HandleReady()
        {
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i].Complete();
            }
        }

        private void HandleFinished()
        {
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i].Stop();
            }
        }

#if UNITY_EDITOR
        public void EditorLink(UISceneController linkedController, LoadingAnimation[] linkedParts)
        {
            sceneController = linkedController;
            parts = linkedParts;
        }
#endif
    }
}
