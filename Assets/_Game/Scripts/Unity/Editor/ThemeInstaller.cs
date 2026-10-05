using UnityEditor;
using UnityEngine;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Turns the board skins into the shop's themes: each gets its price, its tile tint and
    /// start pad hue, and the art Tools/flat_art/themes.py draws for it (the board's sky,
    /// the map's sky and gate signs, the shop card). Safe to rerun.
    /// </summary>
    public static class ThemeInstaller
    {
        public const string CatalogPath = "Assets/_Game/Config/SkinCatalog.asset";
        private const string SkinFolder = "Assets/_Game/Config/Skins";
        private const string ArtFolder = "Assets/_Game/Art/Themes";
        private const string MaterialFolder = SkinFolder + "/Runways";
        private const string BaseMarks = "Assets/_Game/Art/Flat/mat_runway_dash.mat";
        private const string BaseSurface = "Assets/_Game/Art/Flat/mat_runway_surface.mat";

        private static readonly Color SkyColor = new Color32(160, 212, 240, 255);

        // In shop order; the first is what every player starts with. Keep the tints and
        // hues in sync with Tools/flat_art/themes.py.
        private static readonly (string asset, string id, int price, Color32 tile, float startHue, Color32 cloud)[] Themes =
        {
            ("SkinClassic", "default", 0, new Color32(255, 250, 241, 255), 140f, new Color32(255, 255, 255, 255)),
            ("SkinOcean", "ocean", 600, new Color32(238, 255, 248, 255), 165f, new Color32(240, 255, 250, 255)),
            ("SkinSunset", "sunset", 900, new Color32(255, 242, 228, 255), 20f, new Color32(255, 228, 214, 255)),
            ("SkinAurora", "starter", 1200, new Color32(255, 240, 245, 255), 335f, new Color32(255, 238, 245, 255)),
            ("SkinNeon", "neon", 1500, new Color32(222, 230, 255, 255), 220f, new Color32(150, 150, 204, 255)),
        };

        [MenuItem("Tools/AirLine Pop/Install Themes")]
        public static void InstallFromMenu()
        {
            Debug.Log(Install());
        }

        public static string Install()
        {
            var skins = new SkinSO[Themes.Length];
            AssetWriter.EnsureFolder(MaterialFolder);

            for (int i = 0; i < Themes.Length; i++)
            {
                (string asset, string id, int price, Color32 tile, float startHue, Color32 cloud) = Themes[i];
                var skin = AssetDatabase.LoadAssetAtPath<SkinSO>(SkinFolder + "/" + asset + ".asset");

                if (skin == null)
                {
                    return "Missing theme " + asset + "; themes left unchanged.";
                }

                SetPalette(skin, tile, startHue);
                skin.EditorConfigureTheme(price, i == 0, Art("sky_" + id), Art("map_sky_" + id), Art("gate_flown_" + id),
                    Art("gate_current_" + id), Art("gate_locked_" + id), Art("theme_" + id));
                skin.EditorConfigureRunway(RunwayMaterial(BaseSurface, "runway_lights_" + id, "Surface_" + id),
                    RunwayMaterial(BaseMarks, "runway_dash_" + id, "Marks_" + id), cloud);
                EditorUtility.SetDirty(skin);
                skins[i] = skin;
            }

            var catalog = AssetDatabase.LoadAssetAtPath<SkinCatalogSO>(CatalogPath);
            var so = new SerializedObject(catalog);
            SerializedProperty list = so.FindProperty("skins");
            list.arraySize = skins.Length;

            for (int i = 0; i < skins.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = skins[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            return "Themes: " + skins.Length + " installed.";
        }

        /// <summary>A copy of the board's runway material wearing the theme's own texture.</summary>
        private static Material RunwayMaterial(string basePath, string textureName, string materialName)
        {
            string path = ArtFolder + "/" + textureName + ".png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.wrapModeU = TextureWrapMode.Repeat;
            importer.wrapModeV = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            var source = AssetDatabase.LoadAssetAtPath<Material>(basePath);
            var material = new Material(source) { mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path) };
            return AssetWriter.SaveMaterial(MaterialFolder + "/" + materialName + ".mat", material);
        }

        private static Sprite Art(string name)
        {
            return SpriteImport.Import(ArtFolder + "/" + name + ".png", 100f);
        }

        /// <summary>A pastel take on the same flat board: the theme's tile tint, a white centre line, its own start pad hue.</summary>
        private static void SetPalette(SkinSO skin, Color tile, float hue)
        {
            var so = new SerializedObject(skin);
            SerializedProperty palette = so.FindProperty("palette");
            palette.FindPropertyRelative("background").colorValue = SkyColor;
            palette.FindPropertyRelative("cell").colorValue = tile;
            palette.FindPropertyRelative("usesLevelHue").boolValue = false;
            palette.FindPropertyRelative("firstLevelHue").floatValue = hue;
            palette.FindPropertyRelative("hueStepPerLevel").floatValue = 1f;
            palette.FindPropertyRelative("startTone").vector2Value = new Vector2(0.48f, 0.8f);
            // Squares the runway covers only warm a touch: the runway itself shows the way.
            palette.FindPropertyRelative("visitedTone").vector2Value = new Vector2(0.05f, 0.98f);
            palette.FindPropertyRelative("headTone").vector2Value = new Vector2(0.07f, 0.99f);
            palette.FindPropertyRelative("pathTone").vector2Value = new Vector2(0f, 1f);
            palette.FindPropertyRelative("wonTone").vector2Value = new Vector2(0.22f, 1f);
            palette.FindPropertyRelative("pathAlpha").floatValue = 1f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
