using UnityEditor;
using UnityEngine;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Writes the runtime text table into Resources from <see cref="LocalizationTable"/> and
    /// the destinations' names. The scene builders run it first, so every build ships the
    /// text it lays out. Safe to rerun.
    /// </summary>
    public static class LocalizationInstaller
    {
        private const string Folder = "Assets/_Game/Resources";
        private const string AssetPath = Folder + "/" + LocalizationSO.ResourcePath + ".asset";

        [MenuItem("Tools/AirLine Pop/Install Localization")]
        public static void InstallFromMenu()
        {
            Debug.Log(Install());
        }

        public static string Install()
        {
            AssetWriter.EnsureFolder(Folder);
            var table = AssetDatabase.LoadAssetAtPath<LocalizationSO>(AssetPath);

            if (table == null)
            {
                table = ScriptableObject.CreateInstance<LocalizationSO>();
                AssetDatabase.CreateAsset(table, AssetPath);
            }

            table.EditorSetEntries(LocalizationTable.Entries(DestinationInstaller.RouteTexts()));
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            return "Localization: " + table.Entries.Count + " lines in English and Vietnamese.";
        }
    }
}
