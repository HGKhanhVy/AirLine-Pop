using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Carries the level the player picked on the route map over to the gameplay scene,
    /// which takes it once on start. With no request, gameplay opens the saved level as
    /// before. Only a level already reached can be asked for: the campaign never skips ahead.
    /// </summary>
    public static class LevelLaunchRequest
    {
        private static int requested;

        public static void Request(int levelNumber)
        {
            requested = Mathf.Max(0, levelNumber);
        }

        /// <summary>The requested level, if one is waiting; the request is used up either way.</summary>
        public static bool TryTake(out int levelNumber)
        {
            levelNumber = requested;
            requested = 0;
            return levelNumber > 0;
        }

        // Enter Play Mode without a domain reload keeps statics; start every session clean.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlaySession()
        {
            requested = 0;
        }
    }
}
