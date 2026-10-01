using System.Collections.Generic;
using UnityEngine;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Builds a white passenger jet seen from above, sized to a wingspan of 1: a long
    /// round fuselage with a dark windscreen band near the nose, swept wings with red tips,
    /// a red engine slung under each wing, a swept tailplane and a fin with a red top.
    ///
    /// Axes: the nose points along +Y, the right wing along +X, and the top of the
    /// airplane faces the camera, which is -Z on this board.
    ///
    /// Every triangle has its own vertices so each face shades flat under the board light.
    /// </summary>
    public static class AirplaneMeshBuilder
    {
        /// <summary>The middle of the flight deck, behind the windscreen.</summary>
        public static readonly Vector3 PilotSeat = new Vector3(0f, 0.36f, -0.05f);

        private const int RingSides = 16;

        public static Mesh Build(AirplanePalette palette)
        {
            var builder = new Builder();
            AddFuselage(builder, palette);
            AddWings(builder, palette);
            AddEngines(builder, palette);
            AddTail(builder, palette);
            builder.Finish(out List<Vector3> vertices, out List<Color> colors, out List<int> triangles);
            return ToMesh("Airplane", vertices, colors, triangles);
        }

        private static void AddFuselage(Builder builder, AirplanePalette palette)
        {
            // y, half width, half height, centre z. The tail cone lifts towards the camera.
            var body = new[]
            {
                new Vector4(0.52f, 0f, 0f, 0f),
                new Vector4(0.5f, 0.03f, 0.028f, 0f),
                new Vector4(0.46f, 0.052f, 0.05f, 0f),
                new Vector4(0.41f, 0.066f, 0.064f, 0f),
                new Vector4(0.36f, 0.072f, 0.07f, 0f),
                new Vector4(0.1f, 0.075f, 0.072f, 0f),
                new Vector4(-0.25f, 0.075f, 0.072f, 0f),
                new Vector4(-0.38f, 0.055f, 0.058f, -0.01f),
                new Vector4(-0.47f, 0.025f, 0.035f, -0.025f),
                new Vector4(-0.5f, 0f, 0f, -0.03f)
            };

            Color b = palette.Body;
            Color g = palette.Glass;
            builder.AddLoft(body, new[] { b, b, g, b, b, b, b, b, b });
        }

        /// <summary>Swept wings, each a thin slab from the root to a red tip.</summary>
        private static void AddWings(Builder builder, AirplanePalette palette)
        {
            var thickness = new Vector3(0f, 0f, 0.022f);

            for (int side = -1; side <= 1; side += 2)
            {
                var wing = new[]
                {
                    new Vector3(0.06f * side, 0.12f, 0.01f),
                    new Vector3(0.44f * side, -0.07f, 0.01f),
                    new Vector3(0.44f * side, -0.14f, 0.01f),
                    new Vector3(0.06f * side, -0.1f, 0.01f)
                };
                builder.AddPrism(wing, thickness, palette.Wing);

                var tip = new[]
                {
                    new Vector3(0.44f * side, -0.07f, 0.008f),
                    new Vector3(0.5f * side, -0.1f, 0.008f),
                    new Vector3(0.5f * side, -0.15f, 0.008f),
                    new Vector3(0.44f * side, -0.14f, 0.008f)
                };
                builder.AddPrism(tip, thickness, palette.Accent);
            }
        }

        /// <summary>A round engine pod under each wing, a little ahead of it.</summary>
        private static void AddEngines(Builder builder, AirplanePalette palette)
        {
            var pod = new[]
            {
                new Vector4(0.12f, 0.035f, 0.035f, 0.05f),
                new Vector4(0.1f, 0.045f, 0.045f, 0.05f),
                new Vector4(-0.04f, 0.042f, 0.042f, 0.05f),
                new Vector4(-0.08f, 0.02f, 0.02f, 0.05f)
            };

            Color e = palette.Engine;

            for (int side = -1; side <= 1; side += 2)
            {
                builder.AddLoft(pod, new[] { e, e, e }, capStart: true, offsetX: 0.21f * side);
            }
        }

        private static void AddTail(Builder builder, AirplanePalette palette)
        {
            var thickness = new Vector3(0f, 0f, 0.016f);

            for (int side = -1; side <= 1; side += 2)
            {
                var stabiliser = new[]
                {
                    new Vector3(0.03f * side, -0.34f, -0.02f),
                    new Vector3(0.17f * side, -0.43f, -0.02f),
                    new Vector3(0.17f * side, -0.47f, -0.02f),
                    new Vector3(0.03f * side, -0.45f, -0.02f)
                };
                builder.AddPrism(stabiliser, thickness, palette.Wing);
            }

            // The fin stands up towards the camera, swept back, with a red top.
            var fin = new[]
            {
                new Vector3(-0.012f, -0.3f, -0.06f),
                new Vector3(-0.012f, -0.44f, -0.2f),
                new Vector3(-0.012f, -0.5f, -0.2f),
                new Vector3(-0.012f, -0.49f, -0.04f)
            };
            builder.AddPrism(fin, new Vector3(0.024f, 0f, 0f), palette.Body);

            var finTop = new[]
            {
                new Vector3(-0.013f, -0.415f, -0.17f),
                new Vector3(-0.013f, -0.44f, -0.205f),
                new Vector3(-0.013f, -0.505f, -0.205f),
                new Vector3(-0.013f, -0.5f, -0.17f)
            };
            builder.AddPrism(finTop, new Vector3(0.026f, 0f, 0f), palette.Accent);
        }

        private static Mesh ToMesh(string name, List<Vector3> vertices, List<Color> colors, List<int> triangles)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private sealed class Builder
        {
            private readonly List<Vector3> vertices = new List<Vector3>(4096);
            private readonly List<Color> colors = new List<Color>(4096);

            /// <summary>
            /// A tube through elliptical rings, one colour per segment. Stations are
            /// (y, half width, half height, centre z); a ring of zero width closes the tube
            /// to a point, and an open first ring can be capped flat.
            /// </summary>
            public void AddLoft(Vector4[] stations, Color[] segmentColors, bool capStart = false, float offsetX = 0f)
            {
                for (int i = 0; i < stations.Length - 1; i++)
                {
                    Vector4 a = stations[i];
                    Vector4 b = stations[i + 1];
                    var inside = new Vector3(offsetX, (a.x + b.x) * 0.5f, (a.w + b.w) * 0.5f);

                    for (int k = 0; k < RingSides; k++)
                    {
                        Vector3 a0 = Ring(a, k, offsetX);
                        Vector3 a1 = Ring(a, k + 1, offsetX);
                        Vector3 b0 = Ring(b, k, offsetX);
                        Vector3 b1 = Ring(b, k + 1, offsetX);

                        AddTriangle(a0, a1, b0, segmentColors[i], inside);
                        AddTriangle(a1, b1, b0, segmentColors[i], inside);
                    }
                }

                Vector4 first = stations[0];

                if (capStart && first.y > 0f)
                {
                    var centre = new Vector3(offsetX, first.x, first.w);
                    var inside = new Vector3(offsetX, stations[1].x, first.w);

                    for (int k = 0; k < RingSides; k++)
                    {
                        AddTriangle(centre, Ring(first, k, offsetX), Ring(first, k + 1, offsetX), segmentColors[0] * 0.55f, inside);
                    }
                }
            }

            private static Vector3 Ring(Vector4 station, int k, float offsetX)
            {
                float angle = (k + 0.5f) * (2f * Mathf.PI / RingSides);
                return new Vector3(
                    offsetX + station.y * Mathf.Cos(angle),
                    station.x,
                    station.w + station.z * Mathf.Sin(angle));
            }

            /// <summary>A convex outline pushed out along <paramref name="extrude"/> into a slab.</summary>
            public void AddPrism(Vector3[] outline, Vector3 extrude, Color color)
            {
                Vector3 centre = Vector3.zero;

                for (int i = 0; i < outline.Length; i++)
                {
                    centre += outline[i];
                }

                Vector3 inside = centre / outline.Length + extrude * 0.5f;

                for (int i = 1; i < outline.Length - 1; i++)
                {
                    AddTriangle(outline[0], outline[i], outline[i + 1], color, inside);
                    AddTriangle(outline[0] + extrude, outline[i] + extrude, outline[i + 1] + extrude, color, inside);
                }

                for (int i = 0; i < outline.Length; i++)
                {
                    Vector3 a = outline[i];
                    Vector3 b = outline[(i + 1) % outline.Length];
                    AddTriangle(a, b, a + extrude, color, inside);
                    AddTriangle(b, b + extrude, a + extrude, color, inside);
                }
            }

            /// <summary>
            /// Winds the triangle so it faces away from <paramref name="inside"/>, which is
            /// enough for the convex parts this model is built from.
            /// </summary>
            private void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Color color, Vector3 inside)
            {
                Vector3 normal = Vector3.Cross(b - a, c - a);

                if (normal.sqrMagnitude < 1e-12f)
                {
                    return;
                }

                if (Vector3.Dot(normal, (a + b + c) / 3f - inside) < 0f)
                {
                    (b, c) = (c, b);
                }

                var albedo = new Color(color.r, color.g, color.b, 1f);
                vertices.Add(a);
                vertices.Add(b);
                vertices.Add(c);
                colors.Add(albedo);
                colors.Add(albedo);
                colors.Add(albedo);
            }

            public void Finish(out List<Vector3> finalVertices, out List<Color> finalColors, out List<int> triangles)
            {
                triangles = new List<int>(vertices.Count);

                for (int i = 0; i < vertices.Count; i++)
                {
                    triangles.Add(i);
                }

                finalVertices = vertices;
                finalColors = colors;
            }
        }
    }
}
