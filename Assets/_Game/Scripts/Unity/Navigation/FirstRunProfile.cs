using ASTeams.Base;
using ASTeams.Base.Data;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// What the boot screen has to settle before the game proper starts: the consent
    /// answer, and the values a brand new save needs.
    ///
    /// The template does both inside its own loading scene, which this game does not use;
    /// the splash screen is the one boot screen GDD 9.1 asks for, and it calls in here.
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

        /// <summary>
        /// True once the player has answered. An install from the first build answered into
        /// PlayerPrefs, so that answer is carried over rather than asked again.
        /// </summary>
        public static bool HasConsent()
        {
            UserProfileController profile = UserProfileController.Instance;

            if (profile != null && profile.GetParam<bool>(ConsentParam))
            {
                return true;
            }

            if (PlayerPrefs.GetInt(LegacyConsentKey, 0) != 1)
            {
                return false;
            }

            profile?.SetParam(ConsentParam, true);
            return true;
        }

        public static void AcceptConsent()
        {
            UserProfileController.Instance?.SetParam(ConsentParam, true);
        }

        /// <summary>
        /// A save that has never been played needs the numbers the template's services read
        /// through the profile's parameter blob. The profile itself already fills in coins
        /// and level when it creates a user; these are the rest.
        /// </summary>
        public static void Seed()
        {
            UserProfileController profile = UserProfileController.Instance;

            if (profile == null || profile.GetParam<bool>("firstTime"))
            {
                return;
            }

            GameConfig config = ConfigController.Instance == null ? null : ConfigController.Instance.GameConfig;

            profile.SetParam("firstTime", true);
            profile.SetParam("currentLevel", 1);
            profile.SetParam("adsTicket", 0);
            profile.SetParam("winStreak", 0);
            profile.SetParam("openedWinStreakCount", 0);

            if (config != null)
            {
                profile.SetParam("timeToAddLife", config.refillLifeTime);
                profile.SetParam("coin", config.startCoin);
                profile.SetParam("life", config.maxLife);
            }

            profile.SetParam("booster_1", 3);
            profile.SetParam("booster_2", 3);
            profile.SetParam("booster_3", 3);
            profile.SetParam("booster_1_tutorial", false);
            profile.SetParam("booster_2_tutorial", false);
            profile.SetParam("booster_3_tutorial", false);
        }
    }
}
