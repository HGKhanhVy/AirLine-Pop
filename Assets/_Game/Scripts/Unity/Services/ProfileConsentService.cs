using ASTeams.Base.Data;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Consent kept in the saved profile beside the settings (GDD 14). Whether the player
    /// has answered at all stays with <see cref="FirstRunProfile"/>, which also carries
    /// over the answer an older build stored elsewhere.
    /// </summary>
    public sealed class ProfileConsentService : IConsentService
    {
        private const string PersonalizedAdsKey = "consentPersonalizedAds";

        private readonly UserProfileController profile;

        public ProfileConsentService(UserProfileController profile)
        {
            this.profile = profile;
        }

        public bool HasAnswered => FirstRunProfile.HasConsent();

        public bool AllowsPersonalizedAds => profile != null && profile.GetParam<bool>(PersonalizedAdsKey);

        public void Save(bool allowsPersonalizedAds)
        {
            FirstRunProfile.AcceptConsent();
            profile?.SetParam(PersonalizedAdsKey, allowsPersonalizedAds);
        }
    }
}
