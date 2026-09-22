using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Store numbers and text that are data, not code. Fallback prices are only shown
    /// while the store has not answered with the real, localized ones.
    /// </summary>
    [CreateAssetMenu(menuName = "Single Line/Store Config", fileName = "StoreConfig")]
    public sealed class StoreConfigSO : ScriptableObject
    {
        [Header("Remove Ads")]
        [SerializeField] private string removeAdsFallbackPrice = "$1.99";

        [Header("Starter Pack")]
        [SerializeField] private string starterPackFallbackPrice = "$3.99";
        [SerializeField, Min(0)] private int starterPackCoins = 1500;

        [Tooltip("Theme the Starter Pack unlocks. Not sold on its own, which is what makes it exclusive.")]
        [SerializeField] private string starterPackThemeId = "starter";

        public string RemoveAdsFallbackPrice => removeAdsFallbackPrice;

        public string StarterPackFallbackPrice => starterPackFallbackPrice;

        public int StarterPackCoins => starterPackCoins;

        public string StarterPackThemeId => starterPackThemeId;

        public string GetFallbackPrice(StoreProduct product)
        {
            return product == StoreProduct.StarterPack ? starterPackFallbackPrice : removeAdsFallbackPrice;
        }
    }
}
