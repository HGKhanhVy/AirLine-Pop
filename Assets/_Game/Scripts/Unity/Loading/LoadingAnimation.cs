using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// One moving part of the loading screen. The screen plays every part when a scene
    /// change starts, lets each wrap up when the new scene is ready, and stops them all once
    /// the overlay is gone, so nothing animates while the screen is hidden.
    /// </summary>
    public abstract class LoadingAnimation : MonoBehaviour
    {
        public abstract void Play();

        public virtual void Complete()
        {
        }

        public abstract void Stop();
    }
}
