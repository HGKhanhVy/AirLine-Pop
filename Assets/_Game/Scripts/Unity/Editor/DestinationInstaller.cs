using System;
using System.Collections.Generic;
using System.IO;
using ASTeams.SingleLine.Core;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Installs the route map: the destination catalog built from the postcards drawn by
    /// Tools/flat_art/generate_postcards.py, the postcard on the win card, and the album
    /// on Home (built by <see cref="HomeSceneBuilder"/> through <see cref="BuildAlbum"/>).
    /// Safe to rerun.
    /// </summary>
    public static class DestinationInstaller
    {
        private const string PostcardFolder = "Assets/_Game/Art/Postcards";
        private const string RoutesPath = PostcardFolder + "/routes.json";
        public const string CatalogPath = "Assets/_Game/Config/DestinationCatalog.asset";
        private const string IconPath = "Assets/_Game/Art/UIKit/icon_postcard.png";
        private const string HudPrefabPath = "Assets/_Game/UI/AirlineHud.prefab";
        private const string PostcardName = "Postcard";
        private const int FlightsPerDestination = 10;

        [Serializable]
        private sealed class RouteList
        {
            public RouteEntry[] items;
        }

        [Serializable]
        private sealed class RouteEntry
        {
            public string id;
            public string name;
            public string name_vi;
            public string region;
            public string region_name;
            public string region_name_vi;
            public string postcard;
            public string postcard_vi;
        }

        [MenuItem("Tools/AirLine Pop/Install Destinations")]
        public static void InstallFromMenu()
        {
            Debug.Log(Install());
        }

        public static string Install()
        {
            DestinationCatalogSO catalog = BuildCatalog();

            if (catalog == null)
            {
                return "No " + RoutesPath + "; run generate_postcards.py first.";
            }

            UiKitInstaller.ImportKitSprite(IconPath);
            string hud = WireHud(catalog);
            AssetDatabase.SaveAssets();
            return "Destinations installed: " + catalog.Count + " cities.\n" + hud;
        }

        public static DestinationCatalogSO BuildCatalog()
        {
            if (!File.Exists(RoutesPath))
            {
                return null;
            }

            RouteEntry[] routes = ReadRoutes();
            var destinations = new Destination[routes.Length];

            for (int i = 0; i < destinations.Length; i++)
            {
                RouteEntry route = routes[i];
                Sprite postcard = SpriteImport.Import(PostcardFolder + "/" + route.postcard + ".png", 100f);
                Sprite postcardVietnamese = SpriteImport.Import(PostcardFolder + "/" + route.postcard_vi + ".png", 100f);
                destinations[i] = new Destination(route.id, route.region, postcard, postcardVietnamese);
            }

            var catalog = AssetDatabase.LoadAssetAtPath<DestinationCatalogSO>(CatalogPath);

            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<DestinationCatalogSO>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.EditorSetDestinations(destinations, FlightsPerDestination);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static RouteEntry[] ReadRoutes()
        {
            return JsonUtility.FromJson<RouteList>("{\"items\":" + File.ReadAllText(RoutesPath) + "}").items;
        }

        /// <summary>Each city's and country's name in both languages, for the localization table.</summary>
        public static IEnumerable<BilingualTextEntry> RouteTexts()
        {
            if (!File.Exists(RoutesPath))
            {
                yield break;
            }

            var regions = new HashSet<string>();

            foreach (RouteEntry route in ReadRoutes())
            {
                yield return new BilingualTextEntry { Key = Destination.NameKey(route.id), English = route.name, Vietnamese = route.name_vi };

                if (regions.Add(route.region))
                {
                    yield return new BilingualTextEntry
                    {
                        Key = Destination.RegionKey(route.region), English = route.region_name, Vietnamese = route.region_name_vi,
                    };
                }
            }
        }

        // ------------------------------------------------------------------ win card

        private static string WireHud(DestinationCatalogSO catalog)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(HudPrefabPath);

            try
            {
                var view = contents.GetComponentInChildren<WinPanelView>(true);
                var win = contents.GetComponentInChildren<WinSequenceController>(true);

                if (view == null || win == null)
                {
                    return "No win card in " + HudPrefabPath + "; HUD left unchanged.";
                }

                var portrait = (RawImage)new SerializedObject(view).FindProperty("catPortrait").objectReferenceValue;
                var card = (RectTransform)portrait.transform.parent;
                Transform old = card.Find(PostcardName);

                if (old != null)
                {
                    UnityEngine.Object.DestroyImmediate(old.gameObject);
                }

                BuildWinPostcard(card, portrait.rectTransform, view);
                LinkWinRoutes(win, catalog);
                PrefabUtility.SaveAsPrefabAsset(contents, HudPrefabPath);
                return HudPrefabPath + ": postcard on the win card, route map linked.";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        /// <summary>The postcard that takes the portrait's place, tilted a little like a card on a desk.</summary>
        public static void BuildWinPostcard(RectTransform card, RectTransform portrait, WinPanelView view)
        {
            Image postcard = UiBuilder.Image(PostcardName, card, "pill_cream", false);
            postcard.sprite = null;
            postcard.preserveAspect = true;
            postcard.enabled = false;

            RectTransform rect = postcard.rectTransform;
            rect.anchorMin = portrait.anchorMin;
            rect.anchorMax = portrait.anchorMax;
            rect.pivot = portrait.pivot;
            rect.anchoredPosition = portrait.anchoredPosition + new Vector2(0f, -110f);
            rect.sizeDelta = new Vector2(540f, 360f);
            rect.localRotation = Quaternion.Euler(0f, 0f, -4f);

            view.EditorLinkPostcard(postcard);
        }

        public static void LinkWinRoutes(WinSequenceController win, DestinationCatalogSO catalog)
        {
            var so = new SerializedObject(win);
            so.FindProperty("destinations").objectReferenceValue = catalog;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------ album

        /// <summary>
        /// The album: a round button under Settings and a modal of postcard slots, one per
        /// destination, built here so opening it never instantiates anything.
        /// </summary>
        public static PostcardAlbumView BuildAlbum(RectTransform safe, DestinationCatalogSO catalog)
        {
            UiKitInstaller.ImportKitSprite(IconPath);

            RectTransform root = UiBuilder.Stretch(UiBuilder.Rect("PostcardAlbum", safe));

            Button open = UiBuilder.Button("AlbumButton", root, "btn_round_cream", new Vector2(112f, 112f));
            UiBuilder.Place((RectTransform)open.transform, new Vector2(0f, 1f), new Vector2(36f, -160f), new Vector2(112f, 112f));
            UiBuilder.Icon("Icon", open.transform, "postcard", 84f).rectTransform.anchoredPosition = new Vector2(0f, 4f);

            RectTransform panel = UiBuilder.Stretch(UiBuilder.Rect("AlbumPanel", root));
            CanvasGroup group = UiBuilder.Group(panel);
            Image dimmer = panel.gameObject.AddComponent<Image>();
            dimmer.color = UiBuilder.ModalDim;
            dimmer.raycastTarget = true;

            Image card = UiBuilder.Image("Card", panel, "panel_cream", true);
            UiBuilder.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(980f, 1560f));
            card.raycastTarget = true;

            TMP_Text title = UiBuilder.Text("Title", card.transform, "album.title", 80f, false);
            title.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitInstaller.TitleFontPath);
            UiBuilder.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(760f, 110f));

            TMP_Text summary = UiBuilder.Label("Summary", card.transform, "0/0 postcards", 44f, false);
            UiBuilder.Place(summary.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(760f, 60f));

            Button close = UiBuilder.Button("CloseButton", card.transform, "btn_round_cream", new Vector2(100f, 100f));
            UiBuilder.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(-28f, -28f), new Vector2(100f, 100f));
            UiBuilder.Icon("Icon", close.transform, "close", 60f).color = UiBuilder.IconBlue;

            PostcardCellView[] cells = BuildGrid(card.rectTransform, catalog != null ? catalog.Count : 0);

            ModalPanel modal = panel.gameObject.AddComponent<ModalPanel>();
            modal.EditorLink(group, card.rectTransform);

            PostcardViewerView viewer = BuildViewer(panel);

            PostcardAlbumView view = root.gameObject.AddComponent<PostcardAlbumView>();
            view.EditorLink(modal, open, close, summary, cells, viewer);
            return view;
        }

        /// <summary>An earned card shown large over the album; a tap anywhere closes it.</summary>
        private static PostcardViewerView BuildViewer(RectTransform album)
        {
            RectTransform root = UiBuilder.Stretch(UiBuilder.Rect("PostcardViewer", album));
            CanvasGroup group = UiBuilder.Group(root);
            Image backdrop = UiBuilder.Backdrop(root, UiBuilder.ModalDim);
            Button close = backdrop.gameObject.AddComponent<Button>();
            close.transition = Selectable.Transition.None;

            RectTransform card = UiBuilder.Rect("Card", root);
            UiBuilder.Place(card, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(1000f, 667f));

            Image picture = UiBuilder.Image("Picture", card, "pill_cream", false);
            picture.preserveAspect = true;
            UiBuilder.Place(picture.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1000f, 667f));

            TMP_Text hint = UiBuilder.Text("Hint", root, "common.tapToClose", 38f, false);
            hint.color = new Color(1f, 1f, 1f, 0.7f);
            UiBuilder.Place(hint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -360f), new Vector2(900f, 60f));

            ModalPanel modal = root.gameObject.AddComponent<ModalPanel>();
            modal.EditorLink(group, card);
            PostcardViewerView viewer = root.gameObject.AddComponent<PostcardViewerView>();
            viewer.EditorLink(modal, picture, close);
            return viewer;
        }

        private static PostcardCellView[] BuildGrid(RectTransform card, int count)
        {
            RectTransform viewport = UiBuilder.Rect("Viewport", card);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(30f, 36f);
            viewport.offsetMax = new Vector2(-30f, -220f);
            viewport.gameObject.AddComponent<RectMask2D>();
            Image hit = viewport.gameObject.AddComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);

            RectTransform content = UiBuilder.Rect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;

            GridLayoutGroup grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(280f, 260f);
            grid.spacing = new Vector2(20f, 20f);
            grid.padding = new RectOffset(10, 10, 10, 10);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperCenter;
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;

            var cells = new PostcardCellView[count];

            for (int i = 0; i < count; i++)
            {
                cells[i] = BuildCell(content, i);
            }

            return cells;
        }

        private static PostcardCellView BuildCell(RectTransform content, int index)
        {
            RectTransform cell = UiBuilder.Rect("Postcard " + (index + 1), content);

            Image picture = UiBuilder.Image("Picture", cell, "pill_cream", false);
            picture.preserveAspect = true;
            UiBuilder.Place(picture.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(280f, 187f));

            TMP_Text name = UiBuilder.Label("Name", cell, "?", 38f, false);
            UiBuilder.Place(name.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(280f, 56f));

            Image lockBadge = UiBuilder.Image("Lock", picture.transform, "btn_round_cream", false);
            UiBuilder.Place(lockBadge.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 14f), new Vector2(84f, 84f));
            UiBuilder.Icon("Icon", lockBadge.transform, "lock", 52f).rectTransform.anchoredPosition = new Vector2(0f, 3f);

            TMP_Text progress = UiBuilder.Label("Progress", picture.transform, "0/10", 32f, true);
            UiBuilder.Place(progress.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(260f, 46f));

            Image hit = cell.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            Button button = cell.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            cell.gameObject.AddComponent<ButtonPressScale>();

            PostcardCellView view = cell.gameObject.AddComponent<PostcardCellView>();
            view.EditorLink(picture, name, progress, lockBadge.gameObject, button);
            return view;
        }
    }
}
