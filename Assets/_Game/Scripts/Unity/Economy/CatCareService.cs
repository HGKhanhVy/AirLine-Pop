using System;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Care rules from GDD 5: petting and playing grant bond once per cat per day, meals
    /// grant bond up to a daily cap and always cost one food. Actions after the daily
    /// bond still play out; only the reward is withheld.
    /// </summary>
    public sealed class CatCareService : ICatCareService
    {
        private readonly ICollectionStore store;
        private readonly ICatCollectionService collection;
        private readonly EconomyConfigSO config;
        private readonly IDayClock clock;

        public CatCareService(ICollectionStore store, ICatCollectionService collection, EconomyConfigSO config, IDayClock clock)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public event Action OnCareChanged;

        public int FoodCount => store.State.foodCount;

        public int FoodPrice => config.FoodPrice;

        public PurchaseOutcome TryBuyFood(int quantity)
        {
            if (quantity <= 0 || quantity > config.MaxFoodPerPurchase)
            {
                return PurchaseOutcome.Invalid;
            }

            long cost = (long)quantity * config.FoodPrice;

            if (store.Coins < cost)
            {
                return PurchaseOutcome.NotEnoughCoins;
            }

            store.State.foodCount += quantity;
            store.Commit(-cost);
            OnCareChanged?.Invoke();
            return PurchaseOutcome.Purchased;
        }

        public PurchaseOutcome TryBuyFoodPack(SnackPack pack)
        {
            if (pack == null || pack.Quantity <= 0)
            {
                return PurchaseOutcome.Invalid;
            }

            if (store.Coins < pack.Price)
            {
                return PurchaseOutcome.NotEnoughCoins;
            }

            store.State.foodCount += pack.Quantity;
            store.Commit(-pack.Price);
            OnCareChanged?.Invoke();
            return PurchaseOutcome.Purchased;
        }

        public CareOutcome Pet(string catId)
        {
            CatSave cat = GetFreshSave(catId);

            if (cat == null)
            {
                return CareOutcome.Invalid;
            }

            if (cat.hasPettedToday)
            {
                return CareOutcome.NoBond;
            }

            cat.hasPettedToday = true;
            return Grant(cat, config.PetBond);
        }

        public CareOutcome Play(string catId)
        {
            CatSave cat = GetFreshSave(catId);

            if (cat == null)
            {
                return CareOutcome.Invalid;
            }

            if (cat.hasPlayedToday)
            {
                return CareOutcome.NoBond;
            }

            cat.hasPlayedToday = true;
            return Grant(cat, config.PlayBond);
        }

        public bool CanFeed(string catId)
        {
            CatSave cat = GetFreshSave(catId);
            return cat != null && store.State.foodCount > 0 && cat.mealsToday < config.RewardedMealsPerDay;
        }

        public CareOutcome Feed(string catId)
        {
            CatSave cat = GetFreshSave(catId);

            if (cat == null)
            {
                return CareOutcome.Invalid;
            }

            if (cat.mealsToday >= config.RewardedMealsPerDay)
            {
                return CareOutcome.AlreadyFed;
            }

            if (store.State.foodCount <= 0)
            {
                return CareOutcome.NoFood;
            }

            store.State.foodCount--;
            cat.mealsToday++;
            return Grant(cat, config.MealBond);
        }

        public int GetBondLevel(string catId)
        {
            CatSave cat = collection.GetSave(catId);
            return cat == null ? 1 : config.GetBondLevel(cat.bondXP);
        }

        public float GetBondProgress(string catId)
        {
            CatSave cat = collection.GetSave(catId);
            return cat == null ? 0f : config.GetBondProgress(cat.bondXP);
        }

        private CareOutcome Grant(CatSave cat, int bond)
        {
            cat.bondXP += bond;
            store.Commit(0);
            OnCareChanged?.Invoke();
            return CareOutcome.BondGained;
        }

        /// <summary>The cat's save with its daily counters rolled over if the day changed.</summary>
        private CatSave GetFreshSave(string catId)
        {
            CatSave cat = collection.GetSave(catId);

            if (cat == null)
            {
                return null;
            }

            int today = clock.Today;

            if (cat.interactionDay != today)
            {
                cat.interactionDay = today;
                cat.hasPettedToday = false;
                cat.hasPlayedToday = false;
                cat.mealsToday = 0;
            }

            return cat;
        }
    }
}
