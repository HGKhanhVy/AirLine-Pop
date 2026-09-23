using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The two colour moves the board's looks are built from, kept in one place so the
    /// default theme and the skins cannot drift apart.
    /// </summary>
    public static class BoardHues
    {
        public static Color FromHue(float hue01, Vector2 saturationValue)
        {
            return Color.HSVToRGB(hue01, saturationValue.x, saturationValue.y);
        }

        /// <summary>Softens the drawn line so it sits on the squares instead of burning over them.</summary>
        public static Color Fade(Color colour, float alpha)
        {
            colour.a = alpha;
            return colour;
        }
    }
}
