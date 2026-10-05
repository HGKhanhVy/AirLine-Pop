using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Builds the shop tab on Home: a cream sheet with Snacks and Planes tabs, a status line,
    /// and every card already in place, one per snack pack and one per livery, so opening the
    /// shop never creates anything.
    /// </summary>
    public static class ShopUiBuilder
    {
        private static readonly Vector2 SnackCardSize = new Vector2(300f, 500f);
        private static readonly Vector2 PlaneCardSize = new Vector2(450f, 340f);

        // Theme cards stand a little taller, so the wide board preview fills them.
        private static readonly Vector2 ThemeCardSize = new Vector2(450f, 348f);

        public static CanvasGroup Build(RectTransform safe, LiveryCatalogSO liveries, SkinCatalogSO themes, EconomyConfigSO economy,
            out ShopPresenter presenter)
        {
            RectTransform panel = UiBuilder.Stretch(UiBuilder.Rect("ShopPanel", safe));
            CanvasGroup group = UiBuilder.Group(panel);

            Image sheet = UiBuilder.Image("Sheet", panel, "panel_cream", true);
            RectTransform rect = sheet.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(36f, 220f);
            rect.offsetMax = new Vector2(-36f, -290f);
            sheet.raycastTarget = true;

            TMP_Text title = UiBuilder.Text("Title", sheet.transform, "shop.title", 84f, true);
            UiBuilder.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(800f, 100f));

            Button snacksTab = Tab(sheet.transform, "SnacksTab", "shop.snacks", -310f, out Image snacksFace);
            Button planesTab = Tab(sheet.transform, "PlanesTab", "shop.planes", 0f, out Image planesFace);
            Button themesTab = Tab(sheet.transform, "ThemesTab", "shop.themes", 310f, out Image themesFace);

            TMP_Text status = UiBuilder.Label("Status", sheet.transform, string.Empty, 36f, false);
            UiBuilder.Place(status.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -250f), new Vector2(920f, 46f));
            status.enableAutoSizing = true;
            status.fontSizeMin = 22f;
            status.fontSizeMax = 34f;

            int snackCount = economy != null ? economy.SnackPacks.Count : 0;
            int planeCount = liveries != null ? liveries.Liveries.Count : 0;
            int themeCount = themes != null && themes.Skins != null ? themes.Skins.Count : 0;
            RectTransform snacks = Shelf(sheet.transform, "SnacksShelf", SnackCardSize, 3, snackCount, out RectTransform snackArea);
            RectTransform planes = Shelf(sheet.transform, "PlanesShelf", PlaneCardSize, 2, planeCount, out RectTransform planeArea);
            RectTransform themeShelf = Shelf(sheet.transform, "ThemesShelf", ThemeCardSize, 2, themeCount, out RectTransform themeArea);
            var themeCards = new ShopItemCardView[themeCount];

            for (int i = 0; i < themeCount; i++)
            {
                themeCards[i] = Card(themeShelf, "Theme " + themes.Skins[i].Id, ThemeCardSize);
            }

            var snackCards = new ShopItemCardView[snackCount];

            for (int i = 0; i < snackCount; i++)
            {
                snackCards[i] = Card(snacks, "Snack " + (i + 1), SnackCardSize);
            }

            var planeCards = new ShopItemCardView[planeCount];

            for (int i = 0; i < planeCount; i++)
            {
                planeCards[i] = Card(planes, "Livery " + liveries.Liveries[i].Id, PlaneCardSize);
            }

            ShopView view = panel.gameObject.AddComponent<ShopView>();
            view.EditorLink(new[] { snacksTab, planesTab, themesTab }, new[] { snacksFace, planesFace, themesFace },
                new[] { snackArea.gameObject, planeArea.gameObject, themeArea.gameObject }, status, snackCards, planeCards, themeCards,
                UiBuilder.Sprite("btn_sky"), UiBuilder.Sprite("btn_cream"));

            presenter = panel.gameObject.AddComponent<ShopPresenter>();
            presenter.EditorLink(view);
            return group;
        }

        private static Button Tab(Transform sheet, string name, string labelKey, float x, out Image face)
        {
            Button button = UiBuilder.Button(name, sheet, "btn_cream", new Vector2(290f, 100f));
            UiBuilder.Place((RectTransform)button.transform, new Vector2(0.5f, 1f), new Vector2(x, -130f), new Vector2(290f, 100f));
            face = (Image)button.targetGraphic;
            TMP_Text text = UiBuilder.Text("Label", button.transform, labelKey, 46f, false);
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitInstaller.TitleFontPath);
            UiBuilder.Place(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 5f), new Vector2(270f, 80f));
            text.enableAutoSizing = true;
            text.fontSizeMin = 30f;
            text.fontSizeMax = 46f;
            return button;
        }

        /// <summary>
        /// One tab's shelf: the area under the tabs, holding a grid laid out at its designed
        /// size. On narrow or short screens the grid shrinks evenly to fit the area instead
        /// of running off the sheet. Returns the grid, where the cards go.
        /// </summary>
        private static RectTransform Shelf(Transform sheet, string name, Vector2 cardSize, int columns, int count, out RectTransform area)
        {
            area = UiBuilder.Rect(name, sheet);
            area.anchorMin = Vector2.zero;
            area.anchorMax = Vector2.one;
            area.offsetMin = new Vector2(24f, 16f);
            area.offsetMax = new Vector2(-24f, -318f);

            var spacing = new Vector2(24f, 14f);
            int rows = Mathf.Max(1, Mathf.CeilToInt(count / (float)columns));
            var designed = new Vector2(columns * cardSize.x + (columns - 1) * spacing.x, rows * cardSize.y + (rows - 1) * spacing.y);

            RectTransform shelf = UiBuilder.Rect("Grid", area);
            UiBuilder.Place(shelf, new Vector2(0.5f, 1f), Vector2.zero, designed);
            area.gameObject.AddComponent<ContentScaleFitter>().EditorLink(area, shelf, 0f);

            GridLayoutGroup grid = shelf.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = cardSize;
            grid.spacing = spacing;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.childAlignment = TextAnchor.UpperCenter;
            return shelf;
        }

        /// <summary>
        /// One item: its picture at the top, its name under it, and a slim button at the foot,
        /// so the item reads first and the price second.
        /// </summary>
        private static ShopItemCardView Card(RectTransform shelf, string name, Vector2 size)
        {
            Image card = UiBuilder.Image(name, shelf, "panel_card", true);

            const float margin = 12f;
            const float buttonHeight = 70f;
            const float nameHeight = 48f;
            float stageHeight = size.y - buttonHeight - nameHeight - margin * 3f;
            var stageSize = new Vector2(size.x - margin * 2f, stageHeight);

            Image picture = UiBuilder.Image("Picture", card.transform, "pill_cream", false);
            picture.preserveAspect = true;
            UiBuilder.Place(picture.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -margin), stageSize);

            TMP_Text label = UiBuilder.Label("Name", card.transform, string.Empty, 36f, false);
            label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitInstaller.TitleFontPath);
            UiBuilder.Place(label.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, buttonHeight + margin * 1.5f), new Vector2(size.x - 30f, nameHeight));
            label.enableAutoSizing = true;
            label.fontSizeMin = 24f;
            label.fontSizeMax = 36f;

            Vector2 buttonSize = new Vector2(Mathf.Min(size.x - 60f, 280f), buttonHeight);
            Button button = UiBuilder.Button("Button", card.transform, "btn_orange", buttonSize);
            UiBuilder.Place((RectTransform)button.transform, new Vector2(0.5f, 0f), new Vector2(0f, margin), buttonSize);

            RectTransform priceRow = UiBuilder.Rect("Price", button.transform);
            UiBuilder.Place(priceRow, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), buttonSize);
            Image coin = UiBuilder.Icon("Coin", priceRow, "coin", 48f);
            coin.rectTransform.anchoredPosition = new Vector2(-44f, 0f);
            TMP_Text price = UiBuilder.Label("Amount", priceRow, "0", 38f, true);
            price.alignment = TextAlignmentOptions.Left;
            UiBuilder.Place(price.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(50f, 0f), new Vector2(130f, 56f));

            TMP_Text action = UiBuilder.Label("Action", button.transform, string.Empty, 36f, true);
            UiBuilder.Place(action.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), buttonSize - new Vector2(20f, 14f));

            ShopItemCardView view = card.gameObject.AddComponent<ShopItemCardView>();
            view.EditorLink(picture, label, button, (Image)button.targetGraphic, priceRow.gameObject, price, action,
                UiBuilder.Sprite("btn_orange"), UiBuilder.Sprite("btn_sky"), UiBuilder.Sprite("btn_disabled"));
            return view;
        }
    }
}
