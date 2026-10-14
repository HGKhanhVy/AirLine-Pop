using System;
using UnityEngine;

namespace AirlinePop.SingleLine.Unity
{
    /// <summary>
    /// Drives a set of loading screen parts together, whichever screen holds them.
    ///
    /// The parts run inside the scene controller's load coroutine, so an exception in one
    /// would end that coroutine and leave the player stuck on the old scene. The parts are
    /// only decoration: a failing one is logged and skipped, and the scene change goes on.
    /// </summary>
    public static class LoadingAnimations
    {
        public static void Restart(LoadingAnimation[] parts)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                LoadingAnimation part = parts[i];
                Run(part, () =>
                {
                    part.Stop();
                    part.Play();
                });
            }
        }

        public static void Complete(LoadingAnimation[] parts)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                Run(parts[i], parts[i].Complete);
            }
        }

        public static void Stop(LoadingAnimation[] parts)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                Run(parts[i], parts[i].Stop);
            }
        }

        private static void Run(LoadingAnimation part, Action action)
        {
            if (part == null)
            {
                return;
            }

            try
            {
                action();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, part);
            }
        }
    }
}
