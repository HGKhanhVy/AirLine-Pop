using System;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>Food stock and the three care actions (GDD 5).</summary>
    public interface ICatCareService
    {
        event Action OnCareChanged;

        int FoodCount { get; }

        int FoodPrice { get; }

        PurchaseOutcome TryBuyFood(int quantity);

        /// <summary>Buys a shop bundle at its own price, which is cheaper per snack than one by one.</summary>
        PurchaseOutcome TryBuyFoodPack(SnackPack pack);

        CareOutcome Pet(string catId);

        CareOutcome Play(string catId);

        /// <summary>Checks stock and today's limit, then spends the food and grants bond in one save.</summary>
        CareOutcome Feed(string catId);

        bool CanFeed(string catId);

        int GetBondLevel(string catId);

        float GetBondProgress(string catId);
    }
}
