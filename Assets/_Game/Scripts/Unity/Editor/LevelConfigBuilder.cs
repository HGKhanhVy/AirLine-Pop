using System.Collections.Generic;
using System.IO;
using AirlinePop.SingleLine.Core;
using AirlinePop.SingleLine.Data;
using AirlinePop.SingleLine.Editor;
using UnityEditor;
using UnityEngine;

// Lives outside the importer's assembly definition on purpose: the config and the level
// assets are MonoBehaviour side types in Assembly-CSharp, which an asmdef cannot see.
namespace AirlinePop.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Rebuilds the one config that lists every level from the exported chapter files.
    ///
    /// The chapter json stays the shipping data; this turns it into the per level assets
    /// a designer can see and reorder in the Inspector. Assets are reused rather than
    /// recreated so the references already held by the config, and by anything else that
    /// points at a level, survive a rebuild.
    /// </summary>
    public static class LevelConfigBuilder
    {
        public const string DefaultLevelFolder = "Assets/_Game/Config/Levels";
        public const string DefaultConfigPath = "Assets/_Game/Config/SingleLineLevelConfig.asset";

        [MenuItem("Tools/Single Line/Rebuild Level Config")]
        public static void RebuildFromMenu()
        {
            int count = Rebuild(CampaignExporter.DefaultOutputFolder, DefaultLevelFolder, DefaultConfigPath);
            EditorUtility.DisplayDialog("Single Line", "Level config now lists " + count + " levels.", "OK");
        }

        public static int Rebuild(string chapterFolder, string levelFolder, string configPath)
        {
            List<LevelData> ordered = ReadCampaign(chapterFolder, out List<string> chapterIds);

            if (ordered.Count == 0)
            {
                Debug.LogError("No chapter files under " + chapterFolder + ". Run the Level Importer first.");
                return 0;
            }

            EnsureFolder(levelFolder);

            LevelOrderSO config = LoadOrCreate<LevelOrderSO>(configPath);
            var definitions = new List<SingleLineLevelSO>(ordered.Count);

            for (int i = 0; i < ordered.Count; i++)
            {
                int levelNumber = i + 1;
                string assetPath = Path.Combine(levelFolder, "Level_" + levelNumber.ToString("000") + ".asset")
                    .Replace('\\', '/');

                SingleLineLevelSO definition = LoadOrCreate<SingleLineLevelSO>(assetPath);
                Write(definition, ordered[i], levelNumber, chapterIds[i]);

                definitions.Add(definition);
            }

            config.EditorSetLevels(definitions);

            RemoveStaleDefinitions(levelFolder, ordered.Count);

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return ordered.Count;
        }

        private static List<LevelData> ReadCampaign(string chapterFolder, out List<string> chapterIds)
        {
            var levels = new List<LevelData>(CampaignLevelAddress.MaxLevelNumber);
            chapterIds = new List<string>(CampaignLevelAddress.MaxLevelNumber);

            if (!Directory.Exists(chapterFolder))
            {
                return levels;
            }

            string[] files = Directory.GetFiles(chapterFolder, "ch*.json");
            System.Array.Sort(files, System.StringComparer.Ordinal);

            foreach (string file in files)
            {
                string chapterId = Path.GetFileNameWithoutExtension(file);

                foreach (LevelData level in LevelJsonSerializer.DeserializeChapter(File.ReadAllText(file)))
                {
                    levels.Add(level);
                    chapterIds.Add(chapterId);
                }
            }

            return levels;
        }

        /// <summary>
        /// The fields are private and serialized, which is what keeps them read only at
        /// runtime. A SerializedObject is the supported way for a tool to fill them in.
        /// </summary>
        private static void Write(SingleLineLevelSO definition, LevelData level, int levelNumber, string chapterId)
        {
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("levelId").stringValue = level.Id;
            serialized.FindProperty("levelNumber").intValue = levelNumber;
            serialized.FindProperty("chapterId").stringValue = chapterId;
            serialized.FindProperty("difficulty").intValue = Mathf.Clamp(level.Difficulty, 1, 10);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
        }


        /// <summary>
        /// A shorter campaign must not leave the levels it dropped lying in the folder,
        /// where the next reader would take them for part of the run.
        /// </summary>
        private static void RemoveStaleDefinitions(string levelFolder, int keptCount)
        {
            foreach (string path in Directory.GetFiles(levelFolder, "Level_*.asset"))
            {
                string name = Path.GetFileNameWithoutExtension(path);

                if (int.TryParse(name.Substring("Level_".Length), out int number) && number > keptCount)
                {
                    AssetDatabase.DeleteAsset(path.Replace('\\', '/'));
                }
            }
        }

        private static void EnsureFolder(string folder)
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
                AssetDatabase.Refresh();
            }
        }

        private static T LoadOrCreate<T>(string assetPath) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);

            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }
    }
}
