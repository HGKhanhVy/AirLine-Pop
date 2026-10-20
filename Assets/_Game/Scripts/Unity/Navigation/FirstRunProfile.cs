using UnityEngine;

namespace AirlinePop.SingleLine.Unity
{
    /// <summary>
    /// What the boot screen settles before the game proper starts: the consent answer,
    /// and the starting coins of a brand new save.
    /// </summary>
    public static class FirstRunProfile
    {
        /// <summary>
        /// Where the answer lives. GDD 13 files consent under the player's settings, so it
        /// travels with the rest of the profile rather than sitting in a loose key.
        /// </summary>
        private const string ConsentParam = "consent";

        /// <summary>The key the first build wrote. Read once so old installs are not asked twice.</summary>
        private const string LegacyConsentKey = "HasAcceptedConsent";

        private const string SeededParam = "seeded";

        // Saves from earlier builds mark their first launch with this instead.
        private const string LegacySeededParam = "firstTime";

        /// <summary>
        /// True once the player has answered. An install from the first build answered into
        /// PlayerPrefs, so that answer is carried over rather than asked again.
        /// </summary>
        public static bool HasConsent()
        {
            PlayerProfile profile = PlayerProfile.Instance;

            if (profile.Get<bool>(ConsentParam))
            {
                return true;
            }

            if (PlayerPrefs.GetInt(LegacyConsentKey, 0) != 1)
            {
                return false;
            }

            profile.Set(ConsentParam, true);
            return true;
        }

        public static void AcceptConsent()
        {
            PlayerProfile.Instance.Set(ConsentParam, true);
        }

        /// <summary>Gives a save that has never been played its starting coins, once.</summary>
        public static void Seed(int startingCoins)
        {
            PlayerProfile profile = PlayerProfile.Instance;

            if (profile.Get<bool>(SeededParam) || profile.Get<bool>(LegacySeededParam))
            {
                return;
            }

            profile.Set(SeededParam, true, startingCoins);
        }
    }
}
