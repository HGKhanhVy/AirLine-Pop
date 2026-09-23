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
        [Tooltip("Assign the board cell sprite from the project assets.")]
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

        [Header("Level hue")]
        [Tooltip("Derive the board colours from a hue that moves on with every level, the " +
                 "way the reference game does. Turn off to use the fixed colours above.")]
        [SerializeField] private bool usesLevelHue = true;

        [Tooltip("Hue of level 1, in degrees. Measured off the reference game: 65 is the " +
                 "yellow green it opens on.")]
        [SerializeField, Range(0f, 360f)] private float firstLevelHue = 65f;

        [Tooltip("How far the hue turns per level. 22 degrees takes about sixteen levels " +
                 "to come back around, which is the rhythm the reference plays at.")]
        [SerializeField, Range(1f, 90f)] private float hueStepPerLevel = 22f;

        [Tooltip("Saturation and value of each role, sampled from the reference game.")]
        [SerializeField] private Vector2 startTone = new Vector2(0.82f, 0.82f);
        [SerializeField] private Vector2 visitedTone = new Vector2(0.73f, 0.87f);
        [SerializeField] private Vector2 headTone = new Vector2(0.42f, 0.98f);
        [SerializeField] private Vector2 pathTone = new Vector2(0.72f, 0.99f);
        [SerializeField] private Vector2 wonTone = new Vector2(0.35f, 1f);

        [Tooltip("How solid the drawn line is over the squares it crosses. The line " +
                 "material adds light rather than painting over it, so this is the only " +
                 "knob that makes the pipe read softer without changing its colour.")]
        [SerializeField, Range(0.2f, 1f)] private float pathAlpha = 0.78f;

        [Header("Start cue")]
        [Tooltip("The dot in the middle of the start block.")]
        [SerializeField] private Color startDot = Color.white;

        [Tooltip("The ring that keeps swelling out of the dot. Alpha is driven by the cell, not read from here.")]
        [SerializeField] private Color startHalo = new Color(0.78f, 0.80f, 0.84f);

        [Header("Layout")]
        [SerializeField, Min(0.1f)] private float cellSize = 1f;
        [SerializeField, Min(0f)] private float cellSpacing = 0.12f;
        [SerializeField, Min(0.02f)] private float pathWidth = 0.3f;

        [Header("Camera")]
        [Tooltip("Board width kept clear of the screen edges, in cells.")]
        [SerializeField, Min(0f)] private float boardMargin = 0.5f;

        [Tooltip("Share of the usable height the board should fill. GDD 9.2 asks for 0.55 to 0.70.")]
        [SerializeField, Range(0.3f, 0.95f)] private float boardHeightFraction = 0.62f;

        [Tooltip("Largest a single cell may appear, as a share of screen width. Stops small boards blowing up.")]
        [SerializeField, Range(0.08f, 0.40f)] private float maxCellWidthFraction = 0.2f;

        public Sprite CellSprite => cellSprite;

        public Color Background => background;

        public Color Cell => cell;

        public Color Visited => visited;

        public Color Head => head;

        public Color Start => start;

        public Color Path => path;

        public Color Won => won;

        public Color Stuck => stuck;

        public Color StartDot => startDot;

        public Color StartHalo => startHalo;

        /// <summary>
        /// The colours for one level. Unvisited squares and the two warning states keep
        /// the values set above: a square nobody has touched reads the same on every
        /// level, and a warning that blended into the level's own hue would not warn.
        /// </summary>
        public BoardPalette GetPalette(int levelNumber)
        {
            if (!usesLevelHue)
            {
                return new BoardPalette(cell, start, visited, head, Fade(path), won, stuck);
            }

            float hue = Mathf.Repeat(firstLevelHue + (levelNumber - 1) * hueStepPerLevel, 360f) / 360f;

            return new BoardPalette(
                cell,
                FromHue(hue, startTone),
                FromHue(hue, visitedTone),
                FromHue(hue, headTone),
                Fade(FromHue(hue, pathTone)),
                FromHue(hue, wonTone),
                stuck);
        }

        private Color Fade(Color colour)
        {
            return BoardHues.Fade(colour, pathAlpha);
        }

        private static Color FromHue(float hue, Vector2 tone)
        {
            return BoardHues.FromHue(hue, tone);
        }

        public float CellSize => cellSize;

        public float CellSpacing => cellSpacing;

        /// <summary>Distance from one cell centre to the next.</summary>
        public float CellPitch => cellSize + cellSpacing;

        public float PathWidth => pathWidth;

        public float BoardMargin => boardMargin;

        public float BoardHeightFraction => boardHeightFraction;

        public float MaxCellWidthFraction => maxCellWidthFraction;
    }
}
