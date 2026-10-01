using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Builds props in the CubeAnimals look: soft rounded blocks whose colour comes from a
    /// swatch of the pack's shared atlas. Every prop drawn this way uses the animals' own
    /// material, so a whole scene of props and cats batches together.
    ///
    /// Rounded boxes are a subdivided cube whose points are pulled onto a box shrunk by
    /// the corner radius and pushed back out along the offset, which gives flat faces with
    /// round edges and smooth normals from the same step. Y is up.
    /// </summary>
    public sealed class PaletteMeshBuilder
    {
        private readonly List<Vector3> vertices = new List<Vector3>(4096);
        private readonly List<Vector3> normals = new List<Vector3>(4096);
        private readonly List<Vector2> uvs = new List<Vector2>(4096);
        private readonly List<int> triangles = new List<int>(8192);

        private Matrix4x4 transform = Matrix4x4.identity;

        /// <summary>Applies to every shape added until changed; lets a group be moved or turned as one.</summary>
        public Matrix4x4 Transform
        {
            get => transform;
            set => transform = value;
        }

        public void AddRoundedBox(Vector3 center, Vector3 size, float radius, PaletteCell cell, int segments = 4)
        {
            Vector3 half = size * 0.5f;
            radius = Mathf.Min(radius, Mathf.Min(half.x, Mathf.Min(half.y, half.z)));
            Vector3 inner = half - Vector3.one * radius;
            Vector2 uv = cell.CenterUv;

            // Six faces, each an (n+1)^2 grid, projected through the rounding step.
            AddFace(center, half, inner, radius, uv, Vector3.right, Vector3.up, Vector3.forward, segments);
            AddFace(center, half, inner, radius, uv, Vector3.left, Vector3.up, Vector3.back, segments);
            AddFace(center, half, inner, radius, uv, Vector3.up, Vector3.forward, Vector3.right, segments);
            AddFace(center, half, inner, radius, uv, Vector3.down, Vector3.back, Vector3.right, segments);
            AddFace(center, half, inner, radius, uv, Vector3.forward, Vector3.up, Vector3.left, segments);
            AddFace(center, half, inner, radius, uv, Vector3.back, Vector3.up, Vector3.right, segments);
        }

        /// <summary>A rounded box turned about Y, for props that should not sit square to the grid.</summary>
        public void AddRoundedBox(Vector3 center, Vector3 size, float radius, PaletteCell cell, float yawDegrees, int segments = 4)
        {
            Matrix4x4 saved = transform;
            transform = saved * Matrix4x4.TRS(center, Quaternion.Euler(0f, yawDegrees, 0f), Vector3.one);
            AddRoundedBox(Vector3.zero, size, radius, cell, segments);
            transform = saved;
        }

        /// <summary>A capped cylinder along Y with softly faceted sides.</summary>
        public void AddCylinder(Vector3 baseCenter, float radius, float height, int sides, PaletteCell cell)
        {
            AddFrustum(baseCenter, radius, radius, height, sides, cell);
        }

        /// <summary>A cone or truncated cone along Y; <paramref name="topRadius"/> 0 gives a point.</summary>
        public void AddFrustum(Vector3 baseCenter, float bottomRadius, float topRadius, float height, int sides, PaletteCell cell)
        {
            Vector2 uv = cell.CenterUv;
            float slope = (bottomRadius - topRadius) / height;

            for (int i = 0; i < sides; i++)
            {
                float a0 = Mathf.PI * 2f * i / sides;
                float a1 = Mathf.PI * 2f * (i + 1) / sides;
                Vector3 d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                Vector3 d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                Vector3 n0 = (d0 + Vector3.up * slope).normalized;
                Vector3 n1 = (d1 + Vector3.up * slope).normalized;

                int start = vertices.Count;
                AddVertex(baseCenter + d0 * bottomRadius, n0, uv);
                AddVertex(baseCenter + d1 * bottomRadius, n1, uv);
                AddVertex(baseCenter + d1 * topRadius + Vector3.up * height, n1, uv);
                AddVertex(baseCenter + d0 * topRadius + Vector3.up * height, n0, uv);
                AddQuadIndices(start, start + 3, start + 2, start + 1);

                if (topRadius > 0f)
                {
                    AddTriangle(baseCenter + Vector3.up * height, baseCenter + d1 * topRadius + Vector3.up * height,
                        baseCenter + d0 * topRadius + Vector3.up * height, Vector3.up, uv);
                }

                AddTriangle(baseCenter, baseCenter + d0 * bottomRadius, baseCenter + d1 * bottomRadius, Vector3.down, uv);
            }
        }

        /// <summary>A low-poly ball, for canopies, clouds and pom-poms.</summary>
        public void AddSphere(Vector3 center, Vector3 radii, PaletteCell cell, int rings = 6, int sides = 10)
        {
            Vector2 uv = cell.CenterUv;
            int start = vertices.Count;

            for (int r = 0; r <= rings; r++)
            {
                float v = Mathf.PI * r / rings;

                for (int s = 0; s <= sides; s++)
                {
                    float u = Mathf.PI * 2f * s / sides;
                    Vector3 n = new Vector3(Mathf.Sin(v) * Mathf.Cos(u), Mathf.Cos(v), Mathf.Sin(v) * Mathf.Sin(u));
                    AddVertex(center + Vector3.Scale(n, radii), Vector3.Scale(n, new Vector3(1f / radii.x, 1f / radii.y, 1f / radii.z)).normalized, uv);
                }
            }

            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < sides; s++)
                {
                    int a = start + r * (sides + 1) + s;
                    int b = a + sides + 1;
                    AddQuadIndices(a, a + 1, b + 1, b);
                }
            }
        }

        public Mesh Build(string name)
        {
            var mesh = new Mesh { name = name };
            mesh.indexFormat = vertices.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.SetTangents(BuildTangents());
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Any direction across the surface will do: the atlas has no normal map detail,
        /// but the pack's shader still builds a tangent frame, and the frame
        /// RecalculateTangents derives from flat-colour UVs is degenerate (NaN), which
        /// lights whole faces white.
        /// </summary>
        private Vector4[] BuildTangents()
        {
            var tangents = new Vector4[normals.Count];

            for (int i = 0; i < normals.Count; i++)
            {
                Vector3 n = normals[i];
                Vector3 axis = Mathf.Abs(n.y) < 0.9f ? Vector3.up : Vector3.right;
                Vector3 t = Vector3.Cross(axis, n).normalized;
                tangents[i] = new Vector4(t.x, t.y, t.z, 1f);
            }

            return tangents;
        }

        private void AddFace(Vector3 center, Vector3 half, Vector3 inner, float radius, Vector2 uv,
            Vector3 normal, Vector3 axisU, Vector3 axisV, int n)
        {
            int start = vertices.Count;

            for (int j = 0; j <= n; j++)
            {
                for (int i = 0; i <= n; i++)
                {
                    float fu = (i / (float)n) * 2f - 1f;
                    float fv = (j / (float)n) * 2f - 1f;
                    Vector3 p = Vector3.Scale(normal + axisU * fu + axisV * fv, half);
                    Vector3 clamped = new Vector3(
                        Mathf.Clamp(p.x, -inner.x, inner.x),
                        Mathf.Clamp(p.y, -inner.y, inner.y),
                        Mathf.Clamp(p.z, -inner.z, inner.z));
                    Vector3 dir = (p - clamped).normalized;
                    AddVertex(center + clamped + dir * radius, dir, uv);
                }
            }

            for (int j = 0; j < n; j++)
            {
                for (int i = 0; i < n; i++)
                {
                    int a = start + j * (n + 1) + i;
                    int b = a + n + 1;
                    AddQuadIndices(a, a + 1, b + 1, b);
                }
            }
        }

        private void AddVertex(Vector3 position, Vector3 normal, Vector2 uv)
        {
            vertices.Add(transform.MultiplyPoint3x4(position));
            normals.Add(transform.MultiplyVector(normal).normalized);
            uvs.Add(uv);
        }

        private void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Vector3 normal, Vector2 uv)
        {
            int start = vertices.Count;
            AddVertex(a, normal, uv);
            AddVertex(b, normal, uv);
            AddVertex(c, normal, uv);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
        }

        private void AddQuadIndices(int a, int b, int c, int d)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
            triangles.Add(a);
            triangles.Add(c);
            triangles.Add(d);
        }
    }
}
