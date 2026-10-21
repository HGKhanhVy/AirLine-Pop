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

        private const string SeededParam = "seeded";

        public static bool HasConsent()
        {
            return PlayerProfile.Instance.Get<bool>(ConsentParam);
        }

        public static void AcceptConsent()
        {
            PlayerProfile.Instance.Set(ConsentParam, true);
        }

        /// <summary>Gives a save that has never been played its starting coins, once.</summary>
        public static void Seed(int startingCoins)
        {
            PlayerProfile profile = PlayerProfile.Instance;

            if (profile.Get<bool>(SeededParam))
            {
                return;
            }

            profile.Set(SeededParam, true, startingCoins);
        }
    }
}
