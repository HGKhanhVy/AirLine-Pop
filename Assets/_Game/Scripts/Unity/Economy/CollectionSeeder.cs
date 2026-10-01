namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Gives a new save what GDD 6 promises on day one: the free starter cat, placed in
    /// the room and chosen as companion, and the welcome food.
    /// </summary>
    public static class CollectionSeeder
    {
        public static void EnsureSeeded(ICollectionStore store, CatCatalogSO catalog, EconomyConfigSO config)
        {
            CollectionSave state = store.State;

            if (state.isSeeded)
            {
                return;
            }

            foreach (CatBreedSO breed in catalog.Breeds)
            {
                if (breed != null && breed.IsOwnedByDefault)
                {
                    state.cats.Add(new CatSave { catId = breed.Id, name = breed.DisplayName });
                    state.visibleRoomCatIds.Add(breed.Id);

                    if (string.IsNullOrEmpty(state.companionCatId))
                    {
                        state.companionCatId = breed.Id;
                    }
                }
            }

            state.foodCount += config.StarterFood;
            state.isSeeded = true;
            store.Commit(0);
        }
    }
}
