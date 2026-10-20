using System;

namespace AirlinePop.SingleLine.Unity
{
    /// <summary>
    /// Keeps the collection in the player profile. The coins it costs and the collection
    /// it buys are written in one save, so neither can land without the other.
    /// </summary>
    public sealed class ProfileCollectionStore : ICollectionStore
    {
        private const string Key = "collection";

        private readonly PlayerProfile profile;

        public ProfileCollectionStore(PlayerProfile profile)
        {
            this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
            State = profile.Get<CollectionSave>(Key) ?? new CollectionSave();
        }

        public CollectionSave State { get; }

        public long Coins => profile.Coins;

        public void Commit(long coinDelta)
        {
            profile.Set(Key, State, coinDelta);

            if (coinDelta != 0)
            {
                GameplayEvents.RaiseCoinBalanceChanged(Coins);
            }
        }
    }
}
