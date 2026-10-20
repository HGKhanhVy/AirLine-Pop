using UnityEngine;

namespace AirlinePop.SingleLine.Unity
{
    /// <summary>
    /// The object that lives for the whole session: holds the audio and the scene cover,
    /// and sets the frame rate and screen timeout once. Every scene carries a copy so it
    /// can be played on its own in the editor; the first one to wake keeps the job and
    /// any later copy removes itself.
    /// </summary>
    public sealed class AppRoot : MonoBehaviour
    {
        private const int TargetFrameRate = 60;

        private static AppRoot current;

        private void Awake()
        {
            if (current != null && current != this)
            {
                // Removed before its children wake, so they never register as a second set.
                DestroyImmediate(gameObject);
                return;
            }

            current = this;
            DontDestroyOnLoad(gameObject);

            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFrameRate;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        private void OnDestroy()
        {
            if (current == this)
            {
                current = null;
            }
        }
    }
}
