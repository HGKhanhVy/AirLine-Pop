using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Outside the importer's assembly definition on purpose: it reaches the effect player,
// which is an Assembly-CSharp type an asmdef cannot see.
namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Lifts the reference block's particles out of OneLineBlock into prefabs of their
    /// own and hands them to the board's effect player.
    ///
    /// The reference prefab keeps forty particle systems inline, which is why none of
    /// them could be reused as they stood: a prefab field needs an asset to point at.
    /// Doing the lift here rather than by hand means the mapping from a gameplay moment
    /// to a particle is written down and can be rerun after the art is updated.
    ///
    /// The component searches here are the one place they are allowed: this runs once
    /// from a menu, over art whose hierarchy is not ours to hard wire, and nothing it
    /// touches exists at runtime.
    /// </summary>
    public static class BlockEffectExtractor
    {
        private const string SourcePrefab = "Assets/_Game/Asset_Resources/GameObject/OneLineBlock.prefab";
        private const string OutputFolder = "Assets/_Game/Effects";
        private const string BoardPrefab = "Assets/_Game/Prefabs/SingleLineGameplay.prefab";

        /// <summary>
        /// Which child of the reference block plays for which moment. Names come from the
        /// reference art and are matched exactly, so a renamed child is reported rather
        /// than silently skipped.
        /// </summary>
        private static readonly KeyValuePair<GameplayEffect, string>[] Mapping =
        {
            new KeyValuePair<GameplayEffect, string>(GameplayEffect.CellConnected, "effect_jump"),
            new KeyValuePair<GameplayEffect, string>(GameplayEffect.CellRejected, "effect_dust"),
            new KeyValuePair<GameplayEffect, string>(GameplayEffect.StartCue, "glow")
        };

        [MenuItem("Tools/Single Line/Extract Block Effects")]
        public static void Extract()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefab);

            if (source == null)
            {
                EditorUtility.DisplayDialog("Single Line", "Cannot find " + SourcePrefab, "OK");
                return;
            }

            if (!Directory.Exists(OutputFolder))
            {
                Directory.CreateDirectory(OutputFolder);
                AssetDatabase.Refresh();
            }

            var extracted = new Dictionary<GameplayEffect, ParticleSystem>();
            var missing = new List<string>();

            GameObject working = Object.Instantiate(source);

            try
            {
                foreach (KeyValuePair<GameplayEffect, string> entry in Mapping)
                {
                    Transform child = FindByName(working.transform, entry.Value);

                    if (child == null)
                    {
                        missing.Add(entry.Value);
                        continue;
                    }

                    ParticleSystem saved = SaveAsPrefab(child.gameObject, entry.Value);

                    if (saved == null)
                    {
                        missing.Add(entry.Value + " (no particle system)");
                        continue;
                    }

                    extracted.Add(entry.Key, saved);
                }
            }
            finally
            {
                Object.DestroyImmediate(working);
            }

            int assigned = Assign(extracted);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string report = "Extracted " + extracted.Count + " effect prefabs, assigned " + assigned + ".";

            if (missing.Count > 0)
            {
                report += "\nNot found: " + string.Join(", ", missing);
            }

            EditorUtility.DisplayDialog("Single Line", report, "OK");
        }

        private static ParticleSystem SaveAsPrefab(GameObject child, string name)
        {
            if (child.GetComponentInChildren<ParticleSystem>(true) == null)
            {
                return null;
            }

            // Detached first, or the saved prefab would carry the block's own transform
            // offset and every burst would land a fraction of a cell away.
            child.transform.SetParent(null, false);
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            child.SetActive(true);

            string path = OutputFolder + "/FX_" + name + ".prefab";
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(child, path);
            return saved == null ? null : saved.GetComponentInChildren<ParticleSystem>(true);
        }

        /// <summary>
        /// Writes the prefabs into the bindings already listed on the board prefab, so the
        /// capacities and tint flags a designer set are kept.
        /// </summary>
        private static int Assign(Dictionary<GameplayEffect, ParticleSystem> extracted)
        {
            if (extracted.Count == 0)
            {
                return 0;
            }

            var board = AssetDatabase.LoadAssetAtPath<GameObject>(BoardPrefab);
            PooledEffectPlayer player = board == null
                ? null
                : board.GetComponentInChildren<PooledEffectPlayer>(true);

            if (player == null)
            {
                Debug.LogWarning("No PooledEffectPlayer under " + BoardPrefab + ", prefabs were saved but not assigned.");
                return 0;
            }

            var serialized = new SerializedObject(player);
            SerializedProperty bindings = serialized.FindProperty("bindings");
            int assigned = 0;

            for (int i = 0; i < bindings.arraySize; i++)
            {
                SerializedProperty binding = bindings.GetArrayElementAtIndex(i);
                var effect = (GameplayEffect)binding.FindPropertyRelative("effect").enumValueIndex;

                if (!extracted.TryGetValue(effect, out ParticleSystem prefab))
                {
                    continue;
                }

                binding.FindPropertyRelative("prefab").objectReferenceValue = prefab;
                assigned++;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(player);
            PrefabUtility.SavePrefabAsset(board);
            return assigned;
        }

        /// <summary>Depth first by exact name; the reference block nests its effects several levels down.</summary>
        private static Transform FindByName(Transform root, string name)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);

                if (child.name == name)
                {
                    return child;
                }

                Transform found = FindByName(child, name);

                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
