using System;

namespace AirlinePop.SingleLine.Unity
{
    /// <summary>
    /// The profile's coin balance, announced whenever the profile saves, so a label
    /// stays right whichever system spent or granted the coins.
    /// </summary>
    public sealed class ProfileWallet : IWallet, IDisposable
    {
        private readonly PlayerProfile profile;
        private long lastCoins;

        public ProfileWallet(PlayerProfile profile)
        {
            this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
            lastCoins = Coins;
            profile.Changed += HandleProfileChanged;
        }

        public event Action<long> OnCoinsChanged;

        public long Coins => profile.Coins;

        public void Dispose()
        {
            profile.Changed -= HandleProfileChanged;
        }

        private void HandleProfileChanged()
        {
            if (profile.Coins == lastCoins)
            {
                return;
            }

            lastCoins = profile.Coins;
            OnCoinsChanged?.Invoke(lastCoins);
        }
    }
}
