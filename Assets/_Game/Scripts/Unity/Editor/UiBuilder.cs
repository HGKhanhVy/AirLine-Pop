using Crystal;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Small factory for UI in the AirLine Pop kit, so screen builders read as layout
    /// rather than component plumbing. Every element uses the generated kit sprites and the
    /// Baloo 2 fonts from <see cref="UiKitInstaller"/>.
    /// </summary>
    public static class UiBuilder
    {
        public static readonly Color TextBrown = new Color32(74, 46, 28, 255);
        public static readonly Color TextCream = new Color32(255, 253, 247, 255);

        // Glyphs on cream buttons: the deep sky blue of the kit's secondary buttons.
        public static readonly Color IconBlue = new Color32(70, 140, 196, 255);
        public static readonly Color SkyBlue = new Color32(112, 186, 230, 255);

        // Behind a popup: a warm dark wash, deep enough that the screen underneath steps
        // back and cannot be mistaken for part of the popup.
        public static readonly Color ModalDim = new Color32(46, 30, 34, 155);

        // Behind a sheet that stays tied to what is under it, such as the cat menu.
        public static readonly Color SheetDim = new Color32(46, 30, 34, 80);

        // Glyphs that are present but not in focus, such as idle tabs: a soft warm brown.
        public static readonly Color IconIdle = new Color32(176, 146, 132, 255);

        public static Sprite Sprite(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(UiKitInstaller.KitFolder + "/" + name + ".png");
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static RectTransform Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        /// <summary>Anchors and pivots the rect at one point of its parent, then places it.</summary>
        public static RectTransform Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public static Image Image(string name, Transform parent, string sprite, bool isSliced)
        {
            RectTransform rect = Rect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = Sprite(sprite);
            image.type = isSliced ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            image.preserveAspect = !isSliced;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>A full-screen wash under a popup that also takes the taps meant for the screen behind.</summary>
        public static Image Backdrop(Transform parent, Color color)
        {
            RectTransform rect = Stretch(Rect("Backdrop", parent));
            rect.SetAsFirstSibling();
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return image;
        }

        public static Image Icon(string name, Transform parent, string icon, float size)
        {
            Image image = Image(name, parent, "icon_" + icon, false);
            Place(image.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
            return image;
        }

        public static TMP_Text Label(string name, Transform parent, string text, float size, bool isOutlined)
        {
            RectTransform rect = Rect(name, parent);
            TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(isOutlined ? UiKitInstaller.TitleFontPath : UiKitInstaller.BodyFontPath);

            if (isOutlined)
            {
                label.fontSharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(UiKitInstaller.OutlinedMaterialPath);
            }

            label.text = text;
            label.fontSize = size;
            label.color = isOutlined ? TextCream : TextBrown;
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = false;
            label.raycastTarget = false;
            return label;
        }

        /// <summary>
        /// A label of fixed text that follows the chosen language: laid out in English, then
        /// kept in step by a <see cref="LocalizedLabel"/>.
        /// </summary>
        public static TMP_Text Text(string name, Transform parent, string key, float size, bool isOutlined)
        {
            TMP_Text label = Label(name, parent, LocalizationTable.English(key), size, isOutlined);
            label.gameObject.AddComponent<LocalizedLabel>().EditorLink(label, key);
            return label;
        }

        /// <summary>A chunky kit button: sliced face sprite with a press scale, child content on top.</summary>
        public static Button Button(string name, Transform parent, string face, Vector2 size)
        {
            // Round faces and the ticket are drawn whole: a round face's 9-slice borders are
            // wider than the button itself, and slicing squeezed it into a lumpy circle; the
            // ticket's notches and perforation sit at fixed places along it.
            Image image = Image(name, parent, face, !IsDrawnWhole(face));
            image.raycastTarget = true;
            image.rectTransform.sizeDelta = size;
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.pressedColor = new Color(0.9f, 0.88f, 0.84f);
            colors.disabledColor = new Color(0.8f, 0.8f, 0.8f, 0.9f);
            button.colors = colors;
            image.gameObject.AddComponent<ButtonPressScale>();
            return button;
        }

        public static bool IsDrawnWhole(string face)
        {
            return face.StartsWith("btn_round") || face == TicketFace;
        }

        public const string TicketFace = "btn_ticket";

        /// <summary>
        /// A full-size layer that keeps its children inside the screen's safe area, clear of
        /// notches and the home bar. Backdrops and skies go outside it so they still fill the
        /// whole screen.
        /// </summary>
        public static RectTransform SafeLayer(Transform parent)
        {
            RectTransform safe = Stretch(Rect("Safe", parent));
            safe.gameObject.AddComponent<SafeArea>();
            return safe;
        }

        public static CanvasGroup Group(RectTransform rect)
        {
            return rect.gameObject.AddComponent<CanvasGroup>();
        }
    }
}
