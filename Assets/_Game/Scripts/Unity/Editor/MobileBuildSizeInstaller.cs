using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Shrinks the mobile build. Most of the game's art was imported uncompressed, so a
    /// 256 px icon cost 340 KB; every such texture is switched to high quality compression
    /// (ASTC 4x4 on Android), which keeps the flat outlines crisp at a quarter of the size.
    /// IL2CPP is also told to favour a small binary over the last bit of speed, which a
    /// puzzle game never needs. Safe to rerun.
    /// </summary>
    public static class MobileBuildSizeInstaller
    {
        private static readonly string[] ArtFolders = { "Assets/_Game", "Assets/UXUI", "Assets/_SDK" };

        [MenuItem("Tools/AirLine Pop/Shrink Mobile Build")]
        public static void InstallFromMenu()
        {
            Debug.Log(Install());
        }

        public static string Install()
        {
            int compressed = CompressTextures();
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.Android, Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.iOS, Il2CppCodeGeneration.OptimizeSize);
            AssetDatabase.SaveAssets();
            return compressed + " textures compressed; IL2CPP set to smaller builds.";
        }

        private static int CompressTextures()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", ArtFolders);
            int changed = 0;

            try
            {
                AssetDatabase.StartAssetEditing();

                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);

                    if (TryCompress(path))
                    {
                        changed++;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            return changed;
        }

        /// <summary>
        /// Only plain uncompressed art is touched. Textures read on the CPU, platform
        /// overrides someone chose on purpose, and editor-only folders are left alone.
        /// </summary>
        private static bool TryCompress(string path)
        {
            if (path.Contains("/Editor/") || !(AssetImporter.GetAtPath(path) is TextureImporter importer))
            {
                return false;
            }

            if (importer.isReadable
                || importer.textureCompression != TextureImporterCompression.Uncompressed
                || importer.GetPlatformTextureSettings("Android").overridden)
            {
                return false;
            }

            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
            return true;
        }
    }
}
