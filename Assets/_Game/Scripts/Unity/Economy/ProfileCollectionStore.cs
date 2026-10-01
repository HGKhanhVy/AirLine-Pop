using System;
using ASTeams.Base.Data;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Keeps the collection inside the SDK profile's parameter blob. The profile saves
    /// the coin field and the blob in one PlayerPrefs write, so changing the coin field
    /// first and then storing the blob commits both at once.
    /// </summary>
    public sealed class ProfileCollectionStore : ICollectionStore
    {
        private const string Key = "collection";

        private readonly UserProfileController profile;

        public ProfileCollectionStore(UserProfileController profile)
        {
            this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
            State = profile.GetParam<CollectionSave>(Key) ?? new CollectionSave();
        }

        public CollectionSave State { get; }

        public long Coins => profile.userData == null ? 0 : profile.userData.coin;

        public void Commit(long coinDelta)
        {
            if (profile.userData != null && coinDelta != 0)
            {
                profile.userData.coin = Math.Max(0, profile.userData.coin + coinDelta);
            }

            profile.SetParam(Key, State);

            if (coinDelta != 0)
            {
                GameplayEvents.RaiseCoinBalanceChanged(Coins);
            }
        }
    }
}
