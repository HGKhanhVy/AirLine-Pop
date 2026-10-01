using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Economy and care numbers from GDD 5 and 6. All are playtest starting points, so
    /// they live here rather than in code.
    /// </summary>
    [CreateAssetMenu(menuName = "AirLine Pop/Economy Config", fileName = "EconomyConfig")]
    public sealed class EconomyConfigSO : ScriptableObject
    {
        [Header("Coins")]
        [SerializeField, Min(0)] private int firstClearCoins = 80;

        [Header("Food")]
        [SerializeField, Min(0)] private int foodPrice = 40;
        [SerializeField, Min(0)] private int starterFood = 3;
        [SerializeField, Min(1)] private int maxFoodPerPurchase = 10;

        [Header("Care")]
        [SerializeField, Min(0)] private int petBond = 2;
        [SerializeField, Min(0)] private int playBond = 3;
        [SerializeField, Min(0)] private int mealBond = 10;
        [SerializeField, Min(0)] private int rewardedMealsPerDay = 3;

        [Tooltip("Cumulative bond needed for each level; the first entry is level 1.")]
        [SerializeField] private int[] bondLevelThresholds = { 0, 20, 60, 120, 200 };

        [Tooltip("How many loyalty cards there are, one per bond level; their names are tier.0, tier.1... in the localization table.")]
        [SerializeField, Min(1)] private int loyaltyTierCount = 5;

        [Header("Room")]
        [SerializeField, Range(1, 8)] private int maxVisibleCats = 4;

        public int FirstClearCoins => firstClearCoins;

        public int FoodPrice => foodPrice;

        public int StarterFood => starterFood;

        public int MaxFoodPerPurchase => maxFoodPerPurchase;

        public int PetBond => petBond;

        public int PlayBond => playBond;

        public int MealBond => mealBond;

        public int RewardedMealsPerDay => rewardedMealsPerDay;

        public int MaxVisibleCats => maxVisibleCats;

        public int BondLevelCount => bondLevelThresholds.Length;

        /// <summary>1-based bond level reached with <paramref name="bondXP"/>.</summary>
        public int GetBondLevel(int bondXP)
        {
            int level = 1;

            for (int i = 1; i < bondLevelThresholds.Length; i++)
            {
                if (bondXP >= bondLevelThresholds[i])
                {
                    level = i + 1;
                }
            }

            return level;
        }

        /// <summary>The loyalty card a bond level earns, such as "Silver", in the chosen language.</summary>
        public string GetLoyaltyTier(int bondLevel)
        {
            return Localization.Get("tier." + Mathf.Clamp(bondLevel - 1, 0, loyaltyTierCount - 1));
        }

        public float GetBondProgress(int bondXP)
        {
            int level = GetBondLevel(bondXP);

            if (level >= bondLevelThresholds.Length)
            {
                return 1f;
            }

            int from = bondLevelThresholds[level - 1];
            int to = bondLevelThresholds[level];
            return Mathf.Clamp01((bondXP - from) / (float)(to - from));
        }
    }
}
