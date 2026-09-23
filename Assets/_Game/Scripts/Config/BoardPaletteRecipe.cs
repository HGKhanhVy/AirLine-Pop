using System;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// How one look paints the board: the colours a square, the line and the background
    /// take, and whether they turn with the level the way the reference game does.
    ///
    /// Shared by the default theme and by every skin, so a skin can change the whole look
    /// without a second copy of the colour rules.
    /// </summary>
    [Serializable]
    public sealed class BoardPaletteRecipe
    {
        [SerializeField] private Color background = new Color(0f, 0.016f, 0.06f);

        [Tooltip("Squares nobody has touched. Constant across levels on purpose: an untouched square should read the same everywhere.")]
        [SerializeField] private Color cell = new Color(0.27f, 0.31f, 0.38f);

        [SerializeField] private Color stuck = new Color(0.85f, 0.22f, 0.25f);

        [Header("Level hue")]
        [Tooltip("Turn the hue on with every level. Off means the fixed tones below are used as they are.")]
        [SerializeField] private bool usesLevelHue = true;

        [SerializeField, Range(0f, 360f)] private float firstLevelHue = 65f;
        [SerializeField, Range(1f, 90f)] private float hueStepPerLevel = 22f;

        [Header("Roles (saturation, value)")]
        [SerializeField] private Vector2 startTone = new Vector2(0.82f, 0.82f);
        [SerializeField] private Vector2 visitedTone = new Vector2(0.73f, 0.87f);
        [SerializeField] private Vector2 headTone = new Vector2(0.42f, 0.98f);
        [SerializeField] private Vector2 pathTone = new Vector2(0.72f, 0.99f);
        [SerializeField] private Vector2 wonTone = new Vector2(0.35f, 1f);

        [SerializeField, Range(0.2f, 1f)] private float pathAlpha = 0.78f;

        public Color Background => background;

        public BoardPalette GetPalette(int levelNumber)
        {
            float hue = usesLevelHue
                ? Mathf.Repeat(firstLevelHue + (levelNumber - 1) * hueStepPerLevel, 360f) / 360f
                : firstLevelHue / 360f;

            return new BoardPalette(
                cell,
                BoardHues.FromHue(hue, startTone),
                BoardHues.FromHue(hue, visitedTone),
                BoardHues.FromHue(hue, headTone),
                BoardHues.Fade(BoardHues.FromHue(hue, pathTone), pathAlpha),
                BoardHues.FromHue(hue, wonTone),
                stuck);
        }
    }
}
