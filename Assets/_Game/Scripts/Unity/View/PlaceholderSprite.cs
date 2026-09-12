#if UNITY_EDITOR
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// A plain white rounded square, generated once, used only while the real art is
    /// missing. Every board colour still comes from <see cref="ThemeSO"/>; this supplies
    /// nothing but a shape, so a themed build that assigns real sprites never touches it.
    /// </summary>
    public static class PlaceholderSprite
    {
        private const int Size = 32;
        private const int CornerRadius = 7;
        private const float PixelsPerUnit = 32f;

        private static Sprite cached;
        private static Sprite cachedDot;

        public static Sprite RoundedSquare
        {
            get
            {
                if (cached == null)
                {
                    cached = Build();
                }

                return cached;
            }
        }

        /// <summary>A soft round dot, used for the spark that leads a new segment.</summary>
        public static Sprite SoftDot
        {
            get
            {
                if (cachedDot == null)
                {
                    cachedDot = BuildDot();
                }

                return cachedDot;
            }
        }

        private static Sprite BuildDot()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "PlaceholderDot"
            };

            var pixels = new Color32[size * size];
            float centre = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - centre) / centre;
                    float dy = (y - centre) / centre;
                    float alpha = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));

                    // Squared falloff keeps a bright core with no visible rim.
                    alpha *= alpha;
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size,
                0,
                SpriteMeshType.FullRect);
        }

        private static Sprite Build()
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, mipChain: false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "PlaceholderCell"
            };

            var pixels = new Color32[Size * Size];

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    pixels[y * Size + x] = IsInside(x, y) ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);

            // A sliced border lets one small texture stretch to any cell size without the
            // corners smearing.
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, Size, Size),
                new Vector2(0.5f, 0.5f),
                PixelsPerUnit,
                0,
                SpriteMeshType.FullRect,
                new Vector4(CornerRadius, CornerRadius, CornerRadius, CornerRadius));
        }

        /// <summary>Rounded corner test: outside the corner circles the pixel is transparent.</summary>
        private static bool IsInside(int x, int y)
        {
            float cx = Mathf.Clamp(x + 0.5f, CornerRadius, Size - CornerRadius);
            float cy = Mathf.Clamp(y + 0.5f, CornerRadius, Size - CornerRadius);
            float dx = x + 0.5f - cx;
            float dy = y + 0.5f - cy;
            return dx * dx + dy * dy <= CornerRadius * CornerRadius;
        }
    }
}
#endif
