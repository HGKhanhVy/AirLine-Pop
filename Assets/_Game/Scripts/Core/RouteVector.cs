using System;

namespace ASTeams.SingleLine.Core
{
    /// <summary>A point on the route map, in map units: X across, Y up from the first level.</summary>
    public readonly struct RouteVector
    {
        public RouteVector(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float X { get; }

        public float Y { get; }

        public static float Distance(RouteVector a, RouteVector b)
        {
            float dx = b.X - a.X;
            float dy = b.Y - a.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public static RouteVector Lerp(RouteVector a, RouteVector b, float t)
        {
            return new RouteVector(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
        }
    }
}
