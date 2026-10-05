using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Keeps a box of UI inside an area, such as the safe area, with some room to the edges.
    /// </summary>
    public static class ScreenEdgeClamp
    {
        /// <summary>
        /// Moves a box, given by its lower-left and upper-right extents from a point, so the box
        /// fits inside the area, and returns the moved point. A box too big for the area is centred.
        /// </summary>
        public static Vector2 Inside(Vector2 point, Vector2 below, Vector2 above, Rect area, float room)
        {
            point.x = Fit(point.x, below.x, above.x, area.xMin + room, area.xMax - room);
            point.y = Fit(point.y, below.y, above.y, area.yMin + room, area.yMax - room);
            return point;
        }

        private static float Fit(float value, float below, float above, float min, float max)
        {
            float low = min - below;
            float high = max - above;

            if (low > high)
            {
                return (low + high) * 0.5f;
            }

            return Mathf.Clamp(value, low, high);
        }
    }
}
