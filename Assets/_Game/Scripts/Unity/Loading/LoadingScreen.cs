using UnityEngine;

namespace AirlinePop.SingleLine.Unity
{
    /// <summary>
    /// The fun screen on the scene cover: follows the scene transition and
    /// drives its parts only while a change is under way.
    /// </summary>
    public sealed class LoadingScreen : MonoBehaviour
    {
        [SerializeField] private SceneTransition sceneController;
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
            LoadingAnimations.Restart(parts);
        }

        private void HandleReady()
        {
            LoadingAnimations.Complete(parts);
        }

        private void HandleFinished()
        {
            LoadingAnimations.Stop(parts);
        }

#if UNITY_EDITOR
        public void EditorLink(SceneTransition linkedController, LoadingAnimation[] linkedParts)
        {
            sceneController = linkedController;
            parts = linkedParts;
        }
#endif
    }
}
