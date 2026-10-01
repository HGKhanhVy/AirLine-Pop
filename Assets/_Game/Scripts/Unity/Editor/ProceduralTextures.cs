using UnityEditor;
using UnityEngine;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>Small textures drawn in code and saved as PNG sprites.</summary>
    public static class ProceduralTextures
    {
        /// <summary>
        /// A band round the foot of a block on the water, such as surf or a soft shadow:
        /// <paramref name="near"/> hugging the block's rounded outline, blending into
        /// <paramref name="far"/> and fading out into the sea.
        /// </summary>
        /// <param name="blockShare">The block's width as a share of the sprite's width.</param>
        /// <param name="cornerShare">The block's corner radius as a share of the sprite's width.</param>
        public static Sprite WriteShore(string path, int size, float blockShare, float cornerShare,
            Color near, Color far, float pixelsPerUnit)
        {
            var pixels = new Color32[size * size];
            float halfBlock = blockShare * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var point = new Vector2((x + 0.5f) / size - 0.5f, (y + 0.5f) / size - 0.5f);
                    float outside = RoundedSquareDistance(point, halfBlock, cornerShare);
                    pixels[y * size + x] = ShoreColor(outside, near, far);
                }
            }

            Write(path, pixels, size, size);
            return ImportSprite(path, pixelsPerUnit);
        }

        /// <summary>The colours of a chunky game button.</summary>
        public readonly struct ButtonColors
        {
            public ButtonColors(Color top, Color bottom, Color lip, Color outline)
            {
                Top = top;
                Bottom = bottom;
                Lip = lip;
                Outline = outline;
            }

            public Color Top { get; }
            public Color Bottom { get; }
            public Color Lip { get; }
            public Color Outline { get; }
        }

        /// <summary>
        /// A chunky game button: a dark outline, a face shaded from light at the top to deep
        /// at the bottom with a soft gloss across its upper half, sitting on a darker lip
        /// that makes it look pressed out of the screen. Round when <paramref name="cornerRadius"/>
        /// is half the size.
        /// </summary>
        public static Sprite WriteGameButton(string path, int size, float cornerRadius, ButtonColors colors, float pixelsPerUnit)
        {
            const float outline = 5f;
            const float lip = 9f;
            const float gloss = 0.22f;

            var pixels = new Color32[size * size];
            float half = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var point = new Vector2(x + 0.5f - half, y + 0.5f - half);
                    float shape = RoundedSquareDistance(point, half - 1f, cornerRadius);

                    // The face is the same shape lifted and shrunk by the lip, so the lip
                    // shows only along the bottom edge.
                    var facePoint = new Vector2(point.x, point.y - lip * 0.5f);
                    float face = RoundedSquareDistance(facePoint, half - 1f - outline - lip * 0.5f, Mathf.Max(1f, cornerRadius - outline));

                    Color color;

                    if (shape > -outline)
                    {
                        color = colors.Outline;
                    }
                    else if (face > 0f)
                    {
                        color = colors.Lip;
                    }
                    else
                    {
                        float t = Mathf.InverseLerp(-half, half, point.y);
                        color = Color.Lerp(colors.Bottom, colors.Top, t);

                        // A gloss band across the upper part of the face, fading downwards.
                        float shine = Mathf.SmoothStep(0.45f, 0.85f, t) * gloss * Mathf.Clamp01(-face / 6f);
                        color = Color.Lerp(color, Color.white, shine);

                        // A thin inner edge where the face meets the outline.
                        color = Color.Lerp(color, colors.Bottom, Mathf.Clamp01(1f - (-face) / 3f) * 0.35f);
                    }

                    color.a = Mathf.Clamp01(0.5f - shape);
                    pixels[y * size + x] = color;
                }
            }

            Write(path, pixels, size, size);
            return ImportSprite(path, pixelsPerUnit);
        }

        /// <summary>A dark rim fading to clear in the middle, for a soft vignette over the view.</summary>
        public static Sprite WriteVignette(string path, int size, Color rim, float clearRadius, float pixelsPerUnit)
        {
            var pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var point = new Vector2((x + 0.5f) / size - 0.5f, (y + 0.5f) / size - 0.5f) * 2f;
                    float t = Mathf.InverseLerp(clearRadius, 1.45f, point.magnitude);
                    var color = rim;
                    color.a = rim.a * t * t;
                    pixels[y * size + x] = color;
                }
            }

            Write(path, pixels, size, size);
            return ImportSprite(path, pixelsPerUnit);
        }

        /// <summary>
        /// Seamless greyscale grain averaging mid grey: a few octaves of value noise, from
        /// broad patches down to fine speckle, like grass or sand seen from above. Imported
        /// as linear data, so the shader reads the values exactly as written.
        /// </summary>
        public static Texture2D WriteDetail(string path, int size, int seed)
        {
            var octaves = new (int cells, float weight)[] { (4, 0.3f), (8, 0.3f), (24, 0.25f), (64, 0.15f) };
            var lattices = new float[octaves.Length][,];
            var random = new System.Random(seed);

            for (int o = 0; o < octaves.Length; o++)
            {
                lattices[o] = RandomLattice(octaves[o].cells, random);
            }

            var values = new float[size * size];
            float min = float.MaxValue;
            float max = float.MinValue;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float value = 0f;

                    for (int o = 0; o < octaves.Length; o++)
                    {
                        value += TileableValueNoise(lattices[o], octaves[o].cells, x / (float)size, y / (float)size) * octaves[o].weight;
                    }

                    values[y * size + x] = value;
                    min = Mathf.Min(min, value);
                    max = Mathf.Max(max, value);
                }
            }

            var pixels = new Color32[size * size];

            for (int i = 0; i < values.Length; i++)
            {
                var grey = (byte)Mathf.RoundToInt(Mathf.InverseLerp(min, max, values[i]) * 255f);
                pixels[i] = new Color32(grey, grey, grey, 255);
            }

            Write(path, pixels, size, size);
            return ImportLinear(path);
        }

        private static float[,] RandomLattice(int cells, System.Random random)
        {
            var lattice = new float[cells, cells];

            for (int y = 0; y < cells; y++)
            {
                for (int x = 0; x < cells; x++)
                {
                    lattice[x, y] = (float)random.NextDouble();
                }
            }

            return lattice;
        }

        private static Texture2D ImportLinear(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Smoothly interpolated random values on a lattice that wraps round, so the texture tiles.</summary>
        private static float TileableValueNoise(float[,] lattice, int cells, float u, float v)
        {
            float x = u * cells;
            float y = v * cells;
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            float tx = Mathf.SmoothStep(0f, 1f, x - x0);
            float ty = Mathf.SmoothStep(0f, 1f, y - y0);
            int x1 = (x0 + 1) % cells;
            int y1 = (y0 + 1) % cells;
            x0 %= cells;
            y0 %= cells;

            float bottom = Mathf.Lerp(lattice[x0, y0], lattice[x1, y0], tx);
            float top = Mathf.Lerp(lattice[x0, y1], lattice[x1, y1], tx);
            return Mathf.Lerp(bottom, top, ty);
        }

        private static Color ShoreColor(float outside, Color near, Color far)
        {
            const float nearWidth = 0.018f;
            const float fadeWidth = 0.16f;

            if (outside < nearWidth)
            {
                // Solid under the block, where it never shows, so the band has no gap at its foot.
                return near;
            }

            float fade = Mathf.Clamp01((outside - nearWidth) / fadeWidth);
            Color color = Color.Lerp(near, far, Mathf.SmoothStep(0f, 1f, fade * 3f));
            color.a = Mathf.Lerp(near.a, 0f, fade) * (1f - fade * 0.3f);
            return color;
        }

        /// <summary>Signed distance from a point to a rounded square centred on the origin; negative inside.</summary>
        private static float RoundedSquareDistance(Vector2 point, float halfSize, float radius)
        {
            float px = Mathf.Abs(point.x) - (halfSize - radius);
            float py = Mathf.Abs(point.y) - (halfSize - radius);
            float outside = new Vector2(Mathf.Max(px, 0f), Mathf.Max(py, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(px, py), 0f) - radius;
        }

        private static void Write(string path, Color32[] pixels, int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        private static Sprite ImportSprite(string path, float pixelsPerUnit)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
