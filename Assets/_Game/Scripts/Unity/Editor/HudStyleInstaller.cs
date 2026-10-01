using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Restyles the gameplay HUD as chunky game buttons: a glossy blue face on a dark lip,
    /// square at the top of the screen and round at the bottom, with a white icon on top,
    /// and bold white labels with a navy outline and drop shadow.
    ///
    /// The old button art has the icon painted into a translucent grey tile. The icon is
    /// the opaque part, so it is lifted out by its alpha, whitened, and set on the new face
    /// as a child image; the bulb on the hint button keeps its yellow. Only looks change:
    /// the buttons, their listeners and their animations stay as they are. Rerunnable:
    /// icons are always lifted from the original art listed here, never from a restyle.
    /// </summary>
    public static class HudStyleInstaller
    {
        private const string HudPrefabPath = "Assets/_Game/UI/GameplayHudCanvas.prefab";
        private const string ArtFolder = "Assets/_Game/Art/UI";
        private const string SquareButtonPath = ArtFolder + "/btn_square.png";
        private const string RoundButtonPath = ArtFolder + "/btn_round.png";
        private const string IconPathFormat = ArtFolder + "/icon_{0}.png";
        private const string LabelMaterialPath = ArtFolder + "/mat_hud_label.mat";

        private const string IconObjectName = "Icon";
        private const string LevelPillName = "LevelPill";
        private const string BottomBarName = "Bottom";

        // Buttons whose icon keeps its own colours rather than going white.
        private static readonly string[] ColouredIcons = { "Hint" };

        // The original art of each button, which its icon is lifted from.
        private const string OldArtFolder = "Assets/_Game/Asset_Resources/";
        private static readonly (string button, string path)[] SourceArt =
        {
            ("Settings", OldArtFolder + "Texture2D/setting.png"),
            ("Info", OldArtFolder + "Texture2D/info.png"),
            ("Skip", OldArtFolder + "Texture2D/skip-(1).png"),
            ("Store", OldArtFolder + "Texture2D/StoreButton.png"),
            ("Hint", OldArtFolder + "Texture2D/hint (1).png"),
            ("Restart", OldArtFolder + "Sprite/unnamed_0.asset")
        };

        private const int ButtonTextureSize = 160;
        private const float SquareCorner = 38f;
        private const float IconShare = 0.62f;

        // Round buttons carry bigger art with more room round it, so their icons are drawn larger.
        private const float RoundIconShare = 0.78f;

        private const float TitleSize = 56f;
        private const float PixelsPerUnit = 100f;

        // Old tiles are about 40% opaque and their icons 70% or more, so everything well
        // above the tile counts as icon.
        private const float TileAlpha = 0.44f;
        private const float IconAlphaRange = 0.24f;

        private static readonly ProceduralTextures.ButtonColors Blue = new ProceduralTextures.ButtonColors(
            top: new Color(0.42f, 0.78f, 1f),
            bottom: new Color(0.13f, 0.45f, 0.9f),
            lip: new Color(0.07f, 0.26f, 0.6f),
            outline: new Color(0.04f, 0.15f, 0.38f));

        private static readonly Color Navy = new Color(0.03f, 0.12f, 0.32f);
        private static readonly Color PillTint = new Color(0.03f, 0.12f, 0.32f, 0.55f);

        public static string Install()
        {
            AssetWriter.EnsureFolder(ArtFolder);
            Sprite square = ProceduralTextures.WriteGameButton(SquareButtonPath, ButtonTextureSize, SquareCorner, Blue, PixelsPerUnit);
            Sprite round = ProceduralTextures.WriteGameButton(RoundButtonPath, ButtonTextureSize, ButtonTextureSize * 0.5f - 1f, Blue, PixelsPerUnit);

            GameObject contents = PrefabUtility.LoadPrefabContents(HudPrefabPath);

            try
            {
                int restyled = 0;

                foreach (Button button in contents.GetComponentsInChildren<Button>(true))
                {
                    if (button.targetGraphic is Image face)
                    {
                        bool isRound = button.transform.parent != null && button.transform.parent.name == BottomBarName;
                        Restyle(button, face, isRound ? round : square, isRound ? RoundIconShare : IconShare);
                        restyled++;
                    }
                }

                StyleLabels(contents);
                PrefabUtility.SaveAsPrefabAsset(contents, HudPrefabPath);
                AssetDatabase.SaveAssets();
                return HudPrefabPath + ": " + restyled + " buttons restyled, labels outlined.";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void Restyle(Button button, Image face, Sprite background, float iconShare)
        {
            Sprite source = LoadSourceArt(button.name);

            if (source == null)
            {
                return;
            }

            Transform iconTransform = button.transform.Find(IconObjectName);
            Image icon = iconTransform != null ? iconTransform.GetComponent<Image>() : null;

            bool keepsColour = System.Array.IndexOf(ColouredIcons, button.name) >= 0;
            Sprite extracted = ExtractIcon(source, button.name, keepsColour);

            face.sprite = background;
            face.type = Image.Type.Simple;
            face.color = Color.white;
            face.preserveAspect = true;

            if (icon == null)
            {
                var iconObject = new GameObject(IconObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconObject.transform.SetParent(button.transform, false);

                // Under any badge or label already on the button.
                iconObject.transform.SetAsFirstSibling();
                icon = iconObject.GetComponent<Image>();
            }

            var rect = (RectTransform)icon.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            // Nudged up, onto the face and off the lip.
            var size = ((RectTransform)button.transform).rect.size;
            rect.sizeDelta = size * iconShare;
            rect.anchoredPosition = new Vector2(0f, size.y * 0.03f);

            icon.sprite = extracted;
            icon.color = Color.white;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            EditorUtility.SetDirty(icon);
        }

        private static Sprite LoadSourceArt(string buttonName)
        {
            foreach (var (button, path) in SourceArt)
            {
                if (button != buttonName)
                {
                    continue;
                }

                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is Sprite sprite)
                    {
                        return sprite;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Lifts the icon out of an old button tile. The tile is drawn through a render
        /// texture, because the source may not be readable, and each pixel is kept in
        /// proportion to how far it stands above the tile's own opacity.
        /// </summary>
        private static Sprite ExtractIcon(Sprite source, string name, bool keepsColour)
        {
            Texture2D texture = source.texture;
            Rect area = source.textureRect;
            var target = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture previous = RenderTexture.active;

            Graphics.Blit(texture, target);
            RenderTexture.active = target;
            var copy = new Texture2D((int)area.width, (int)area.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(area, 0, 0);
            copy.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);

            Color[] pixels = copy.GetPixels();

            for (int i = 0; i < pixels.Length; i++)
            {
                Color pixel = pixels[i];
                float mask = Mathf.Clamp01((pixel.a - TileAlpha) / IconAlphaRange);
                pixels[i] = keepsColour ? new Color(pixel.r, pixel.g, pixel.b, mask) : new Color(1f, 1f, 1f, mask);
            }

            copy.SetPixels(pixels);
            string path = string.Format(IconPathFormat, name.ToLowerInvariant());
            System.IO.File.WriteAllBytes(path, copy.EncodeToPNG());
            Object.DestroyImmediate(copy);

            return SpriteImport.Import(path, PixelsPerUnit);
        }

        /// <summary>The level title and the other labels: white, bold, with a navy outline and a soft shadow.</summary>
        private static void StyleLabels(GameObject contents)
        {
            TMP_Text any = contents.GetComponentInChildren<TMP_Text>(true);

            if (any == null)
            {
                return;
            }

            var material = new Material(any.fontSharedMaterial);
            material.EnableKeyword(ShaderUtilities.Keyword_Outline);
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.22f);
            material.SetColor(ShaderUtilities.ID_OutlineColor, Navy);
            material.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            material.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(Navy.r, Navy.g, Navy.b, 0.7f));
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.7f);
            material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.25f);
            material.SetFloat(ShaderUtilities.ID_FaceDilate, 0.12f);
            material = AssetWriter.SaveMaterial(LabelMaterialPath, material);

            foreach (TMP_Text label in contents.GetComponentsInChildren<TMP_Text>(true))
            {
                // Coin counts keep their gold.
                if (label.color.b > label.color.r * 0.6f)
                {
                    label.color = Color.white;
                }

                label.fontSharedMaterial = material;
                EditorUtility.SetDirty(label);
            }

            Transform pill = FindDeep(contents.transform, LevelPillName);

            if (pill == null)
            {
                return;
            }

            if (pill.TryGetComponent(out Image pillImage))
            {
                pillImage.color = PillTint;
            }

            // The level title is the one label that should carry across the screen.
            foreach (TMP_Text title in pill.GetComponentsInChildren<TMP_Text>(true))
            {
                title.fontSize = TitleSize;
                title.fontStyle = FontStyles.Bold;
            }
        }

        private static Transform FindDeep(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                {
                    return child;
                }

                Transform found = FindDeep(child, name);

                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
