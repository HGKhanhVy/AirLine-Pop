using System.Collections.Generic;
using ASTeams.SingleLine.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Draws the flight route: a solid trail where the plane has flown, a faint dashed line
    /// ahead of it. The route runs far beyond the screen, so only a window around
    /// what is visible is meshed, and the mesh is rebuilt only when scrolling carries the
    /// view out of that window.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RouteLineGraphic : MaskableGraphic
    {
        [SerializeField, Min(1f)] private float thickness = 9f;
        [SerializeField, Min(1f)] private float dashLength = 26f;
        [SerializeField, Min(1f)] private float gapLength = 20f;
        [SerializeField] private Color flown = Color.white;
        [SerializeField] private Color ahead = new Color(1f, 1f, 1f, 0.55f);

        [Tooltip("Extra route meshed above and below the screen, so a short scroll needs no rebuild.")]
        [SerializeField, Min(0f)] private float windowMargin = 900f;

        private RouteLayout layout;
        private float offsetX;
        private float offsetY;
        private float flownDistance;
        private float windowLow = float.MaxValue;
        private float windowHigh = float.MinValue;

        /// <param name="mapToLocal">Added to every map point to place it in this graphic's space.</param>
        public void SetRoute(RouteLayout route, Vector2 mapToLocal, float flownTo)
        {
            layout = route;
            offsetX = mapToLocal.x;
            offsetY = mapToLocal.y;
            flownDistance = flownTo;
            windowLow = float.MaxValue;
            windowHigh = float.MinValue;
            SetVerticesDirty();
        }

        public void SetFlown(float flownTo)
        {
            if (!Mathf.Approximately(flownTo, flownDistance))
            {
                flownDistance = flownTo;
                SetVerticesDirty();
            }
        }

        /// <summary>Tells the line which stretch of the map is on screen, in map units.</summary>
        public void ShowRange(float low, float high)
        {
            if (low >= windowLow && high <= windowHigh)
            {
                return;
            }

            windowLow = low - windowMargin;
            windowHigh = high + windowMargin;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (layout == null || layout.Curve.Count < 2)
            {
                return;
            }

            IReadOnlyList<RouteVector> points = layout.Curve;
            IReadOnlyList<float> distances = layout.CurveDistance;
            float period = dashLength + gapLength;

            for (int i = layout.FirstCurveIndexAbove(windowLow); i < points.Count - 1; i++)
            {
                RouteVector a = points[i];

                if (a.Y > windowHigh)
                {
                    break;
                }

                RouteVector b = points[i + 1];
                float start = distances[i];
                float end = distances[i + 1];
                float span = end - start;

                if (span <= 0f)
                {
                    continue;
                }

                // Flown: one solid trail. Ahead: dashes, the route still to fly.
                if (start < flownDistance)
                {
                    float solidEnd = Mathf.Min(end, flownDistance);
                    AddQuad(vh, a, RouteVector.Lerp(a, b, (solidEnd - start) / span), flown);

                    if (solidEnd >= end)
                    {
                        continue;
                    }

                    a = RouteVector.Lerp(a, b, (solidEnd - start) / span);
                    start = solidEnd;
                    span = end - start;
                }

                float dash = Mathf.Floor(start / period) * period;

                for (; dash < end; dash += period)
                {
                    float from = Mathf.Max(start, dash);
                    float to = Mathf.Min(end, dash + dashLength);

                    if (to <= from)
                    {
                        continue;
                    }

                    RouteVector p = RouteVector.Lerp(a, b, (from - start) / span);
                    RouteVector q = RouteVector.Lerp(a, b, (to - start) / span);
                    AddQuad(vh, p, q, ahead);
                }
            }
        }

        private void AddQuad(VertexHelper vh, RouteVector p, RouteVector q, Color tint)
        {
            var from = new Vector2(p.X + offsetX, p.Y + offsetY);
            var to = new Vector2(q.X + offsetX, q.Y + offsetY);
            Vector2 along = to - from;

            if (along.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Vector2 side = new Vector2(-along.y, along.x).normalized * (thickness * 0.5f);
            Color32 colour = tint * color;
            int index = vh.currentVertCount;
            vh.AddVert(from - side, colour, Vector2.zero);
            vh.AddVert(from + side, colour, Vector2.zero);
            vh.AddVert(to + side, colour, Vector2.zero);
            vh.AddVert(to - side, colour, Vector2.zero);
            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index + 2, index + 3, index);
        }
    }
}
