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
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);

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
