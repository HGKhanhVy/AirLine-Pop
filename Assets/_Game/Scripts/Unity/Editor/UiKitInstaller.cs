using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Imports the generated UI kit (Tools/ui_kit/generate_ui_kit.py) as sliced sprites and
    /// builds the Vietnamese-ready TMP font the new screens use, with the brown outline
    /// baked into a shared material so labels match the art without per-label setup.
    /// </summary>
    public static class UiKitInstaller
    {
        public const string KitFolder = "Assets/_Game/Art/UIKit";
        public const string FontFolder = "Assets/_Game/Art/Fonts";
        public const string TitleFontPath = FontFolder + "/Baloo2-ExtraBold SDF.asset";
        public const string BodyFontPath = FontFolder + "/Baloo2-SemiBold SDF.asset";
        public const string OutlinedMaterialPath = FontFolder + "/Baloo2-ExtraBold Outline.mat";

        private static readonly Color32 OutlineBrown = new Color32(90, 56, 37, 255);

        // Characters baked up front so the first frame never stalls on a dynamic glyph.
        private const string VietnameseCharacters =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,:;!?+-×/%()\"'·…" +
            "ĂÂĐÊÔƠƯăâđêôơư" +
            "ÀÁẢÃẠẰẮẲẴẶẦẤẨẪẬÈÉẺẼẸỀẾỂỄỆÌÍỈĨỊÒÓỎÕỌỒỐỔỖỘỜỚỞỠỢÙÚỦŨỤỪỨỬỮỰỲÝỶỸỴ" +
            "àáảãạằắẳẵặầấẩẫậèéẻẽẹềếểễệìíỉĩịòóỏõọồốổỗộờớởỡợùúủũụừứửữựỳýỷỹỵ";

        [MenuItem("Tools/AirLine Pop/Install UI Kit")]
        public static void InstallFromMenu()
        {
            EditorUtility.DisplayDialog("AirLine Pop", Install(), "OK");
        }

        public static string Install()
        {
            int sprites = ImportSprites();
            TMP_FontAsset title = BuildFont(FontFolder + "/Baloo2-ExtraBold.ttf", TitleFontPath);
            BuildFont(FontFolder + "/Baloo2-SemiBold.ttf", BodyFontPath);
            BuildOutlinedMaterial(title);
            AssetDatabase.SaveAssets();
            return "UI kit: " + sprites + " sprites imported, Baloo 2 fonts built.";
        }

        private static int ImportSprites()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { KitFolder });

            foreach (string guid in guids)
            {
                ImportKitSprite(AssetDatabase.GUIDToAssetPath(guid));
            }

            return guids.Length;
        }

        /// <summary>
        /// Imports one kit sprite the way every kit sprite must be: other installers that add
        /// an icon to the kit call this rather than a generic sprite import.
        /// </summary>
        public static Sprite ImportKitSprite(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null)
            {
                return null;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;

            // The kit is drawn at twice its layout size; twice the canvas's 100 px per
            // unit keeps sliced borders the size they were designed at.
            importer.spritePixelsPerUnit = KitPixelsPerUnit;

            // Icons are shown well below their drawn size; mipmaps keep the
            // downscaled outline smooth instead of shimmering and jagged.
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Trilinear;
            importer.alphaIsTransparency = true;

            // Block compression turns the crisp brown outlines into blotchy blocks.
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            ClearPlatformCompression(importer);
            importer.spriteBorder = BorderFor(System.IO.Path.GetFileNameWithoutExtension(path)) * KitScale;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // Keep in sync with OUT_SCALE in Tools/ui_kit/generate_ui_kit.py.
        private const float KitScale = 2f;
        private const float KitPixelsPerUnit = 100f * KitScale;

        /// <summary>
        /// Platform overrides win over the default setting, so a compressed Android or iOS
        /// override would bring the artefacts back on device. Each one is set uncompressed.
        /// </summary>
        private static void ClearPlatformCompression(TextureImporter importer)
        {
            foreach (string platform in new[] { "Android", "iPhone", "Standalone", "WebGL" })
            {
                TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platform);

                if (!settings.overridden)
                {
                    continue;
                }

                settings.format = TextureImporterFormat.RGBA32;
                settings.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SetPlatformTextureSettings(settings);
            }
        }

        /// <summary>9-slice borders in layout pixels (left, bottom, right, top); icons stay unsliced.</summary>
        private static Vector4 BorderFor(string name)
        {
            // Drawn whole: icons, the ticket and the switch parts are never stretched.
            if (name.StartsWith("icon_") || name.StartsWith("toggle_") || name.StartsWith("tab_") || name == "soft_shadow"
                || name == UiBuilder.TicketFace)
            {
                return Vector4.zero;
            }

            if (name.StartsWith("btn_round"))
            {
                return new Vector4(56, 60, 56, 56);
            }

            if (name.StartsWith("btn_"))
            {
                return new Vector4(36, 44, 36, 36);
            }

            // Bars slice at their round caps, so a short one stays a pill.
            if (name.StartsWith("bar_"))
            {
                return new Vector4(16, 16, 16, 16);
            }

            if (name == "pill_cream")
            {
                return new Vector4(30, 30, 30, 30);
            }

            return new Vector4(40, 44, 40, 40);
        }

        private static TMP_FontAsset BuildFont(string sourcePath, string assetPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);

            if (existing != null)
            {
                existing.TryAddCharacters(VietnameseCharacters);
                return existing;
            }

            var source = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic);
            font.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(font, assetPath);
            font.material.name = font.name + " Material";
            AssetDatabase.AddObjectToAsset(font.material, font);
            font.atlasTextures[0].name = font.name + " Atlas";
            AssetDatabase.AddObjectToAsset(font.atlasTextures[0], font);
            font.TryAddCharacters(VietnameseCharacters);
            EditorUtility.SetDirty(font);
            return font;
        }

        private static void BuildOutlinedMaterial(TMP_FontAsset font)
        {
            var material = new Material(font.material) { name = "Baloo2-ExtraBold Outline" };
            material.EnableKeyword("OUTLINE_ON");
            material.SetColor(ShaderUtilities.ID_OutlineColor, OutlineBrown);
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.22f);
            material.SetFloat(ShaderUtilities.ID_FaceDilate, 0.18f);
            material.EnableKeyword("UNDERLAY_ON");
            material.SetColor(ShaderUtilities.ID_UnderlayColor, new Color32(90, 56, 37, 200));
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.6f);
            material.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.3f);
            AssetWriter.SaveMaterial(OutlinedMaterialPath, material);
        }
    }
}
