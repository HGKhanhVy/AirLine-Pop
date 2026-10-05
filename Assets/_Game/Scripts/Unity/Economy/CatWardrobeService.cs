using System;
using System.Collections.Generic;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Keeps what each regular wears in its own save. An accessory unlocks at its loyalty
    /// card, and a cat that somehow wears one it has not earned is shown without it.
    /// </summary>
    public sealed class CatWardrobeService : ICatWardrobeService
    {
        private readonly ICollectionStore store;
        private readonly ICatCollectionService collection;
        private readonly EconomyConfigSO config;
        private readonly AccessoryCatalogSO catalog;

        public CatWardrobeService(ICollectionStore store, ICatCollectionService collection, EconomyConfigSO config, AccessoryCatalogSO catalog)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.catalog = catalog;
        }

        public event Action<string> OnWornChanged;

        public IReadOnlyList<AccessorySO> Accessories => catalog == null ? Array.Empty<AccessorySO>() : catalog.Accessories;

        public bool IsUnlocked(string catId, AccessorySO accessory)
        {
            CatSave cat = collection.GetSave(catId);

            if (cat == null || accessory == null)
            {
                return false;
            }

            return config.GetTierIndex(config.GetBondLevel(cat.bondXP)) >= accessory.RequiredTier;
        }

        public AccessorySO GetWorn(string catId)
        {
            CatSave cat = collection.GetSave(catId);
            AccessorySO worn = cat == null || catalog == null ? null : catalog.Find(cat.accessoryId);
            return worn != null && IsUnlocked(catId, worn) ? worn : null;
        }

        public bool Wear(string catId, AccessorySO accessory)
        {
            CatSave cat = collection.GetSave(catId);

            if (cat == null || (accessory != null && !IsUnlocked(catId, accessory)))
            {
                return false;
            }

            cat.accessoryId = accessory == null ? string.Empty : accessory.Id;
            store.Commit(0);
            OnWornChanged?.Invoke(catId);
            return true;
        }
    }
}
