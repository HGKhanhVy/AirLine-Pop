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

        private static readonly Color HeartColour = new Color32(240, 124, 108, 255);

        private static readonly Color BadgeColour = new Color32(240, 110, 96, 255);

        /// <summary>
        /// The radial menu round a cat: the care actions on holders the view lays out on an
        /// arc round the cat, the name tag that hangs under its feet, and a status line that
        /// pops over the arc. A tap anywhere else closes it, so there is no close button.
        /// </summary>
        public static CatMenuView BuildCatMenu(RectTransform screen)
        {
            RectTransform root = UiBuilder.Stretch(UiBuilder.Rect("CatMenu", screen));
            CanvasGroup group = UiBuilder.Group(root);

            // An invisible catch-all: a tap anywhere off the menu closes it.
            Image backdrop = UiBuilder.Backdrop(root, Color.clear);
            Button backdropButton = backdrop.gameObject.AddComponent<Button>();
            backdropButton.transition = Selectable.Transition.None;

            RectTransform anchor = UiBuilder.Rect("Anchor", UiBuilder.SafeLayer(root));
            UiBuilder.Place(anchor, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            // In arc order, from the cat's left round over its head.
            Button play = CareButton(anchor, "Play", "paw", "care.play", out _, out RectTransform playPop);
            Button pet = CareButton(anchor, "Pet", "heart", "care.greet", out _, out RectTransform petPop);
            Button feed = CareButton(anchor, "Feed", "fish", null, out TMP_Text feedLabel, out RectTransform feedPop);
            GameObject badge = CountBadge(feed.transform, out TMP_Text count);
            Button wardrobe = CareButton(anchor, "Wardrobe", "paw", "care.wardrobe", out _, out RectTransform wardrobePop);

            // Dress up shows the first accessory itself, the bow, rather than a generic glyph.
            Image hanger = wardrobe.transform.Find("Icon").GetComponent<Image>();
            hanger.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BowArt);
            hanger.color = Color.white;
            hanger.rectTransform.sizeDelta = new Vector2(76f, 76f);

            RectTransform tagPop = BuildNameTag(anchor, out TMP_Text name, out TMP_Text tier, out ProgressBarView bar);

            RectTransform statusPop = UiBuilder.Rect("Status", anchor);
            UiBuilder.Place(statusPop, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460f, 68f));
            Image statusPill = UiBuilder.Image("Pill", statusPop, "pill_cream", true);
            UiBuilder.Stretch(statusPill.rectTransform);
            TMP_Text status = UiBuilder.Label("Text", statusPop, string.Empty, 30f, false);
            UiBuilder.Place(status.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 3f), new Vector2(430f, 56f));
            status.enableAutoSizing = true;
            status.fontSizeMin = 20f;
            status.fontSizeMax = 30f;

            CatMenuView view = root.gameObject.AddComponent<CatMenuView>();
            view.EditorLink(group, anchor, name, tier, bar, feedLabel, badge, count, status, statusPop, pet, play, feed, wardrobe,
                backdropButton, new[] { playPop, petPop, feedPop, wardrobePop }, tagPop);
            return view;
        }

        private const string BowArt = "Assets/_Game/Art/Accessories/acc_bow.png";
        private const int WardrobeSlots = 5;

        /// <summary>
        /// The row of accessories Dress up opens over the cat, inside the menu so it rings the
        /// same cat: a slot to take the accessory off, then one per accessory, each with a ring
        /// for the one worn and a padlock until it unlocks, and a Back button.
        /// </summary>
        public static CatWardrobePickerView BuildWardrobePicker(CatMenuView menu)
        {
            RectTransform anchor = (RectTransform)menu.transform.Find("Safe/Anchor");
            RectTransform row = UiBuilder.Rect("Wardrobe", anchor);
            UiBuilder.Place(row, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 176f));

            Image panel = UiBuilder.Image("Panel", row, "panel_cream", true);
            UiBuilder.Stretch(panel.rectTransform);
            panel.raycastTarget = true;

            var slots = new Button[WardrobeSlots];
            var icons = new Image[WardrobeSlots];
            var labels = new TMP_Text[WardrobeSlots];
            var locks = new GameObject[WardrobeSlots];
            var rings = new GameObject[WardrobeSlots];

            for (int i = 0; i < WardrobeSlots; i++)
            {
                float x = (i - (WardrobeSlots - 1) * 0.5f) * 116f;

                Image ring = UiBuilder.Image("WornRing " + i, row, "btn_round_cream", false);
                ring.color = new Color32(250, 204, 72, 255);
                UiBuilder.Place(ring.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, 20f), new Vector2(108f, 108f));
                rings[i] = ring.gameObject;

                Button slot = UiBuilder.Button("Slot " + i, row, "btn_round_cream", new Vector2(94f, 94f));
                UiBuilder.Place((RectTransform)slot.transform, new Vector2(0.5f, 0.5f), new Vector2(x, 20f), new Vector2(94f, 94f));
                slots[i] = slot;

                Image icon = UiBuilder.Image("Icon", slot.transform, "pill_cream", false);
                UiBuilder.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(70f, 70f));
                icons[i] = icon;

                Image padlock = UiBuilder.Icon("Lock", slot.transform, "lock", 40f);
                padlock.color = UiBuilder.TextBrown;
                padlock.rectTransform.anchoredPosition = new Vector2(30f, -30f);
                locks[i] = padlock.gameObject;

                TMP_Text label = UiBuilder.Label("Label", row, string.Empty, 22f, false);
                UiBuilder.Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, -54f), new Vector2(112f, 36f));
                label.enableAutoSizing = true;
                label.fontSizeMin = 14f;
                label.fontSizeMax = 22f;
                labels[i] = label;
            }

            Button back = UiBuilder.Button("BackButton", row, "btn_round_cream", new Vector2(64f, 64f));
            UiBuilder.Place((RectTransform)back.transform, new Vector2(0.5f, 0.5f), new Vector2(-300f, 84f), new Vector2(64f, 64f));
            UiBuilder.Icon("Icon", back.transform, "back", 40f).color = UiBuilder.IconBlue;

            CatWardrobePickerView picker = menu.gameObject.AddComponent<CatWardrobePickerView>();
            picker.EditorLink(row, slots, icons, labels, locks, rings, back);
            return picker;
        }

        /// <summary>The "+20" pill that rises from a cat handing over its daily gift.</summary>
        public static CatGiftPopView BuildGiftPop(RectTransform safe, Camera worldCamera)
        {
            RectTransform holder = UiBuilder.Stretch(UiBuilder.Rect("GiftPop", safe));
            RectTransform pop = UiBuilder.Rect("Pop", holder);
            UiBuilder.Place(pop, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 76f));
            pop.pivot = new Vector2(0.5f, 0f);
            CanvasGroup group = UiBuilder.Group(pop);
            group.blocksRaycasts = false;

            Image pill = UiBuilder.Image("Pill", pop, "pill_cream", true);
            UiBuilder.Stretch(pill.rectTransform);
            Image coin = UiBuilder.Icon("Coin", pop, "coin", 56f);
            coin.rectTransform.anchoredPosition = new Vector2(-54f, 2f);
            TMP_Text amount = UiBuilder.Label("Amount", pop, "+0", 44f, true);
            amount.color = new Color32(250, 196, 64, 255);
            UiBuilder.Place(amount.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(24f, 4f), new Vector2(120f, 64f));

            CatGiftPopView view = holder.gameObject.AddComponent<CatGiftPopView>();
            view.EditorLink(pop, group, amount, worldCamera);
            return view;
        }

        /// <summary>The cat's name, a heart with its loyalty progress and its loyalty card, on one small tag.</summary>
        private static RectTransform BuildNameTag(RectTransform anchor, out TMP_Text name, out TMP_Text tier, out ProgressBarView bar)
        {
            RectTransform tagPop = Pop(anchor, "Tag");
            Image tag = UiBuilder.Image("Pill", tagPop, "pill_cream", true);
            UiBuilder.Place(tag.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(360f, 100f));
            tag.raycastTarget = true;

            name = UiBuilder.Label("Name", tag.transform, "Tabby", 40f, false);
            name.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitInstaller.TitleFontPath);
            UiBuilder.Place(name.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(290f, 50f));
            name.enableAutoSizing = true;
            name.fontSizeMin = 26f;
            name.fontSizeMax = 40f;

            Image heart = UiBuilder.Icon("Heart", tag.transform, "heart", 30f);
            heart.color = HeartColour;
            heart.rectTransform.anchoredPosition = new Vector2(-146f, -22f);
            Image track = UiBuilder.Image("TierTrack", tag.transform, "bar_track", true);
            UiBuilder.Place(track.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-50f, -22f), new Vector2(160f, 24f));
            Image fill = UiBuilder.Image("TierFill", track.transform, "bar_fill", true);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0.3f, 1f);
            fill.rectTransform.offsetMin = new Vector2(4f, 4f);
            fill.rectTransform.offsetMax = new Vector2(-4f, -4f);
            bar = track.gameObject.AddComponent<ProgressBarView>();
            bar.EditorLink(fill.rectTransform);

            tier = UiBuilder.Label("Tier", tag.transform, "New guest", 24f, false);
            tier.color = UiBuilder.IconBlue;
            UiBuilder.Place(tier.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(100f, -22f), new Vector2(124f, 34f));
            tier.enableAutoSizing = true;
            tier.fontSizeMin = 16f;
            tier.fontSizeMax = 24f;
            return tagPop;
        }

        /// <summary>An empty holder the view moves and springs out; the piece itself keeps a scale of one.</summary>
        private static RectTransform Pop(RectTransform anchor, string name)
        {
            RectTransform pop = UiBuilder.Rect(name, anchor);
            UiBuilder.Place(pop, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            return pop;
        }

        /// <summary>
        /// One round care button with its short name on a little pill under it; every one is
        /// the same size, so the arc reads evenly. A null key leaves the label to the presenter.
        /// </summary>
        private static Button CareButton(RectTransform anchor, string name, string icon, string labelKey,
            out TMP_Text text, out RectTransform pop)
        {
            pop = Pop(anchor, name);
            Button button = UiBuilder.Button(name + "Button", pop, "btn_round_cream", new Vector2(116f, 116f));
            UiBuilder.Place((RectTransform)button.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(116f, 116f));
            Image glyph = UiBuilder.Icon("Icon", button.transform, icon, 64f);
            glyph.rectTransform.anchoredPosition = new Vector2(0f, 3f);
            glyph.color = icon == "heart" ? HeartColour : UiBuilder.IconBlue;

            Image pill = UiBuilder.Image("LabelPill", pop, "pill_cream", true);
            UiBuilder.Place(pill.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -78f), new Vector2(132f, 44f));
            text = labelKey != null
                ? UiBuilder.Text("Label", pill.transform, labelKey, 26f, false)
                : UiBuilder.Label("Label", pill.transform, string.Empty, 26f, false);
            UiBuilder.Place(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(120f, 38f));
            text.enableAutoSizing = true;
            text.fontSizeMin = 16f;
            text.fontSizeMax = 26f;
            return button;
        }

        /// <summary>The little red count in a button's corner, as games show how many of an item are left.</summary>
        private static GameObject CountBadge(Transform button, out TMP_Text count)
        {
            Image badge = UiBuilder.Image("CountBadge", button, "btn_round_cream", false);
            badge.color = BadgeColour;
            UiBuilder.Place(badge.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(40f, 40f), new Vector2(48f, 48f));
            count = UiBuilder.Label("Count", badge.transform, "0", 28f, true);
            UiBuilder.Place(count.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(46f, 40f));
            count.enableAutoSizing = true;
            count.fontSizeMin = 16f;
            count.fontSizeMax = 28f;
            return badge.gameObject;
        }

        private const string CareBubbleArt = "Assets/_Game/Art/Loading/care_bubble.png";
        private const string CareBubbleLeftArt = "Assets/_Game/Art/Loading/care_bubble_left.png";

        /// <summary>The paw button that pops up over a touched cat and opens its care card.</summary>
        public static CatCareBubbleView BuildCareBubble(RectTransform safe)
        {
            // A speech bubble whose tail tip is the pivot, so placing it puts the tail on the cat.
            // The bubble pops and bobs on its own holder; the button inside keeps a scale of one,
            // which its press squash takes as its resting size.
            var size = new Vector2(150f, 142f);
            var tail = new Vector2(14f / 180f, 1f - 160f / 170f);
            RectTransform holder = UiBuilder.Stretch(UiBuilder.Rect("CareBubble", safe));
            RectTransform bubble = UiBuilder.Rect("Bubble", holder);
            UiBuilder.Place(bubble, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            bubble.pivot = tail;

            Button button = UiBuilder.Button("Button", bubble, "btn_round_cream", size);
            var face = (Image)button.targetGraphic;
            face.sprite = SpriteImport.Import(CareBubbleArt, 100f);
            face.type = Image.Type.Simple;
            face.preserveAspect = true;
            RectTransform press = UiBuilder.Stretch((RectTransform)button.transform);
            press.pivot = tail;

            // The paw sits in the middle of the bubble's round body, not of the whole picture.
            Image paw = UiBuilder.Icon("Icon", press, "paw", 72f);
            paw.color = UiBuilder.IconBlue;
            paw.rectTransform.anchoredPosition = new Vector2(8f, 10f);

            CatCareBubbleView view = holder.gameObject.AddComponent<CatCareBubbleView>();
            view.EditorLink(bubble, button, null);
            view.EditorLinkFacing(face, paw.rectTransform, press, face.sprite, SpriteImport.Import(CareBubbleLeftArt, 100f));
            return view;
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
