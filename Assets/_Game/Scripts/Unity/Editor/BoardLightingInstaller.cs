using UnityEditor;
using UnityEngine;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Creates the board light asset if there is none and puts <see cref="BoardLighting"/>
    /// on the gameplay prefab's root. An existing asset is left as tuned.
    /// </summary>
    public static class BoardLightingInstaller
    {
        private const string GameplayPrefabPath = "Assets/_Game/Prefabs/SingleLineGameplay.prefab";
        private const string LightingPath = "Assets/_Game/Config/BoardLighting.asset";

        public static string Install()
        {
            var lighting = AssetDatabase.LoadAssetAtPath<BoardLightingSO>(LightingPath);

            if (lighting == null)
            {
                lighting = ScriptableObject.CreateInstance<BoardLightingSO>();
                AssetDatabase.CreateAsset(lighting, LightingPath);
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(GameplayPrefabPath);

            try
            {
                var applier = AssetWriter.GetOrAdd<BoardLighting>(contents);
                var applierObject = new SerializedObject(applier);
                applierObject.FindProperty("lighting").objectReferenceValue = lighting;
                applierObject.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, GameplayPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            AssetDatabase.SaveAssets();
            return GameplayPrefabPath + ": board light from " + LightingPath + ".";
        }
    }
}
