using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Measures the top of the head and the neck in every cat drawing and writes them to one
    /// <see cref="CatHeadAnchorsSO"/>, so an accessory can ride the cat frame by frame.
    ///
    /// The head is found from the drawing itself: the top rows of the silhouette give the
    /// head's middle (a tail sticking out sideways would throw off the whole outline's), and
    /// the highest point in a narrow band there is the crown between the ears, where a hat
    /// rests. The neck is found from the body instead, so a cat's own cap cannot throw it off:
    /// the middle of the lower body and the bottom of the feet, which move with the cat
    /// through a hop, then a fixed step up to just under the chin, the same for every cat
    /// because they share one skeleton and one drawing scale. Rolled-over drawings are marked
    /// as having nowhere to wear anything. Safe to rerun.
    /// </summary>
    public static class CatHeadAnchorBaker
    {
        public const string AssetPath = "Assets/_Game/Config/CatHeadAnchors.asset";
        private const string MotionFolder = "Assets/_Game/Art/FlatCats/Motion";

        // Share of the drawing's height, from the topmost pixel down, treated as the head.
        private const float HeadDepth = 0.22f;

        // Half width of the band the crown is looked for in, as a share of the drawing's width.
        private const float CrownBand = 0.08f;

        private const byte Solid = 128;

        // The lower body band the neck is measured from, as shares of the drawing's height up from the feet.
        private const float BodyBandLow = 0.15f;
        private const float BodyBandHigh = 0.45f;

        // From the middle of that band and the bottom of the feet to just under the chin, in drawing pixels.
        private static readonly Vector2 NeckStep = new Vector2(-40f, 77f);

        [MenuItem("Tools/AirLine Pop/Bake Cat Head Anchors")]
        public static void BakeFromMenu()
        {
            Debug.Log(Bake());
        }

        public static string Bake()
        {
            var sprites = new List<Sprite>();
            var heads = new List<Vector2>();
            var necks = new List<Vector2>();
            var upright = new List<bool>();

            foreach (string path in Directory.GetFiles(MotionFolder, "*.png"))
            {
                string assetPath = path.Replace('\\', '/');
                var pixels = new Texture2D(2, 2, TextureFormat.RGBA32, false);

                if (!pixels.LoadImage(File.ReadAllBytes(assetPath)))
                {
                    continue;
                }

                Color32[] colours = pixels.GetPixels32();

                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
                {
                    if (!(asset is Sprite sprite))
                    {
                        continue;
                    }

                    sprites.Add(sprite);
                    upright.Add(!sprite.name.Contains("_Roll"));
                    heads.Add(MeasureHead(sprite, colours, pixels.width));
                    necks.Add(MeasureNeck(sprite, colours, pixels.width));
                }

                Object.DestroyImmediate(pixels);
            }

            var anchors = AssetDatabase.LoadAssetAtPath<CatHeadAnchorsSO>(AssetPath);

            if (anchors == null)
            {
                anchors = ScriptableObject.CreateInstance<CatHeadAnchorsSO>();
                AssetDatabase.CreateAsset(anchors, AssetPath);
            }

            anchors.EditorSet(sprites.ToArray(), heads.ToArray(), necks.ToArray(), upright.ToArray());
            EditorUtility.SetDirty(anchors);
            AssetDatabase.SaveAssets();
            return "Cat head anchors: " + sprites.Count + " drawings measured.";
        }

        /// <summary>Just under the chin, in the sprite's own units, measured from its pivot.</summary>
        private static Vector2 MeasureNeck(Sprite sprite, Color32[] colours, int textureWidth)
        {
            Rect rect = sprite.rect;
            int x0 = (int)rect.x;
            int y0 = (int)rect.y;
            int width = (int)rect.width;
            int height = (int)rect.height;
            int top = -1;
            int bottom = -1;

            for (int y = 0; y < height; y++)
            {
                if (!RowHasSolid(colours, textureWidth, x0, y0 + y, width))
                {
                    continue;
                }

                if (bottom < 0)
                {
                    bottom = y;
                }

                top = y;
            }

            if (top < 0)
            {
                return Vector2.zero;
            }

            int span = top - bottom;
            long sum = 0;
            int count = 0;

            for (int y = bottom + (int)(span * BodyBandLow); y < bottom + (int)(span * BodyBandHigh); y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (colours[(y0 + y) * textureWidth + x0 + x].a >= Solid)
                    {
                        sum += x;
                        count++;
                    }
                }
            }

            float middle = count > 0 ? (float)sum / count : width * 0.5f;
            float ppu = sprite.pixelsPerUnit;
            return new Vector2((middle + NeckStep.x - sprite.pivot.x) / ppu, (bottom + NeckStep.y - sprite.pivot.y) / ppu);
        }

        private static bool RowHasSolid(Color32[] colours, int textureWidth, int x0, int row, int width)
        {
            for (int x = 0; x < width; x++)
            {
                if (colours[row * textureWidth + x0 + x].a >= Solid)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The crown of the head in the sprite's own units, measured from its pivot.</summary>
        private static Vector2 MeasureHead(Sprite sprite, Color32[] colours, int textureWidth)
        {
            Rect rect = sprite.rect;
            int x0 = (int)rect.x;
            int y0 = (int)rect.y;
            int width = (int)rect.width;
            int height = (int)rect.height;

            int top = -1;

            for (int y = height - 1; y >= 0 && top < 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    if (colours[(y0 + y) * textureWidth + x0 + x].a >= Solid)
                    {
                        top = y;
                        break;
                    }
                }
            }

            if (top < 0)
            {
                return Vector2.zero;
            }

            // The head's middle: the average column of everything solid in its top rows.
            int headBottom = Mathf.Max(0, top - (int)(height * HeadDepth));
            long sum = 0;
            int count = 0;

            for (int y = headBottom; y <= top; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (colours[(y0 + y) * textureWidth + x0 + x].a >= Solid)
                    {
                        sum += x;
                        count++;
                    }
                }
            }

            int centre = count > 0 ? (int)(sum / count) : width / 2;
            int band = Mathf.Max(2, (int)(width * CrownBand));
            int crown = headBottom;

            for (int y = top; y >= headBottom; y--)
            {
                bool found = false;

                for (int x = Mathf.Max(0, centre - band); x <= Mathf.Min(width - 1, centre + band); x++)
                {
                    if (colours[(y0 + y) * textureWidth + x0 + x].a >= Solid)
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    crown = y;
                    break;
                }
            }

            float ppu = sprite.pixelsPerUnit;
            return new Vector2((centre - sprite.pivot.x) / ppu, (crown - sprite.pivot.y) / ppu);
        }
    }
}
