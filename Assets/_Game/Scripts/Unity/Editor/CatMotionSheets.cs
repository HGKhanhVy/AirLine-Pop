using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Imports the frame-by-frame cats from import_craftpix_cats.py: each sheet is
    /// cut into equal frames that share one pivot, the point where the cat stands.
    /// </summary>
    public static class CatMotionSheets
    {
        public const string Folder = "Assets/_Game/Art/FlatCats/Motion";

        private const string MetaPath = Folder + "/cat_motion.json";

        public static CatMotionMeta LoadMeta()
        {
            if (!File.Exists(MetaPath))
            {
                return null;
            }

            return JsonUtility.FromJson<CatMotionMeta>(File.ReadAllText(MetaPath));
        }

        public static string SheetPath(string breedId, string clipName)
        {
            return Folder + "/cat_" + breedId + "_" + clipName + ".png";
        }

        /// <summary>The clip's frames in order, or null when the breed has no sheet for it.</summary>
        public static Sprite[] Import(string breedId, CatMotionClipMeta clip, CatMotionMeta meta)
        {
            string path = SheetPath(breedId, clip.name);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
            {
                return null;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = meta.pixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();

            Slice(importer, breedId, clip, meta);

            string prefix = FramePrefix(breedId, clip.name);
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .Where(sprite => sprite.name.StartsWith(prefix))
                .OrderBy(sprite => sprite.name)
                .ToArray();
        }

        private static void Slice(TextureImporter importer, string breedId, CatMotionClipMeta clip, CatMotionMeta meta)
        {
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            var pivot = new Vector2(meta.pivotX / meta.frameWidth, meta.pivotY / meta.frameHeight);
            var rects = new SpriteRect[clip.count];

            for (int i = 0; i < clip.count; i++)
            {
                int column = i % clip.columns;
                int row = i / clip.columns;

                // Sheets are laid out from the top; sprite rects count from the bottom.
                rects[i] = new SpriteRect
                {
                    name = FramePrefix(breedId, clip.name) + i.ToString("00"),
                    spriteID = GUID.Generate(),
                    rect = new Rect(column * meta.frameWidth, height - (row + 1) * meta.frameHeight, meta.frameWidth, meta.frameHeight),
                    alignment = SpriteAlignment.Custom,
                    pivot = pivot,
                };
            }

            provider.SetSpriteRects(rects);

            var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            names?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));

            provider.Apply();
            importer.SaveAndReimport();
        }

        private static string FramePrefix(string breedId, string clipName)
        {
            return "cat_" + breedId + "_" + clipName + "_";
        }
    }
}
