using System;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Keeps the daily gifts in the cats' own saves: the day each one last brought a gift.
    /// The coins and the day are written in one save, so a gift is never paid twice or lost.
    /// </summary>
    public sealed class CatGiftService : ICatGiftService
    {
        private readonly ICollectionStore store;
        private readonly ICatCollectionService collection;
        private readonly EconomyConfigSO config;
        private readonly IDayClock clock;

        public CatGiftService(ICollectionStore store, ICatCollectionService collection, EconomyConfigSO config, IDayClock clock)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public event Action<string, int> OnGiftCollected;

        public bool HasGift(string catId)
        {
            CatSave cat = collection.GetSave(catId);
            return cat != null && cat.giftDay != clock.Today && CoinsFor(cat) > 0;
        }

        public int Collect(string catId)
        {
            if (!HasGift(catId))
            {
                return 0;
            }

            CatSave cat = collection.GetSave(catId);
            int coins = CoinsFor(cat);
            cat.giftDay = clock.Today;
            store.Commit(coins);
            OnGiftCollected?.Invoke(catId, coins);
            return coins;
        }

        private int CoinsFor(CatSave cat)
        {
            return config.GetDailyGiftCoins(config.GetBondLevel(cat.bondXP));
        }
    }
}
