namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The player's privacy answer (GDD 12.3). Ads must read this before they start: no
    /// personalized ads until the player has said yes to them.
    /// </summary>
    public interface IConsentService
    {
        bool HasAnswered { get; }

        bool AllowsPersonalizedAds { get; }

        void Save(bool allowsPersonalizedAds);
    }
}
