using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// One look the player can own and wear, sold as a theme in the shop (GDD 7, 8.1). A theme
    /// only repaints: the sky and the tiles behind the board, the start pad, and the route
    /// map's sky and gate signs. The rules and the hitboxes never change.
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

        [Header("Art")]
        [Tooltip("The gradient stretched behind the board.")]
        [SerializeField] private Sprite boardSky;

        [Tooltip("The gradient behind the route map.")]
        [SerializeField] private Sprite mapSky;

        [SerializeField] private Sprite gateFlown;
        [SerializeField] private Sprite gateCurrent;
        [SerializeField] private Sprite gateLocked;

        [Tooltip("The picture on the theme's card in the shop.")]
        [SerializeField] private Sprite preview;

        [Header("Runway")]
        [Tooltip("The runway's tarmac with its edge lamps, tiled along the path.")]
        [SerializeField] private Material runwaySurface;

        [Tooltip("The runway's centre marks: dashes, flowers or stars.")]
        [SerializeField] private Material runwayMarks;

        [Tooltip("Tint for the clouds behind the board and on the route map.")]
        [SerializeField] private Color cloudTint = Color.white;

        public string Id => id;

        public string DisplayName => Localization.Get("theme." + id);

        public int Price => price;

        public bool IsSoldInShop => isSoldInShop;

        public bool IsOwnedByDefault => isOwnedByDefault;

        public Color Background => palette.Background;

        public Sprite BoardSky => boardSky;

        public Sprite MapSky => mapSky;

        public Sprite GateFlown => gateFlown;

        public Sprite GateCurrent => gateCurrent;

        public Sprite GateLocked => gateLocked;

        public Sprite Preview => preview;

        public Material RunwaySurface => runwaySurface;

        public Material RunwayMarks => runwayMarks;

        public Color CloudTint => cloudTint;

        public BoardPalette GetPalette(int levelNumber)
        {
            return palette.GetPalette(levelNumber);
        }

#if UNITY_EDITOR
        public void EditorConfigureTheme(int themePrice, bool isStarter, Sprite sky, Sprite map, Sprite flown, Sprite current,
            Sprite locked, Sprite card)
        {
            price = themePrice;
            isOwnedByDefault = isStarter;
            isSoldInShop = !isStarter;
            boardSky = sky;
            mapSky = map;
            gateFlown = flown;
            gateCurrent = current;
            gateLocked = locked;
            preview = card;
        }

        public void EditorConfigureRunway(Material surface, Material marks, Color clouds)
        {
            runwaySurface = surface;
            runwayMarks = marks;
            cloudTint = clouds;
        }
#endif
    }
}
