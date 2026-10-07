using UnityEditor;
using UnityEngine;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Dresses the VIP guest as His Majesty: a crimson cape over his yellow robe, hung from
    /// his neck behind his body, so it rides every drawing the way a bow does and only its
    /// shoulders, sides and hem show. The cape's art is drawn around the neck point, so it
    /// imports with a centre pivot; a collar over the front joins its two sides. The yellow robe and red crown band are painted into his drawings by
    /// Tools/flat_art/import_craftpix_cats.py; the cape's art comes from accessories.py.
    /// <see cref="FlatCatBuilder"/> calls the same step when it bakes the guest. Safe to rerun.
    /// </summary>
    public static class VipRobeInstaller
    {
        public const string GuestId = "vip";

        private const string PrefabPath = "Assets/_Game/Art/FlatCats/CatFlat_" + GuestId + ".prefab";
        private const string ArtFolder = "Assets/_Game/Art/Accessories/";
        private const string AssetFolder = "Assets/_Game/Config/Accessories/";

        // The two pieces of the cape: the back, hidden by the body but for its shoulders,
        // sides and hem, and the collar across the front that joins them. Each is drawn on
        // a canvas centred on the neck point, so both import with a centre pivot.
        // Name, art, order against the body (behind or over).
        private static readonly (string name, string art, int order)[] Pieces =
        {
            ("Cape", "royal_cape", -1),
            ("Collar", "royal_collar", 2),
        };

        // The cat drawings' own pixels per unit, so the cape is drawn at the cat's scale.
        private const float CatPixelsPerUnit = 390f;

        [MenuItem("Tools/AirLine Pop/Dress VIP Guest")]
        public static void InstallFromMenu()
        {
            Debug.Log(Install());
        }

        public static string Install()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);

            try
            {
                if (!Dress(root))
                {
                    return "The VIP guest, its cape art or the head anchors are missing.";
                }

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                return "VIP guest dressed in his robe.";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>Hangs the cape on a baked guest, replacing one from an earlier run.</summary>
        public static bool Dress(GameObject root)
        {
            Transform body = root.transform.Find("Visual/Body");
            var anchors = AssetDatabase.LoadAssetAtPath<CatHeadAnchorsSO>(CatHeadAnchorBaker.AssetPath);

            if (body == null || anchors == null)
            {
                return false;
            }

            var bodyRenderer = body.GetComponent<SpriteRenderer>();

            foreach ((string name, string art, int order) in Pieces)
            {
                AccessorySO piece = Piece(art);

                if (piece == null)
                {
                    return false;
                }

                Hang(body, bodyRenderer, anchors, name, order, piece);
            }

            return true;
        }

        private static void Hang(Transform body, SpriteRenderer bodyRenderer, CatHeadAnchorsSO anchors, string name, int order, AccessorySO piece)
        {
            Transform old = body.Find(name);

            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            var holder = new GameObject(name);
            holder.transform.SetParent(body, false);
            var renderer = holder.AddComponent<SpriteRenderer>();
            renderer.sortingLayerID = bodyRenderer.sortingLayerID;
            renderer.sortingOrder = bodyRenderer.sortingOrder + order;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.enabled = false;

            CatAccessoryView view = holder.AddComponent<CatAccessoryView>();
            view.EditorLink(bodyRenderer, renderer, anchors);
            view.EditorLinkWornFromStart(piece);
        }

        /// <summary>A piece of the cape as an accessory worn at the neck; it is not in the wardrobe, only His Majesty wears it.</summary>
        private static AccessorySO Piece(string art)
        {
            Sprite sprite = SpriteImport.Import(ArtFolder + "acc_" + art + ".png", CatPixelsPerUnit, SpriteAlignment.Center);

            if (sprite == null)
            {
                return null;
            }

            string path = AssetFolder + "Accessory_" + art + ".asset";
            var piece = AssetDatabase.LoadAssetAtPath<AccessorySO>(path);

            if (piece == null)
            {
                piece = ScriptableObject.CreateInstance<AccessorySO>();
                AssetDatabase.CreateAsset(piece, path);
            }

            piece.EditorConfigure(art, "accessory." + art, sprite, sprite, AccessorySlot.Neck, 0, 1f);
            EditorUtility.SetDirty(piece);
            AssetDatabase.SaveAssetIfDirty(piece);
            return piece;
        }
    }
}
