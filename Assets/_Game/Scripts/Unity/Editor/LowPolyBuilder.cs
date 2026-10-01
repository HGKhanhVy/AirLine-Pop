using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Collects simple vertex-coloured shapes into one mesh, so a whole set of scenery is
    /// a single draw call.
    ///
    /// Shapes stand on a ground plane at z = 0 with "up" towards the camera, which is -Z
    /// on this board. Positions are given as (x, y) on the ground plus a height, so callers
    /// never deal with the flipped axis. Every vertex carries its normal, and every
    /// triangle is wound to face along its normals.
    /// </summary>
    public sealed class LowPolyBuilder
    {
        private static readonly Vector3 Up = new Vector3(0f, 0f, -1f);

        private readonly List<Vector3> vertices = new List<Vector3>(8192);
        private readonly List<Vector3> normals = new List<Vector3>(8192);
        private readonly List<Color> colors = new List<Color>(8192);
        private readonly List<int> triangles = new List<int>(16384);
        private readonly List<int> facetedTriangles = new List<int>(16384);

        private Quaternion leanRotation = Quaternion.identity;
        private Vector3 leanPivot;
        private bool isLeaning;

        /// <summary>
        /// Height of the surface shapes currently stand on, such as the top of an island.
        /// Every height passed to the Add methods is measured from here.
        /// </summary>
        public float Elevation { get; set; }

        /// <summary>
        /// While set, shapes are flat shaded: every triangle takes its own face normal, so
        /// the facets catch the light one by one, the low poly look.
        /// </summary>
        public bool IsFaceted { get; set; }

        /// <summary>
        /// Tips the shapes added until <see cref="EndLean"/> away from the camera (+Y) about
        /// their foot at <paramref name="foot"/>, so they stand more across the camera's line
        /// of sight: a camera looking down from high up then sees their sides, as in a
        /// three-quarter view.
        /// </summary>
        public void BeginLean(Vector2 foot, float degrees)
        {
            leanPivot = Ground(foot, 0f);
            leanRotation = Quaternion.AngleAxis(degrees, Vector3.right);
            isLeaning = true;
        }

        public void EndLean()
        {
            isLeaning = false;
        }

        private Vector3 Ground(Vector2 position, float height)
        {
            return new Vector3(position.x, position.y, -(height + Elevation));
        }

        /// <summary>A flat rectangle lying on the ground, lifted by <paramref name="height"/>.</summary>
        public void AddGroundRect(Rect rect, float height, Color color)
        {
            AddQuad(
                Ground(new Vector2(rect.xMin, rect.yMin), height),
                Ground(new Vector2(rect.xMax, rect.yMin), height),
                Ground(new Vector2(rect.xMax, rect.yMax), height),
                Ground(new Vector2(rect.xMin, rect.yMax), height),
                Up, color);
        }

        /// <summary>A flat disc lying on the ground, such as a flower or a pond.</summary>
        public void AddGroundDisc(Vector2 centre, float radius, float height, int sides, Color color)
        {
            Vector3 middle = Ground(centre, height);

            for (int i = 0; i < sides; i++)
            {
                Vector3 a = Ground(centre + Circle(i, sides) * radius, height);
                Vector3 b = Ground(centre + Circle(i + 1, sides) * radius, height);
                AddTriangle(middle, a, b, Up, color);
            }
        }

        /// <summary>An upright box standing on the ground.</summary>
        public void AddBox(Vector2 centre, Vector2 size, float bottom, float top, Color sides, Color lid)
        {
            Vector2 half = size * 0.5f;
            var corners = new[]
            {
                centre + new Vector2(-half.x, -half.y),
                centre + new Vector2(half.x, -half.y),
                centre + new Vector2(half.x, half.y),
                centre + new Vector2(-half.x, half.y)
            };

            for (int i = 0; i < 4; i++)
            {
                Vector2 a = corners[i];
                Vector2 b = corners[(i + 1) % 4];
                Vector2 outward = ((a + b) * 0.5f - centre).normalized;
                AddQuad(Ground(a, bottom), Ground(b, bottom), Ground(b, top), Ground(a, top),
                    new Vector3(outward.x, outward.y, 0f), sides);
            }

            AddQuad(Ground(corners[0], top), Ground(corners[1], top), Ground(corners[2], top), Ground(corners[3], top), Up, lid);
        }

        /// <summary>
        /// A barrel roof: half a cylinder lying along X on top of a box, like a hangar.
        /// </summary>
        public void AddArchRoof(Vector2 centre, float length, float radius, float baseHeight, int segments, Color roof, Color ends)
        {
            float halfLength = length * 0.5f;

            for (int i = 0; i < segments; i++)
            {
                Vector2 a = Arch(i, segments);
                Vector2 b = Arch(i + 1, segments);
                var normalA = new Vector3(0f, a.x, -a.y);
                var normalB = new Vector3(0f, b.x, -b.y);

                int start = vertices.Count;
                AddVertex(ArchPoint(centre, -halfLength, a, radius, baseHeight), normalA, roof);
                AddVertex(ArchPoint(centre, halfLength, a, radius, baseHeight), normalA, roof);
                AddVertex(ArchPoint(centre, halfLength, b, radius, baseHeight), normalB, roof);
                AddVertex(ArchPoint(centre, -halfLength, b, radius, baseHeight), normalB, roof);
                AddFacing(start, start + 1, start + 2);
                AddFacing(start, start + 2, start + 3);

                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 hub = ArchPoint(centre, side * halfLength, Vector2.zero, radius, baseHeight);
                    AddTriangle(hub,
                        ArchPoint(centre, side * halfLength, a, radius, baseHeight),
                        ArchPoint(centre, side * halfLength, b, radius, baseHeight),
                        new Vector3(side, 0f, 0f), ends);
                }
            }
        }

        /// <summary>
        /// An upright frustum: a cylinder when both radii match, a cone when the top one is
        /// zero. Smooth sides, so it reads as round rather than faceted.
        /// </summary>
        public void AddFrustum(Vector2 centre, float bottom, float top, float bottomRadius, float topRadius,
            int sides, Color sideColor, Color lidColor)
        {
            float slope = (bottomRadius - topRadius) / Mathf.Max(0.001f, top - bottom);

            for (int i = 0; i < sides; i++)
            {
                Vector2 dirA = Circle(i, sides);
                Vector2 dirB = Circle(i + 1, sides);
                Vector3 normalA = SideNormal(dirA, slope);
                Vector3 normalB = SideNormal(dirB, slope);

                int start = vertices.Count;
                AddVertex(Ground(centre + dirA * bottomRadius, bottom), normalA, sideColor);
                AddVertex(Ground(centre + dirB * bottomRadius, bottom), normalB, sideColor);
                AddVertex(Ground(centre + dirB * topRadius, top), normalB, sideColor);
                AddVertex(Ground(centre + dirA * topRadius, top), normalA, sideColor);
                AddFacing(start, start + 1, start + 2);
                AddFacing(start, start + 2, start + 3);
            }

            if (topRadius > 0f)
            {
                AddGroundDisc(centre, topRadius, top, sides, lidColor);
            }
        }

        /// <summary>
        /// A smooth ellipsoid, or its upper half when <paramref name="isDome"/>: tree tops,
        /// bushes and hills are all made of these.
        /// </summary>
        public void AddBlob(Vector2 centre, float centreHeight, Vector3 radii, int segments, int rings,
            Color color, bool isDome = false)
        {
            int firstRing = isDome ? rings / 2 : 0;
            int start = vertices.Count;
            int stride = segments + 1;

            for (int ring = firstRing; ring <= rings; ring++)
            {
                // From the bottom pole (-90 degrees) to the top one (+90).
                float latitude = Mathf.Lerp(-90f, 90f, ring / (float)rings) * Mathf.Deg2Rad;
                float across = Mathf.Cos(latitude);
                float upness = Mathf.Sin(latitude);

                for (int segment = 0; segment <= segments; segment++)
                {
                    Vector2 direction = Circle(segment, segments);
                    Vector3 position = Ground(
                        centre + new Vector2(direction.x * across * radii.x, direction.y * across * radii.y),
                        centreHeight + upness * radii.z);

                    // An ellipsoid's normal divides by the radii rather than multiplying.
                    var normal = new Vector3(
                        direction.x * across / radii.x,
                        direction.y * across / radii.y,
                        -upness / radii.z).normalized;
                    AddVertex(position, normal, color);
                }
            }

            int ringCount = rings - firstRing;

            for (int ring = 0; ring < ringCount; ring++)
            {
                for (int segment = 0; segment < segments; segment++)
                {
                    int a = start + ring * stride + segment;
                    int d = a + stride;
                    AddFacing(a, a + 1, d + 1);
                    AddFacing(a, d + 1, d);
                }
            }
        }

        /// <summary>
        /// A round shape spun from a profile, like a potter's lathe: each point is
        /// (radius, height, shade) and the rings run from the bottom of the shape to its top.
        /// Normals follow the profile's slope, so the surface shades smoothly, and each ring
        /// takes its colour between <paramref name="dark"/> and <paramref name="light"/> by
        /// its shade, the way a painted tree darkens under each tier and brightens to the tip.
        /// </summary>
        public void AddLathe(Vector2 centre, IReadOnlyList<Vector3> profile, int sides, Color dark, Color light)
        {
            int start = vertices.Count;
            int stride = sides + 1;

            for (int ring = 0; ring < profile.Count; ring++)
            {
                Vector3 below = profile[Mathf.Max(0, ring - 1)];
                Vector3 above = profile[Mathf.Min(profile.Count - 1, ring + 1)];
                float slopeRadius = above.x - below.x;
                float slopeHeight = above.y - below.y;

                // The outward normal is the profile's tangent turned a quarter out.
                var normal2D = new Vector2(slopeHeight, -slopeRadius).normalized;
                Color color = Color.Lerp(dark, light, profile[ring].z);

                for (int side = 0; side <= sides; side++)
                {
                    Vector2 direction = Circle(side % sides, sides);
                    var normal = new Vector3(direction.x * normal2D.x, direction.y * normal2D.x, -normal2D.y);
                    AddVertex(Ground(centre + direction * profile[ring].x, profile[ring].y), normal, color);
                }
            }

            for (int ring = 0; ring < profile.Count - 1; ring++)
            {
                for (int side = 0; side < sides; side++)
                {
                    int a = start + ring * stride + side;
                    int d = a + stride;
                    AddFacing(a, a + 1, d + 1);
                    AddFacing(a, d + 1, d);
                }
            }
        }

        /// <summary>
        /// A round ellipsoid whose colour runs from <paramref name="under"/> on its underside
        /// to <paramref name="over"/> on top, like a tree crown or a cloud puff lit from above.
        /// </summary>
        public void AddShadedBlob(Vector2 centre, float centreHeight, float radius, float halfHeight,
            int sides, int rings, Color under, Color over)
        {
            var profile = new List<Vector3>(rings + 1);

            for (int i = 0; i <= rings; i++)
            {
                float latitude = Mathf.Lerp(-90f, 90f, i / (float)rings) * Mathf.Deg2Rad;
                float shade = Mathf.Clamp01(Mathf.Sin(latitude) * 0.6f + 0.5f);
                profile.Add(new Vector3(radius * Mathf.Cos(latitude), centreHeight + halfHeight * Mathf.Sin(latitude), shade));
            }

            AddLathe(centre, profile, sides, under, over);
        }

        /// <summary>
        /// A flat grid lying on the ground with a colour picked per vertex, so colour flows
        /// smoothly across it, like deep and shallow water.
        /// </summary>
        public void AddGroundGrid(Rect rect, int columns, int rows, float height, System.Func<Vector2, Color> colorAt)
        {
            int start = vertices.Count;

            for (int row = 0; row <= rows; row++)
            {
                for (int column = 0; column <= columns; column++)
                {
                    var point = new Vector2(
                        Mathf.Lerp(rect.xMin, rect.xMax, column / (float)columns),
                        Mathf.Lerp(rect.yMin, rect.yMax, row / (float)rows));
                    AddVertex(Ground(point, height), Up, colorAt(point));
                }
            }

            int stride = columns + 1;

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    int a = start + row * stride + column;
                    int d = a + stride;
                    AddFacing(a, a + 1, d + 1);
                    AddFacing(a, d + 1, d);
                }
            }
        }

        /// <summary>
        /// A flat-topped mound with a softly rounded rim, like a sandy plateau: walls rising
        /// from the ground, a bevel rolling over the edge, and a flat top. Smooth normals all
        /// round, so the light rolls over the rim instead of breaking on it.
        /// </summary>
        public void AddRoundedPlateau(Vector2 centre, float[] radii, float height, float bevel, Color wall, Color top)
        {
            const int bevelSegments = 4;
            int sides = radii.Length;

            // Rings from the foot of the wall, up the wall, round the bevel to the top edge.
            var profile = new List<(float inset, float height, float upness)>
            {
                (0f, 0f, 0f),
                (0f, height - bevel, 0f)
            };

            for (int i = 1; i <= bevelSegments; i++)
            {
                float angle = i / (float)bevelSegments * Mathf.PI * 0.5f;
                profile.Add((bevel * (1f - Mathf.Cos(angle)), height - bevel + bevel * Mathf.Sin(angle), Mathf.Sin(angle)));
            }

            int start = vertices.Count;
            int stride = sides + 1;

            for (int ring = 0; ring < profile.Count; ring++)
            {
                var (inset, ringHeight, upness) = profile[ring];
                float sideness = Mathf.Sqrt(Mathf.Max(0f, 1f - upness * upness));
                Color color = Color.Lerp(wall, top, Mathf.Clamp01(upness * 1.5f - 0.3f));

                for (int i = 0; i <= sides; i++)
                {
                    int side = i % sides;
                    Vector2 direction = Circle(side, sides);
                    Vector2 point = centre + direction * (radii[side] - inset);
                    var normal = new Vector3(direction.x * sideness, direction.y * sideness, -upness).normalized;
                    AddVertex(Ground(point, ringHeight), normal, color);
                }
            }

            for (int ring = 0; ring < profile.Count - 1; ring++)
            {
                for (int i = 0; i < sides; i++)
                {
                    int a = start + ring * stride + i;
                    int d = a + stride;
                    AddFacing(a, a + 1, d + 1);
                    AddFacing(a, d + 1, d);
                }
            }

            // The top closes on the last ring itself, so the lid meets the bevel exactly.
            int topRing = start + (profile.Count - 1) * stride;
            int middle = vertices.Count;
            AddVertex(Ground(centre, height), Up, top);

            for (int i = 0; i < sides; i++)
            {
                AddFacing(middle, topRing + i, topRing + i + 1);
            }
        }

        /// <summary>
        /// A flat band lying on the ground between two outlines round the same centre, such
        /// as the line of surf round an island. Both outlines need the same number of radii.
        /// </summary>
        public void AddBand(Vector2 centre, float[] inner, float[] outer, float height, Color color)
        {
            int sides = inner.Length;

            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                Vector2 dirA = Circle(i, sides);
                Vector2 dirB = Circle(next, sides);
                AddQuad(
                    Ground(centre + dirA * inner[i], height),
                    Ground(centre + dirB * inner[next], height),
                    Ground(centre + dirB * outer[next], height),
                    Ground(centre + dirA * outer[i], height),
                    Up, color);
            }
        }

        /// <summary>
        /// A flat curved stroke lying on the ground along an arc, thickest in the middle and
        /// tapering to a point at both ends, like a brushed wave line.
        /// </summary>
        /// <param name="startAngle">Where the arc starts, in radians round <paramref name="centre"/>.</param>
        /// <param name="sweep">How far the arc runs, in radians.</param>
        public void AddGroundArc(Vector2 centre, float radius, float startAngle, float sweep, float width,
            int segments, float height, Color color)
        {
            for (int i = 0; i < segments; i++)
            {
                float t0 = i / (float)segments;
                float t1 = (i + 1) / (float)segments;
                float half0 = width * 0.5f * Mathf.Sin(t0 * Mathf.PI);
                float half1 = width * 0.5f * Mathf.Sin(t1 * Mathf.PI);
                Vector2 dir0 = Direction(startAngle + sweep * t0);
                Vector2 dir1 = Direction(startAngle + sweep * t1);
                AddQuad(
                    Ground(centre + dir0 * (radius - half0), height),
                    Ground(centre + dir1 * (radius - half1), height),
                    Ground(centre + dir1 * (radius + half1), height),
                    Ground(centre + dir0 * (radius + half0), height),
                    Up, color);
            }
        }

        /// <summary>
        /// Blends every colour added so far towards <paramref name="haze"/> by an amount
        /// that depends on the vertex position, like air softening a distant landscape.
        /// </summary>
        public void ApplyHaze(Color haze, System.Func<Vector3, float> amount)
        {
            for (int i = 0; i < colors.Count; i++)
            {
                colors[i] = Color.Lerp(colors[i], haze, Mathf.Clamp01(amount(vertices[i])));
            }
        }

        /// <summary>
        /// Shears everything added so far away from the camera (+Y) in proportion to its
        /// height, like leaning a stack of cards: whatever stands at water level stays put,
        /// and higher parts slide back. A camera looking down then sees the shapes as if from
        /// <paramref name="degrees"/> lower down, without tilting the camera itself.
        /// </summary>
        public void ShearAway(float degrees)
        {
            float slope = Mathf.Tan(degrees * Mathf.Deg2Rad);

            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 position = vertices[i];

                // Up is -Z, so the height is -z.
                position.y -= position.z * slope;
                vertices[i] = position;

                // Normals take the inverse transpose of the shear.
                Vector3 normal = normals[i];
                normal.z += normal.y * slope;
                normals[i] = normal.normalized;
            }
        }

        /// <summary>
        /// Smooth triangles share their vertices; faceted ones get three of their own with
        /// the face normal. Vertices no triangle uses are left out.
        /// </summary>
        public Mesh ToMesh(string name)
        {
            int capacity = triangles.Count + facetedTriangles.Count;
            var outVertices = new List<Vector3>(capacity);
            var outNormals = new List<Vector3>(capacity);
            var outColors = new List<Color>(capacity);
            var outTriangles = new List<int>(capacity);
            var remap = new int[vertices.Count];

            for (int i = 0; i < remap.Length; i++)
            {
                remap[i] = -1;
            }

            foreach (int index in triangles)
            {
                if (remap[index] < 0)
                {
                    remap[index] = outVertices.Count;
                    outVertices.Add(vertices[index]);
                    outNormals.Add(normals[index]);
                    outColors.Add(colors[index]);
                }

                outTriangles.Add(remap[index]);
            }

            for (int i = 0; i < facetedTriangles.Count; i += 3)
            {
                int a = facetedTriangles[i];
                int b = facetedTriangles[i + 1];
                int c = facetedTriangles[i + 2];
                Vector3 face = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]).normalized;
                Color color = (colors[a] + colors[b] + colors[c]) / 3f;

                AddFacetCorner(a);
                AddFacetCorner(b);
                AddFacetCorner(c);

                void AddFacetCorner(int index)
                {
                    outTriangles.Add(outVertices.Count);
                    outVertices.Add(vertices[index]);
                    outNormals.Add(face);
                    outColors.Add(color);
                }
            }

            var mesh = new Mesh { name = name };
            mesh.indexFormat = outVertices.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(outVertices);
            mesh.SetNormals(outNormals);
            mesh.SetColors(outColors);
            mesh.SetTriangles(outTriangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Color color)
        {
            int start = vertices.Count;
            AddVertex(a, normal, color);
            AddVertex(b, normal, color);
            AddVertex(c, normal, color);
            AddVertex(d, normal, color);
            AddFacing(start, start + 1, start + 2);
            AddFacing(start, start + 2, start + 3);
        }

        private void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Vector3 normal, Color color)
        {
            int start = vertices.Count;
            AddVertex(a, normal, color);
            AddVertex(b, normal, color);
            AddVertex(c, normal, color);
            AddFacing(start, start + 1, start + 2);
        }

        /// <summary>
        /// Adds the triangle wound so it faces the same way as its vertex normals: Unity
        /// draws a triangle whose winding normal points at the viewer.
        /// </summary>
        private void AddFacing(int a, int b, int c)
        {
            Vector3 face = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);

            if (face.sqrMagnitude < 1e-14f)
            {
                return;
            }

            if (Vector3.Dot(face, normals[a] + normals[b] + normals[c]) < 0f)
            {
                (b, c) = (c, b);
            }

            List<int> target = IsFaceted ? facetedTriangles : triangles;
            target.Add(a);
            target.Add(b);
            target.Add(c);
        }

        private void AddVertex(Vector3 position, Vector3 normal, Color color)
        {
            if (isLeaning)
            {
                position = leanPivot + leanRotation * (position - leanPivot);
                normal = leanRotation * normal;
            }

            vertices.Add(position);
            normals.Add(normal);
            colors.Add(new Color(color.r, color.g, color.b, 1f));
        }

        private Vector3 ArchPoint(Vector2 centre, float along, Vector2 arch, float radius, float baseHeight)
        {
            return Ground(centre + new Vector2(along, arch.x * radius), baseHeight + arch.y * radius);
        }

        /// <summary>Half circle from the front (y = -1) over the top to the back.</summary>
        private static Vector2 Arch(int index, int count)
        {
            float angle = Mathf.PI - index * (Mathf.PI / count);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        private static Vector3 SideNormal(Vector2 direction, float slope)
        {
            return new Vector3(direction.x, direction.y, -slope).normalized;
        }

        private static Vector2 Circle(int index, int count)
        {
            return Direction(index * (2f * Mathf.PI / count));
        }

        private static Vector2 Direction(float angle)
        {
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
    }
}
