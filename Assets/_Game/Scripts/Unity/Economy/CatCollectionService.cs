using System;
using System.Collections.Generic;
using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Unity
{
    public sealed class CatCollectionService : ICatCollectionService
    {
        private readonly ICollectionStore store;
        private readonly CatCatalogSO catalog;
        private readonly EconomyConfigSO config;

        public CatCollectionService(ICollectionStore store, CatCatalogSO catalog, EconomyConfigSO config)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public event Action OnCollectionChanged;

        public IReadOnlyList<CatBreedSO> Breeds => catalog.Breeds;

        public IReadOnlyList<string> VisibleCatIds => store.State.visibleRoomCatIds;

        public string CompanionCatId => store.State.companionCatId;

        public bool IsOwned(CatBreedSO breed)
        {
            return breed != null && GetSave(breed.Id) != null;
        }

        public CatSave GetSave(string catId)
        {
            List<CatSave> cats = store.State.cats;

            for (int i = 0; i < cats.Count; i++)
            {
                if (cats[i].catId == catId)
                {
                    return cats[i];
                }
            }

            return null;
        }

        public IReadOnlyList<CatBreedSO> WelcomeArrivals(int nextFlight)
        {
            List<CatBreedSO> arrived = null;
            CollectionSave state = store.State;

            foreach (CatBreedSO breed in catalog.Breeds)
            {
                if (breed == null || IsOwned(breed) || !RegularArrivals.IsDue(breed.ArrivesAfterFlight, nextFlight))
                {
                    continue;
                }

                state.cats.Add(new CatSave { catId = breed.Id, name = breed.DisplayName });

                // A new regular walks into the lounge when there is a seat for it.
                if (state.visibleRoomCatIds.Count < config.MaxVisibleCats)
                {
                    state.visibleRoomCatIds.Add(breed.Id);
                }

                arrived ??= new List<CatBreedSO>();
                arrived.Add(breed);
            }

            if (arrived == null)
            {
                return Array.Empty<CatBreedSO>();
            }

            store.Commit(0);
            OnCollectionChanged?.Invoke();
            return arrived;
        }

        public bool SetVisible(string catId, bool isVisible)
        {
            List<string> visible = store.State.visibleRoomCatIds;
            bool isShown = visible.Contains(catId);

            if (GetSave(catId) == null || isShown == isVisible)
            {
                return isShown == isVisible;
            }

            if (isVisible && visible.Count >= config.MaxVisibleCats)
            {
                return false;
            }

            if (isVisible)
            {
                visible.Add(catId);
            }
            else
            {
                visible.Remove(catId);
            }

            store.Commit(0);
            OnCollectionChanged?.Invoke();
            return true;
        }

        public void SetCompanion(string catId)
        {
            if (GetSave(catId) == null || store.State.companionCatId == catId)
            {
                return;
            }

            store.State.companionCatId = catId;
            store.Commit(0);
            OnCollectionChanged?.Invoke();
        }
    }
}
