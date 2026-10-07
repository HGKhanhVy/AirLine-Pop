using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The same cat loading screen shown on the boot splash: there is no scene change to
    /// follow there, so its parts play for as long as the splash is up.
    /// </summary>
    public sealed class SplashLoadingScreen : MonoBehaviour
    {
        [SerializeField] private LoadingAnimation[] parts = new LoadingAnimation[0];

        private void OnEnable()
        {
            LoadingAnimations.Restart(parts);
        }

        private void OnDisable()
        {
            LoadingAnimations.Stop(parts);
        }

#if UNITY_EDITOR
        public void EditorLink(LoadingAnimation[] linkedParts)
        {
            parts = linkedParts;
        }
#endif
    }
}
