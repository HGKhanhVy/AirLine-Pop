using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Builds the lounge's UI for <see cref="HomeSceneBuilder"/>: the regular's card with its
    /// loyalty card and Greet / Play / Snack, and the "new regular" toast.
    /// </summary>
    public static class LoungeUiBuilder
    {

        public static CatMenuView BuildCatMenu(RectTransform safe)
        {
            RectTransform root = UiBuilder.Stretch(UiBuilder.Rect("CatMenu", safe));
            CanvasGroup group = UiBuilder.Group(root);

            // A light wash keeps the cat in view while the menu is clearly on top; a tap on
            // it closes the menu.
            Image backdrop = UiBuilder.Backdrop(root, UiBuilder.SheetDim);
            Button backdropButton = backdrop.gameObject.AddComponent<Button>();
            backdropButton.transition = Selectable.Transition.None;

            Image card = UiBuilder.Image("Card", root, "panel_cream", true);
            UiBuilder.Place(card.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 232f), new Vector2(980f, 440f));
            card.raycastTarget = true;

            TMP_Text name = UiBuilder.Label("Name", card.transform, "Tabby", 68f, false);
            name.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitInstaller.TitleFontPath);
            name.alignment = TextAlignmentOptions.Left;
            UiBuilder.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(48f, -26f), new Vector2(420f, 90f));

            // The sky-blue button face, so the cream loyalty text stands out on it.
            Image tierPill = UiBuilder.Image("TierPill", card.transform, "btn_sky", true);
            UiBuilder.Place(tierPill.rectTransform, new Vector2(0f, 1f), new Vector2(470f, -32f), new Vector2(300f, 76f));
            TMP_Text tier = UiBuilder.Label("Tier", tierPill.transform, "New guest", 40f, true);
            UiBuilder.Place(tier.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 3f), new Vector2(280f, 64f));

            Button close = UiBuilder.Button("CloseButton", card.transform, "btn_round_cream", new Vector2(92f, 92f));
            UiBuilder.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(-26f, -24f), new Vector2(92f, 92f));
            UiBuilder.Icon("Icon", close.transform, "close", 54f).color = UiBuilder.IconBlue;

            // Loyalty: a heart, then a rounded trough the pink fill grows along.
            Image heart = UiBuilder.Icon("TierHeart", card.transform, "heart", 64f);
            heart.color = new Color32(240, 124, 108, 255);
            UiBuilder.Place(heart.rectTransform, new Vector2(0f, 1f), new Vector2(64f, -136f), new Vector2(60f, 60f));
            Image track = UiBuilder.Image("TierTrack", card.transform, "bar_track", true);
            UiBuilder.Place(track.rectTransform, new Vector2(0.5f, 1f), new Vector2(40f, -136f), new Vector2(800f, 52f));
            Image fill = UiBuilder.Image("TierFill", track.transform, "bar_fill", true);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0.3f, 1f);
            fill.rectTransform.offsetMin = new Vector2(7f, 7f);
            fill.rectTransform.offsetMax = new Vector2(-7f, -7f);
            ProgressBarView bar = track.gameObject.AddComponent<ProgressBarView>();
            bar.EditorLink(fill.rectTransform);

            TMP_Text status = UiBuilder.Label("Status", card.transform, string.Empty, 40f, false);
            UiBuilder.Place(status.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -186f), new Vector2(884f, 56f));

            Button pet = ActionButton(card.transform, "PetButton", "heart", "care.greet", -300f, out _);
            Button play = ActionButton(card.transform, "PlayButton", "paw", "care.play", 0f, out _);
            Button feed = ActionButton(card.transform, "FeedButton", "fish", null, 300f, out TMP_Text feedLabel);

            CatMenuView view = root.gameObject.AddComponent<CatMenuView>();
            view.EditorLink(group, card.rectTransform, name, tier, bar, feedLabel, status, pet, play, feed, close, backdropButton);
            return view;
        }

        /// <summary>A care button; a null key leaves the label to the presenter, which writes counts into it.</summary>
        private static Button ActionButton(Transform card, string name, string icon, string labelKey, float x, out TMP_Text text)
        {
            Button button = UiBuilder.Button(name, card, "btn_cream", new Vector2(280f, 150f));
            UiBuilder.Place((RectTransform)button.transform, new Vector2(0.5f, 0f), new Vector2(x, 36f), new Vector2(280f, 150f));

            Image glyph = UiBuilder.Icon("Icon", button.transform, icon, 64f);
            UiBuilder.Place(glyph.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(64f, 64f));
            glyph.color = icon == "heart" ? new Color32(240, 124, 108, 255) : UiBuilder.IconBlue;

            text = labelKey != null
                ? UiBuilder.Text("Label", button.transform, labelKey, 34f, false)
                : UiBuilder.Label("Label", button.transform, string.Empty, 34f, false);
            UiBuilder.Place(text.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(260f, 50f));
            text.enableAutoSizing = true;
            text.fontSizeMin = 22f;
            text.fontSizeMax = 34f;
            return button;
        }

        public static ArrivalToastView BuildArrivalToast(RectTransform safe)
        {
            Image pill = UiBuilder.Image("ArrivalToast", safe, "pill_cream", true);
            UiBuilder.Place(pill.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -300f), new Vector2(900f, 100f));

            Image icon = UiBuilder.Icon("Icon", pill.transform, "cat", 76f);
            UiBuilder.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(22f, 3f), new Vector2(76f, 76f));

            TMP_Text label = UiBuilder.Label("Text", pill.transform, string.Empty, 40f, false);
            UiBuilder.Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(40f, 3f), new Vector2(760f, 80f));
            label.enableAutoSizing = true;
            label.fontSizeMin = 26f;
            label.fontSizeMax = 40f;

            CanvasGroup group = UiBuilder.Group(pill.rectTransform);
            ArrivalToastView view = pill.gameObject.AddComponent<ArrivalToastView>();
            view.EditorLink(group, pill.rectTransform, label);
            return view;
        }
    }
}
