using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Builds the stamp book on Home: a round button in the top bar and a modal book with one
    /// country to a page (a long country over several), turned with arrows at its foot.
    /// Every page and stamp is built here, so opening and turning never instantiate anything.
    /// </summary>
    public static class StampAlbumUiBuilder
    {
        private const string IconPath = "Assets/_Game/Art/UIKit/icon_postcard.png";
        private const string PostmarkPath = "Assets/_Game/Art/Postcards/postmark.png";
        private const string VipSealPath = "Assets/_Game/Art/Flat/vip_seal.png";

        // Four across and three down fit a page; a country with more stamps runs onto the next page.
        private static readonly Vector2 CellSize = new Vector2(215f, 300f);
        private const int Columns = 4;
        private const int StampsPerPage = 12;

        /// <param name="safe">Where the open button goes, in the top bar.</param>
        /// <param name="screen">Where the album itself goes: its wash fills the whole screen.</param>
        public static StampAlbumView Build(RectTransform safe, RectTransform screen, DestinationCatalogSO catalog, out Button openButton)
        {
            UiKitInstaller.ImportKitSprite(IconPath);
            Sprite postmark = SpriteImport.Import(PostmarkPath, 100f);

            RectTransform root = UiBuilder.Stretch(UiBuilder.Rect("StampAlbum", safe));

            // In the top bar, beside the Settings gear.
            Button open = UiBuilder.Button("AlbumButton", root, "btn_round_cream", new Vector2(112f, 112f));
            UiBuilder.Place((RectTransform)open.transform, new Vector2(0f, 1f), new Vector2(164f, -28f), new Vector2(112f, 112f));
            UiBuilder.Icon("Icon", open.transform, "postcard", 84f).rectTransform.anchoredPosition = new Vector2(0f, 4f);
            openButton = open;

            RectTransform panel = UiBuilder.Stretch(UiBuilder.Rect("AlbumPanel", screen));
            CanvasGroup group = UiBuilder.Group(panel);
            Image dimmer = panel.gameObject.AddComponent<Image>();
            dimmer.color = UiBuilder.ModalDim;
            dimmer.raycastTarget = true;

            RectTransform bookArea = UiBuilder.SafeLayer(panel);
            Image card = UiBuilder.Image("Card", bookArea, "panel_cream", true);
            UiBuilder.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(980f, 1560f));

            // The book is tall: on short or narrow screens it shrinks evenly instead of spilling over.
            bookArea.gameObject.AddComponent<ContentScaleFitter>().EditorLink(bookArea, card.rectTransform, 16f);
            card.raycastTarget = true;

            TMP_Text title = UiBuilder.Text("Title", card.transform, "album.title", 80f, false);
            title.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitInstaller.TitleFontPath);
            UiBuilder.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(760f, 110f));

            TMP_Text summary = UiBuilder.Label("Summary", card.transform, "0/0", 44f, false);
            UiBuilder.Place(summary.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(760f, 60f));

            Button close = UiBuilder.Button("CloseButton", card.transform, "btn_round_cream", new Vector2(100f, 100f));
            UiBuilder.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(-28f, -28f), new Vector2(100f, 100f));
            UiBuilder.Icon("Icon", close.transform, "close", 60f).color = UiBuilder.IconBlue;

            // The pages slide across as they turn; the book clips them to the card.
            RectTransform book = UiBuilder.Rect("Book", card.transform);
            book.anchorMin = Vector2.zero;
            book.anchorMax = Vector2.one;
            book.offsetMin = new Vector2(30f, 170f);
            book.offsetMax = new Vector2(-30f, -210f);
            book.gameObject.AddComponent<RectMask2D>();
            Image swipeArea = book.gameObject.AddComponent<Image>();
            swipeArea.color = Color.clear;
            PageSwipe swipe = book.gameObject.AddComponent<PageSwipe>();

            BuildPages(book, catalog, postmark, out RectTransform[] pages, out TMP_Text[] headings, out int[] pageCountry,
                out StampCellView[] cells);

            Button previous = PageButton("PreviousButton", card.rectTransform, new Vector2(0f, 0f), new Vector2(48f, 40f), false);
            Button next = PageButton("NextButton", card.rectTransform, new Vector2(1f, 0f), new Vector2(-48f, 40f), true);
            TMP_Text pageLabel = UiBuilder.Label("Page", card.transform, "1/1", 40f, false);
            UiBuilder.Place(pageLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(480f, 60f));

            ModalPanel modal = panel.gameObject.AddComponent<ModalPanel>();
            modal.EditorLink(group, card.rectTransform);

            PostcardViewerView viewer = BuildViewer(panel);

            StampAlbumView view = root.gameObject.AddComponent<StampAlbumView>();
            view.EditorLink(modal, open, close, previous, next, summary, pageLabel, swipe, pages, headings, pageCountry, cells, viewer);
            return view;
        }

        /// <summary>Each country's stamps over as many pages as they need, a heading on every page.</summary>
        private static void BuildPages(RectTransform book, DestinationCatalogSO catalog, Sprite postmark, out RectTransform[] pages,
            out TMP_Text[] headings, out int[] pageCountry, out StampCellView[] cells)
        {
            int count = catalog != null ? catalog.Count : 0;
            int countries = count > 0 ? catalog.Regions.PageCount : 0;
            var pageList = new List<RectTransform>();
            var headingList = new List<TMP_Text>();
            var countryList = new List<int>();
            cells = new StampCellView[count];

            for (int country = 0; country < countries; country++)
            {
                int first = catalog.Regions.FirstDestination(country);
                int total = catalog.Regions.DestinationCount(country);

                for (int start = 0; start < total; start += StampsPerPage)
                {
                    RectTransform page = UiBuilder.Stretch(UiBuilder.Rect("Page " + (pageList.Count + 1), book));

                    TMP_Text heading = UiBuilder.Label("Country", page, "Vietnam", 52f, false);
                    heading.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitInstaller.TitleFontPath);
                    heading.color = new Color32(214, 84, 76, 255);
                    UiBuilder.Place(heading.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(900f, 80f));

                    RectTransform grid = UiBuilder.Rect("Stamps", page);
                    UiBuilder.Place(grid, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(920f, 960f));
                    GridLayoutGroup layout = grid.gameObject.AddComponent<GridLayoutGroup>();
                    layout.cellSize = CellSize;
                    layout.spacing = new Vector2(16f, 20f);
                    layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    layout.constraintCount = Columns;
                    layout.childAlignment = TextAnchor.UpperCenter;

                    for (int i = start; i < Mathf.Min(total, start + StampsPerPage); i++)
                    {
                        cells[first + i] = BuildCell(grid, first + i, postmark);
                    }

                    page.gameObject.SetActive(pageList.Count == 0);
                    pageList.Add(page);
                    headingList.Add(heading);
                    countryList.Add(country);
                }
            }

            pages = pageList.ToArray();
            headings = headingList.ToArray();
            pageCountry = countryList.ToArray();
        }

        private static Button PageButton(string name, RectTransform card, Vector2 anchor, Vector2 position, bool isForward)
        {
            Button button = UiBuilder.Button(name, card, "btn_round_cream", new Vector2(110f, 110f));
            UiBuilder.Place((RectTransform)button.transform, anchor, position, new Vector2(110f, 110f));
            Image icon = UiBuilder.Icon("Icon", button.transform, "back", 64f);
            icon.color = UiBuilder.IconBlue;

            if (isForward)
            {
                icon.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            }

            return button;
        }

        private static StampCellView BuildCell(RectTransform grid, int index, Sprite postmarkSprite)
        {
            RectTransform cell = UiBuilder.Rect("Stamp " + (index + 1), grid);

            Image stamp = UiBuilder.Image("Stamp", cell, "pill_cream", false);
            stamp.sprite = null;
            stamp.preserveAspect = true;
            UiBuilder.Place(stamp.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(186f, 223f));
            stamp.rectTransform.localRotation = Quaternion.Euler(0f, 0f, index % 2 == 0 ? -2f : 2f);

            Image postmark = UiBuilder.Image("Postmark", stamp.transform, "pill_cream", false);
            postmark.sprite = postmarkSprite;
            UiBuilder.Place(postmark.rectTransform, new Vector2(1f, 0f), new Vector2(26f, 44f), new Vector2(140f, 105f));
            postmark.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -12f);

            // Still to collect: a small padlock in the corner and the flights so far, the picture left visible.
            Image lockBadge = UiBuilder.Image("Lock", stamp.transform, "btn_round_cream", false);
            UiBuilder.Place(lockBadge.rectTransform, new Vector2(1f, 1f), new Vector2(-12f, -12f), new Vector2(52f, 52f));
            UiBuilder.Icon("Icon", lockBadge.transform, "lock", 32f).rectTransform.anchoredPosition = new Vector2(0f, 2f);

            // A landed VIP flight presses a gold seal into the stamp's lower left corner.
            Image seal = UiBuilder.Image("VipSeal", stamp.transform, "pill_cream", false);
            seal.sprite = SpriteImport.Import(VipSealPath, 128f);
            seal.raycastTarget = false;
            UiBuilder.Place(seal.rectTransform, new Vector2(0f, 0f), new Vector2(20f, 34f), new Vector2(82f, 82f));
            seal.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 10f);
            seal.gameObject.SetActive(false);

            TMP_Text progress = UiBuilder.Label("Progress", stamp.transform, "0/10", 28f, true);
            UiBuilder.Place(progress.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 18f), new Vector2(180f, 40f));

            TMP_Text name = UiBuilder.Label("Name", cell, "?", 32f, false);
            UiBuilder.Place(name.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(215f, 50f));
            name.enableAutoSizing = true;
            name.fontSizeMin = 20f;
            name.fontSizeMax = 32f;

            Image hit = cell.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            Button button = cell.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            cell.gameObject.AddComponent<ButtonPressScale>();

            StampCellView view = cell.gameObject.AddComponent<StampCellView>();
            view.EditorLink(stamp, postmark.gameObject, name, progress, lockBadge.gameObject, button, seal.gameObject);
            return view;
        }

        /// <summary>A collected city's postcard shown large over the album; a tap anywhere closes it.</summary>
        private static PostcardViewerView BuildViewer(RectTransform album)
        {
            RectTransform root = UiBuilder.Stretch(UiBuilder.Rect("PostcardViewer", album));
            CanvasGroup group = UiBuilder.Group(root);
            Image backdrop = UiBuilder.Backdrop(root, UiBuilder.ModalDim);
            Button close = backdrop.gameObject.AddComponent<Button>();
            close.transition = Selectable.Transition.None;

            RectTransform safe = UiBuilder.SafeLayer(root);
            RectTransform card = UiBuilder.Rect("Card", safe);
            UiBuilder.Place(card, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(1000f, 667f));

            Image picture = UiBuilder.Image("Picture", card, "pill_cream", false);
            picture.preserveAspect = true;
            UiBuilder.Place(picture.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1000f, 667f));

            TMP_Text hint = UiBuilder.Text("Hint", safe, "common.tapToClose", 38f, false);
            hint.color = new Color(1f, 1f, 1f, 0.7f);
            UiBuilder.Place(hint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -360f), new Vector2(900f, 60f));

            ModalPanel modal = root.gameObject.AddComponent<ModalPanel>();
            modal.EditorLink(group, card);
            PostcardViewerView viewer = root.gameObject.AddComponent<PostcardViewerView>();
            viewer.EditorLink(modal, picture, close);
            return viewer;
        }
    }
}
