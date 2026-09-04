using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Every colour and measurement the board is drawn with.
    ///
    /// All current art is placeholder, so nothing may reach for a sprite or a colour by
    /// name at runtime. Routing it through one asset means swapping the look later is a
    /// data change, and it is what lets several themes ship without touching gameplay.
    /// </summary>
    [CreateAssetMenu(menuName = "Single Line/Theme", fileName = "Theme")]
    public sealed class ThemeSO : ScriptableObject
    {
        [Header("Sprites")]
        [Tooltip("Leave empty to fall back to a plain generated square while art is missing.")]
        [SerializeField] private Sprite cellSprite;

        [Header("Colours")]
        [SerializeField] private Color background = new Color(0.04f, 0.05f, 0.07f);
        [SerializeField] private Color cell = new Color(0.24f, 0.28f, 0.34f);
        [SerializeField] private Color visited = new Color(0.11f, 0.44f, 0.49f);
        [SerializeField] private Color head = new Color(0.20f, 0.66f, 0.72f);
        [SerializeField] private Color start = new Color(0.85f, 0.22f, 0.25f);
        [SerializeField] private Color path = new Color(0.20f, 0.66f, 0.72f);
        [SerializeField] private Color won = new Color(0.29f, 0.72f, 0.44f);
        [SerializeField] private Color stuck = new Color(0.85f, 0.22f, 0.25f);

        [Header("Layout")]
        [SerializeField, Min(0.1f)] private float cellSize = 1f;
        [SerializeField, Min(0f)] private float cellSpacing = 0.12f;
        [SerializeField, Min(0.02f)] private float pathWidth = 0.3f;

        [Header("Camera")]
        [Tooltip("Board width kept clear of the screen edges, in cells.")]
        [SerializeField, Min(0f)] private float boardMargin = 0.5f;

        [Tooltip("Share of the usable height the board should fill. GDD 9.2 asks for 0.55 to 0.70.")]
        [SerializeField, Range(0.3f, 0.95f)] private float boardHeightFraction = 0.62f;

        public Sprite CellSprite => cellSprite;

        public Color Background => background;

        public Color Cell => cell;

        public Color Visited => visited;

        public Color Head => head;

        public Color Start => start;

        public Color Path => path;

        public Color Won => won;

        public Color Stuck => stuck;

        public float CellSize => cellSize;

        public float CellSpacing => cellSpacing;

        /// <summary>Distance from one cell centre to the next.</summary>
        public float CellPitch => cellSize + cellSpacing;

        public float PathWidth => pathWidth;

        public float BoardMargin => boardMargin;

        public float BoardHeightFraction => boardHeightFraction;
    }
}
