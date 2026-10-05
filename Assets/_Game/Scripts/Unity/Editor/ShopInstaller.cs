using UnityEditor;
using UnityEngine;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Creates what the shop sells: one livery asset per plane model drawn by Tools/flat_art
    /// (the coral starter by generate_flat_art.py and generate_home.py, the rest by
    /// plane_models.py), their catalogue, and the pictures on the snack packs.
    /// </summary>
    public static class ShopInstaller
    {
        public const string CatalogPath = Folder + "/LiveryCatalog.asset";
        public const string EconomyPath = "Assets/_Game/Config/EconomyConfig.asset";

        private const string Folder = "Assets/_Game/Config/Liveries";
        private const string BoardArt = "Assets/_Game/Art/Flat";
        private const string HomeArt = "Assets/_Game/Art/FlatHome";
        private const float BoardPlanePixelsPerUnit = 512f;
        private const float HomePixelsPerUnit = 128f;

        // Keep in sync with Tools/flat_art/plane_models.py; the first is the starter. The ids
        // once named paint jobs and are saved in profiles, so each now stands for a model:
        // sunny the propeller plane, mint the seaplane, sky the jumbo, lavender the supersonic.
        private static readonly (string id, int price)[] Liveries =
        {
            ("coral", 0), ("sunny", 600), ("mint", 900), ("sky", 1200), ("lavender", 1500), ("whale", 2000),
        };

        private static readonly string[] SnackArt = { "shop_snack_1", "shop_snack_5", "shop_snack_10" };

        [MenuItem("Tools/AirLine Pop/Install Shop")]
        public static void InstallFromMenu()
        {
            Debug.Log(Install());
        }

        public static string Install()
        {
            LiveryCatalogSO catalog = BuildCatalog();
            LinkSnackArt();
            AssetDatabase.SaveAssets();
            return "Shop: " + catalog.Liveries.Count + " liveries and the snack packs.";
        }

        public static LiveryCatalogSO BuildCatalog()
        {
            AssetWriter.EnsureFolder(Folder);
            var liveries = new LiverySO[Liveries.Length];

            for (int i = 0; i < Liveries.Length; i++)
            {
                (string id, int price) = Liveries[i];
                string suffix = i == 0 ? string.Empty : "_" + id;
                string path = Folder + "/Livery_" + id + ".asset";
                var livery = AssetDatabase.LoadAssetAtPath<LiverySO>(path);
                bool isNew = livery == null;

                if (isNew)
                {
                    livery = ScriptableObject.CreateInstance<LiverySO>();
                    AssetDatabase.CreateAsset(livery, path);
                }

                Sprite topDown = SpriteImport.Import(BoardArt + "/airplane" + suffix + ".png", BoardPlanePixelsPerUnit);
                Sprite parked = ImportFooted(HomeArt + "/plane_parked" + suffix + ".png");
                livery.EditorConfigure(id, price, i == 0, topDown, parked);
                EditorUtility.SetDirty(livery);
                liveries[i] = livery;
            }

            var catalog = AssetDatabase.LoadAssetAtPath<LiveryCatalogSO>(CatalogPath);

            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<LiveryCatalogSO>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.EditorSetLiveries(liveries);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        /// <summary>The parked plane stands on its wheels, so its pivot is at its feet like the Home props.</summary>
        private static Sprite ImportFooted(string path)
        {
            SpriteImport.Import(path, HomePixelsPerUnit);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(0.5f, 0f);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void LinkSnackArt()
        {
            var economy = AssetDatabase.LoadAssetAtPath<EconomyConfigSO>(EconomyPath);

            if (economy == null)
            {
                return;
            }

            for (int i = 0; i < economy.SnackPacks.Count && i < SnackArt.Length; i++)
            {
                economy.SnackPacks[i].EditorSetArtwork(UiKitInstaller.ImportKitSprite(UiKitInstaller.KitFolder + "/" + SnackArt[i] + ".png"));
            }

            EditorUtility.SetDirty(economy);
        }
    }
}
