using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// One board look the player can own and wear (GDD 7, 8.1). A skin only repaints:
    /// the squares, the line and the background change, the rules and the hitboxes do not.
    /// </summary>
    [CreateAssetMenu(menuName = "Single Line/Skin", fileName = "Skin")]
    public sealed class SkinSO : ScriptableObject
    {
        [Tooltip("Saved with the player's profile, so it must not change once it has shipped.")]
        [SerializeField] private string id = "default";

        [SerializeField] private string displayName = "Default";

        [Tooltip("Price in coins. GDD 8.1: 1,000 for a common look, 3,000 for a rare one.")]
        [SerializeField, Min(0)] private int price;

        [Tooltip("Off for a skin that cannot be bought with coins, such as one that only comes with the Starter Pack.")]
        [SerializeField] private bool isSoldInShop = true;

        [Tooltip("On for the look a brand new player already wears.")]
        [SerializeField] private bool isOwnedByDefault;

        [SerializeField] private BoardPaletteRecipe palette = new BoardPaletteRecipe();

        public string Id => id;

        public string DisplayName => displayName;

        public int Price => price;

        public bool IsSoldInShop => isSoldInShop;

        public bool IsOwnedByDefault => isOwnedByDefault;

        public Color Background => palette.Background;

        public BoardPalette GetPalette(int levelNumber)
        {
            return palette.GetPalette(levelNumber);
        }
    }
}
