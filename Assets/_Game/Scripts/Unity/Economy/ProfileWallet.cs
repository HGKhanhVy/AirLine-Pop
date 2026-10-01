using System;
using ASTeams.Base.Data;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The SDK profile's coin balance, announced whenever the profile saves, so a label
    /// stays right whichever system spent or granted the coins.
    /// </summary>
    public sealed class ProfileWallet : IWallet, IDisposable
    {
        private readonly UserProfileController profile;
        private long lastCoins;

        public ProfileWallet(UserProfileController profile)
        {
            this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
            lastCoins = Coins;
            profile.OnUserChanged.AddListener(HandleUserChanged);
        }

        public event Action<long> OnCoinsChanged;

        public long Coins => profile.userData == null ? 0 : profile.userData.coin;

        public void Dispose()
        {
            profile.OnUserChanged.RemoveListener(HandleUserChanged);
        }

        private void HandleUserChanged(UserData data)
        {
            if (data == null || data.coin == lastCoins)
            {
                return;
            }

            lastCoins = data.coin;
            OnCoinsChanged?.Invoke(lastCoins);
        }
    }
}
