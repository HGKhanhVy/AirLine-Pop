using System.Collections.Generic;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Builds a board block: a rounded slab of ground whose top edge is bevelled and whose
    /// walls run down into the sea, like a chunk of island.
    ///
    /// The top face lies on z = 0, where the path and every mark on a square are drawn, so
    /// the 2D overlays stay exactly on the block. The walls go to z = +height, away from
    /// the camera; the tilted camera sees the near ones.
    ///
    /// Vertex alpha marks what is ground cover and what is earth: the top and bevel carry
    /// 0, so they take the palette colour, and the walls switch to 1 just under a thin lip
    /// of cover, so they take the material's wall colour. The walls also darken towards
    /// the foot, a cheap stand-in for the shade where the block meets the water.
    ///
    /// Normals are smooth around the outline and across the bevel, so the light rolls over
    /// the rounded edge instead of breaking into facets. The bottom is left open: it is
    /// never seen.
    /// </summary>
    public static class RoundedSlabMesh
    {
        private const int CornerSegments = 6;
        private const int BevelSegments = 4;

        // How far the ground cover hangs over the top of the wall before the earth shows,
        // as a share of the wall below the bevel.
        private const float LipShare = 0.18f;

        // The earth starts this much below the lip, which makes a crisp edge rather than a smear.
        private const float LipEdge = 0.012f;

        // Brightness at the foot of the wall.
        private const float FootShade = 0.55f;

        // Brightness at the edge of the flat top and at the bottom of the bevel. The middle
        // of the top is 1, so the top reads as a gentle dome of cover rather than flat paint.
        private const float TopEdgeShade = 0.95f;
        private const float BevelShade = 0.8f;

        private readonly struct Ring
        {
            public Ring(float inset, float depth, float upness, float wallness, float shade)
            {
                Inset = inset;
                Depth = depth;
                Upness = upness;
                Wallness = wallness;
                Shade = shade;
            }

            public float Inset { get; }
            public float Depth { get; }
            public float Upness { get; }
            public float Wallness { get; }
            public float Shade { get; }
        }

        public static Mesh Build(Vector2 size, float height, float cornerRadius, float bevel)
        {
            bevel = Mathf.Min(bevel, cornerRadius * 0.9f);

            var vertices = new List<Vector3>(512);
            var normals = new List<Vector3>(512);
            var colors = new List<Color>(512);
            var triangles = new List<int>(2048);

            List<Vector2> directions = OutlineDirections();
            Vector2 half = size * 0.5f;
            Vector2 cornerCentre = half - new Vector2(cornerRadius, cornerRadius);
            int ringSize = directions.Count;
            List<Ring> profile = Profile(height, bevel);

            foreach (Ring ring in profile)
            {
                float sideness = Mathf.Sqrt(Mathf.Max(0f, 1f - ring.Upness * ring.Upness));
                var color = new Color(ring.Shade, ring.Shade, ring.Shade, ring.Wallness);

                foreach (Vector2 direction in directions)
                {
                    Vector2 corner = new Vector2(
                        Mathf.Sign(direction.x) * cornerCentre.x,
                        Mathf.Sign(direction.y) * cornerCentre.y);
                    Vector2 point = corner + direction * (cornerRadius - ring.Inset);
                    vertices.Add(new Vector3(point.x, point.y, ring.Depth));
                    normals.Add(new Vector3(direction.x * sideness, direction.y * sideness, -ring.Upness).normalized);
                    colors.Add(color);
                }
            }

            for (int ring = 0; ring < profile.Count - 1; ring++)
            {
                int a = ring * ringSize;
                int b = a + ringSize;

                for (int k = 0; k < ringSize; k++)
                {
                    int next = (k + 1) % ringSize;
                    AddFacing(triangles, vertices, normals, a + k, a + next, b + next);
                    AddFacing(triangles, vertices, normals, a + k, b + next, b + k);
                }
            }

            int centre = vertices.Count;
            vertices.Add(Vector3.zero);
            normals.Add(new Vector3(0f, 0f, -1f));
            colors.Add(new Color(1f, 1f, 1f, 0f));

            for (int k = 0; k < ringSize; k++)
            {
                AddFacing(triangles, vertices, normals, centre, k, (k + 1) % ringSize);
            }

            var mesh = new Mesh { name = "Rounded Slab" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Rings from the edge of the flat top, round the bevel and down the lip of ground
        /// cover, then from the top of the earth to the foot of the wall.
        /// </summary>
        private static List<Ring> Profile(float height, float bevel)
        {
            var profile = new List<Ring>(BevelSegments + 4);

            for (int i = 0; i <= BevelSegments; i++)
            {
                float angle = i / (float)BevelSegments * Mathf.PI * 0.5f;
                // The cover darkens a touch as it rolls over the edge, which rounds the top.
                float shade = Mathf.Lerp(TopEdgeShade, BevelShade, i / (float)BevelSegments);
                profile.Add(new Ring(bevel * (1f - Mathf.Sin(angle)), bevel * (1f - Mathf.Cos(angle)), Mathf.Cos(angle), 0f, shade));
            }

            float lip = bevel + (height - bevel) * LipShare;
            profile.Add(new Ring(0f, lip, 0f, 0f, BevelShade * 0.92f));
            profile.Add(new Ring(0f, Mathf.Min(height, lip + LipEdge), 0f, 1f, 1f));
            profile.Add(new Ring(0f, height, 0f, 1f, FootShade));
            return profile;
        }

        /// <summary>
        /// Outward directions round the outline: a quarter circle per corner, counter-clockwise
        /// from the right. The straight sides fall between the end of one corner and the
        /// start of the next.
        /// </summary>
        private static List<Vector2> OutlineDirections()
        {
            var directions = new List<Vector2>(4 * (CornerSegments + 1));

            for (int corner = 0; corner < 4; corner++)
            {
                for (int i = 0; i <= CornerSegments; i++)
                {
                    float angle = (corner + i / (float)CornerSegments) * Mathf.PI * 0.5f;

                    // Nudge the ends inside their own quadrant so the sign picks the right corner.
                    float nudged = Mathf.Clamp(angle, corner * Mathf.PI * 0.5f + 1e-4f, (corner + 1) * Mathf.PI * 0.5f - 1e-4f);
                    directions.Add(new Vector2(Mathf.Cos(nudged), Mathf.Sin(nudged)));
                }
            }

            return directions;
        }

        /// <summary>Adds the triangle wound so it faces the same way as its vertex normals.</summary>
        private static void AddFacing(List<int> triangles, List<Vector3> vertices, List<Vector3> normals, int a, int b, int c)
        {
            Vector3 face = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);

            if (face.sqrMagnitude < 1e-12f)
            {
                return;
            }

            Vector3 smooth = normals[a] + normals[b] + normals[c];

            if (Vector3.Dot(face, smooth) < 0f)
            {
                (b, c) = (c, b);
            }

            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
        }
    }
}
