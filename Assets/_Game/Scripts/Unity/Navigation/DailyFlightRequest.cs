using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Carries the player's choice of today's daily flight over to the gameplay scene, which
    /// takes it once on start. With no request, gameplay opens the campaign as before.
    /// </summary>
    public static class DailyFlightRequest
    {
        private static bool isRequested;

        public static void Request()
        {
            isRequested = true;
        }

        /// <summary>True when the daily flight was asked for; the request is used up either way.</summary>
        public static bool TryTake()
        {
            bool requested = isRequested;
            isRequested = false;
            return requested;
        }

        // Enter Play Mode without a domain reload keeps statics; start every session clean.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlaySession()
        {
            isRequested = false;
        }
    }
}
