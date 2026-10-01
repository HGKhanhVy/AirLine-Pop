using UnityEditor;
using UnityEngine;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Saves generated assets so that running a tool again refreshes them in place: the
    /// asset keeps its GUID, and every prefab pointing at it keeps working.
    /// </summary>
    public static class AssetWriter
    {
        public static Material SaveMaterial(string path, Material material)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (existing == null)
            {
                AssetDatabase.CreateAsset(material, path);
                return material;
            }

            existing.shader = material.shader;
            existing.CopyPropertiesFromMaterial(material);
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(material);
            return existing;
        }

        public static Mesh SaveMesh(string path, Mesh mesh)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);

            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }

            CopyGeometry(mesh, existing);
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(mesh);
            return existing;
        }

        /// <summary>
        /// Rewrites the mesh through the Mesh API rather than CopySerialized, which updates
        /// the data on disk but leaves the copy already on the GPU drawing the old shape
        /// until the editor restarts.
        /// </summary>
        private static void CopyGeometry(Mesh source, Mesh target)
        {
            target.Clear();
            target.indexFormat = source.indexFormat;
            target.SetVertices(source.vertices);
            target.SetNormals(source.normals);

            if (source.tangents.Length == source.vertexCount)
            {
                target.SetTangents(source.tangents);
            }

            target.SetColors(source.colors);
            target.SetUVs(0, source.uv);
            target.subMeshCount = source.subMeshCount;

            for (int i = 0; i < source.subMeshCount; i++)
            {
                target.SetTriangles(source.GetTriangles(i), i);
            }

            target.RecalculateBounds();
        }

        public static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            int slash = folder.LastIndexOf('/');
            EnsureFolder(folder.Substring(0, slash));
            AssetDatabase.CreateFolder(folder.Substring(0, slash), folder.Substring(slash + 1));
        }

        /// <summary>Returns the child with that name, creating it at the parent's origin if missing.</summary>
        public static Transform FindOrCreateChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);

            if (child != null)
            {
                return child;
            }

            var created = new GameObject(name);
            created.transform.SetParent(parent, false);
            return created.transform;
        }

        public static T GetOrAdd<T>(GameObject target) where T : Component
        {
            if (!target.TryGetComponent(out T component))
            {
                component = target.AddComponent<T>();
            }

            return component;
        }
    }
}
