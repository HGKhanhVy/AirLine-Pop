using UnityEditor;
using UnityEngine;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    public static class SpriteImport
    {
        /// <summary>Imports a PNG as a single centred sprite and returns it, or null if it is missing.</summary>
        public static Sprite Import(string path, float pixelsPerUnit)
        {
            return Import(path, pixelsPerUnit, SpriteAlignment.Center);
        }

        /// <summary>Imports a PNG as a single sprite pivoted at <paramref name="alignment"/>, such as the bottom for a hat.</summary>
        public static Sprite Import(string path, float pixelsPerUnit, SpriteAlignment alignment)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null)
            {
                return null;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)alignment;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
