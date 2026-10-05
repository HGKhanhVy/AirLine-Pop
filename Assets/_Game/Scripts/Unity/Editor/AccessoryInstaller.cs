using UnityEditor;
using UnityEngine;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Creates the accessories regulars unlock with loyalty and their catalog, from the art
    /// Tools/flat_art/accessories.py draws. Accessories import at the cats' own drawing scale:
    /// hats pivoted at the bottom so they rest on the head, neckwear at the middle so it sits
    /// centred under the chin. Safe to rerun.
    /// </summary>
    public static class AccessoryInstaller
    {
        public const string CatalogPath = "Assets/_Game/Config/AccessoryCatalog.asset";
        public const string GiftBoxArt = ArtFolder + "/gift_box.png";
        private const string ArtFolder = "Assets/_Game/Art/Accessories";
        private const string AssetFolder = "Assets/_Game/Config/Accessories";

        // The cat drawings' own pixels per unit, so an accessory drawn at head size fits a head.
        private const float CatPixelsPerUnit = 390f;

        // In wardrobe order: id, art, where it is worn, loyalty card that unlocks it, size on the cat.
        private static readonly (string id, string art, AccessorySlot slot, int tier, float scale)[] Items =
        {
            ("bow", "acc_bow", AccessorySlot.Neck, 1, 0.5f),
            ("pilot_cap", "acc_pilot_cap", AccessorySlot.Head, 2, 0.95f),
            ("flower_crown", "acc_flower_crown", AccessorySlot.Head, 3, 0.85f),
            ("crown", "acc_crown", AccessorySlot.Head, 4, 0.9f),
        };

        [MenuItem("Tools/AirLine Pop/Install Accessories")]
        public static void InstallFromMenu()
        {
            Debug.Log(Install());
        }

        public static string Install()
        {
            AssetWriter.EnsureFolder(AssetFolder);
            var accessories = new AccessorySO[Items.Length];

            for (int i = 0; i < Items.Length; i++)
            {
                (string id, string art, AccessorySlot slot, int tier, float scale) = Items[i];
                SpriteAlignment pivot = slot == AccessorySlot.Head ? SpriteAlignment.BottomCenter : SpriteAlignment.Center;
                Sprite sprite = SpriteImport.Import(ArtFolder + "/" + art + ".png", CatPixelsPerUnit, pivot);
                string path = AssetFolder + "/Accessory_" + id + ".asset";
                var accessory = AssetDatabase.LoadAssetAtPath<AccessorySO>(path);

                if (accessory == null)
                {
                    accessory = ScriptableObject.CreateInstance<AccessorySO>();
                    AssetDatabase.CreateAsset(accessory, path);
                }

                accessory.EditorConfigure(id, "accessory." + id, sprite, sprite, slot, tier, scale);
                EditorUtility.SetDirty(accessory);
                accessories[i] = accessory;
            }

            var catalog = AssetDatabase.LoadAssetAtPath<AccessoryCatalogSO>(CatalogPath);

            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<AccessoryCatalogSO>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.EditorSetAccessories(accessories);
            EditorUtility.SetDirty(catalog);
            SpriteImport.Import(GiftBoxArt, 250f);
            AssetDatabase.SaveAssets();
            return "Accessories: " + accessories.Length + " installed.";
        }
    }
}
