using System.IO;
using UnityEditor;
using UnityEngine;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Gives every flat cat prefab a slot for an accessory on its head: a sprite drawn just
    /// over the body, moved by <see cref="CatAccessoryView"/> to the head of whatever drawing
    /// the body shows. <see cref="FlatCatBuilder"/> calls the same step when it bakes a cat.
    /// Safe to rerun.
    /// </summary>
    public static class CatAccessoryInstaller
    {
        private const string CatFolder = "Assets/_Game/Art/FlatCats";
        private const string SlotName = "Accessory";

        [MenuItem("Tools/AirLine Pop/Install Cat Accessory Slots")]
        public static void InstallFromMenu()
        {
            Debug.Log(Install());
        }

        public static string Install()
        {
            var anchors = AssetDatabase.LoadAssetAtPath<CatHeadAnchorsSO>(CatHeadAnchorBaker.AssetPath);

            if (anchors == null)
            {
                return "Bake the cat head anchors first.";
            }

            int fitted = 0;

            foreach (string path in Directory.GetFiles(CatFolder, "CatFlat_*.prefab"))
            {
                string assetPath = path.Replace('\\', '/');
                GameObject root = PrefabUtility.LoadPrefabContents(assetPath);

                try
                {
                    Transform body = root.transform.Find("Visual/Body");

                    if (body == null)
                    {
                        continue;
                    }

                    AddTo(root, body.GetComponent<SpriteRenderer>(), anchors);
                    PrefabUtility.SaveAsPrefabAsset(root, assetPath);
                    fitted++;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            return "Accessory slots fitted to " + fitted + " cats.";
        }

        /// <summary>Adds (or rebuilds) the accessory slot on one cat and links it to the cat's view.</summary>
        public static void AddTo(GameObject root, SpriteRenderer body, CatHeadAnchorsSO anchors)
        {
            Transform old = body.transform.Find(SlotName);

            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            var slot = new GameObject(SlotName);
            slot.transform.SetParent(body.transform, false);
            var renderer = slot.AddComponent<SpriteRenderer>();
            renderer.sortingLayerID = body.sortingLayerID;
            renderer.sortingOrder = body.sortingOrder + 1;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.enabled = false;

            CatAccessoryView view = AssetWriter.GetOrAdd<CatAccessoryView>(root);
            view.EditorLink(body, renderer, anchors);
            root.GetComponent<CatView>().EditorLinkAccessory(view);
        }
    }
}
