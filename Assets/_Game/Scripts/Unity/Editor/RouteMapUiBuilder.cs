using System.Collections.Generic;
using Crystal;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Builds the flight route map tab: the sky, a scroll view over the tall map, the route,
    /// pools of gate signs, airport cards and chapter passes, clouds and airport cloud
    /// islands, the airline's plane on the route and its planes crossing far off, all made
    /// here so scrolling never instantiates anything. Art comes from
    /// Tools/flat_art/generate_route_map.py and the Home airport's own props.
    /// </summary>
    public static class RouteMapUiBuilder
    {
        public const string ArtFolder = "Assets/_Game/Art/RouteMap";
        public const string ConfigPath = "Assets/_Game/Config/RouteMapConfig.asset";
        private const string HomeArtFolder = "Assets/_Game/Art/FlatHome";
        private const string PlanePath = "Assets/_Game/Art/Flat/airplane.png";
        private const string PilotPath = "Assets/_Game/Art/Flat/airplane_pilot.png";

        private const int GatePool = 24;
        private const int HubPool = 3;
        private const int ChapterPool = 3;
        private const int CloudPool = 12;
        private const int IslandPool = 3;

        private static readonly Color Navy = new Color32(52, 64, 96, 255);
        private static readonly Color Gold = new Color32(246, 196, 72, 255);
        private static readonly string[] Liveries = { "plane_parked", "plane_parked_sky", "plane_parked_mint", "plane_parked_lavender", "plane_parked_sunny" };
        private static readonly string[] Islands = { "island_tower", "island_cargo", "island_hangar", "island_terminal" };

        private static readonly Dictionary<string, Sprite> Imported = new Dictionary<string, Sprite>();

        public static CanvasGroup Build(Transform canvas, out RouteMapView view)
        {
            Imported.Clear();
            RouteMapConfigSO config = LoadOrCreateConfig();
            RectTransform panel = UiBuilder.Stretch(UiBuilder.Rect("MapPanel", canvas));
            panel.SetAsFirstSibling();
            CanvasGroup group = UiBuilder.Group(panel);

            Image sky = UiBuilder.Image("Sky", panel, "pill_cream", false);
            sky.sprite = Art("map_sky");
            sky.preserveAspect = false;
            UiBuilder.Stretch(sky.rectTransform);

            RectTransform viewport = UiBuilder.Stretch(UiBuilder.Rect("Viewport", panel));
            viewport.gameObject.AddComponent<RectMask2D>();
            Image hit = viewport.gameObject.AddComponent<Image>();
            hit.color = Color.clear;

            RectTransform content = UiBuilder.Rect("Content", viewport);
            UiBuilder.Place(content, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(config.Width, 2000f));

            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.decelerationRate = 0.12f;

            RouteMapDecor decor = BuildDecor(content);

            // Pivot at the bottom middle: the line's vertices share the pieces' origin.
            RectTransform lineRect = UiBuilder.Stretch(UiBuilder.Rect("Route", content));
            lineRect.pivot = new Vector2(0.5f, 0f);
            lineRect.gameObject.AddComponent<CanvasRenderer>();
            RouteLineGraphic line = lineRect.gameObject.AddComponent<RouteLineGraphic>();
            line.raycastTarget = false;

            ChapterCardView[] chapters = Pool(content, "Chapters", ChapterPool, BuildChapterCard);
            HubCardView[] hubs = Pool(content, "Airports", HubPool, BuildHub);
            LevelNodeView[] gates = Pool(content, "Gates", GatePool, BuildGate);

            // The plane the player chose, seen from above and drawn nose up, with Captain Bơ laid
            // over its cockpit as on the board (the pilot art is cut square around the cockpit).
            Image planeImage = Piece("Plane", content, AssetDatabase.LoadAssetAtPath<Sprite>(PlanePath), new Vector2(132f, 132f));
            planeImage.gameObject.AddComponent<PlaneLiveryImage>()
                .EditorLink(planeImage, AssetDatabase.LoadAssetAtPath<LiveryCatalogSO>(ShopInstaller.CatalogPath), false);
            Image pilot = Centered("Pilot", planeImage.transform, AssetDatabase.LoadAssetAtPath<Sprite>(PilotPath),
                new Vector2(0f, 132f * (256f - 175f) / 512f), new Vector2(132f * 176f / 512f, 132f * 176f / 512f));
            pilot.preserveAspect = true;
            RoutePlaneView plane = planeImage.gameObject.AddComponent<RoutePlaneView>();
            plane.EditorLink(planeImage.rectTransform, 90f);

            AirlinerFlyby flyby = BuildFlyby(panel);
            BuildHeader(panel);

            view = panel.gameObject.AddComponent<RouteMapView>();
            view.EditorLink(config, scroll, content, line, gates, hubs, chapters, plane, decor, flyby);

            // The theme the player wears dresses the map's sky and gate signs.
            panel.gameObject.AddComponent<RouteMapThemer>()
                .EditorLink(AssetDatabase.LoadAssetAtPath<SkinCatalogSO>(ThemeInstaller.CatalogPath), sky, gates, view, decor);
            return group;
        }

        private static RouteMapConfigSO LoadOrCreateConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<RouteMapConfigSO>(ConfigPath);

            if (config == null)
            {
                config = ScriptableObject.CreateInstance<RouteMapConfigSO>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            return config;
        }

        /// <summary>Each piece of art is imported once per build, however many pooled views use it.</summary>
        private static Sprite Art(string name)
        {
            return Import(ArtFolder + "/" + name + ".png");
        }

        private static Sprite Import(string path)
        {
            if (!Imported.TryGetValue(path, out Sprite sprite))
            {
                sprite = SpriteImport.Import(path, 100f);
                Imported[path] = sprite;
            }

            return sprite;
        }

        private static T[] Pool<T>(RectTransform content, string layerName, int count, System.Func<RectTransform, int, T> build)
        {
            RectTransform layer = UiBuilder.Stretch(UiBuilder.Rect(layerName, content));
            var pool = new T[count];

            for (int i = 0; i < count; i++)
            {
                pool[i] = build(layer, i);
            }

            return pool;
        }

        /// <summary>An image centred on a point of the map, the way every map piece is placed.</summary>
        private static Image Piece(string name, Transform parent, Sprite sprite, Vector2 size)
        {
            RectTransform rect = UiBuilder.Rect(name, parent);
            UiBuilder.Place(rect, new Vector2(0.5f, 0f), Vector2.zero, size);
            rect.pivot = new Vector2(0.5f, 0.5f);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static Image Centered(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size)
        {
            Image image = UiBuilder.Image(name, parent, "pill_cream", false);
            image.sprite = sprite;
            UiBuilder.Place(image.rectTransform, new Vector2(0.5f, 0.5f), position, size);
            image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            return image;
        }

        private static TMP_Text CenteredLabel(string name, Transform parent, string text, float size, Vector2 position, Vector2 box, bool isTitle)
        {
            TMP_Text label = UiBuilder.Label(name, parent, text, size, false);

            if (isTitle)
            {
                label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitInstaller.TitleFontPath);
            }

            UiBuilder.Place(label.rectTransform, new Vector2(0.5f, 0.5f), position, box);
            label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            return label;
        }

        /// <summary>A level: a gate sign with its number written on the chip under the word GATE.</summary>
        private static LevelNodeView BuildGate(RectTransform layer, int index)
        {
            RectTransform root = UiBuilder.Rect("Gate " + (index + 1), layer);
            UiBuilder.Place(root, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(128f, 84f));
            root.pivot = new Vector2(0.5f, 0.5f);
            Image hit = root.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            Button button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            root.gameObject.AddComponent<ButtonPressScale>();

            RectTransform pulse = UiBuilder.Stretch(UiBuilder.Rect("Pulse", root));
            Image face = UiBuilder.Image("Sign", pulse, "pill_cream", false);
            face.sprite = Art("gate_locked");
            UiBuilder.Stretch(face.rectTransform);
            TMP_Text number = CenteredLabel("Number", pulse, "1", 34f, new Vector2(0f, -9f), new Vector2(100f, 40f), true);

            root.gameObject.SetActive(false);
            LevelNodeView view = root.gameObject.AddComponent<LevelNodeView>();
            view.EditorLink(root, pulse, face, number, button, Art("gate_flown"), Art("gate_current"), Art("gate_locked"));
            return view;
        }

        /// <summary>
        /// A city's airport card: AIRPORT and the IATA code on the navy header, the landmark,
        /// the city's name and the flights done below, the arrival stamp once landed.
        /// </summary>
        private static HubCardView BuildHub(RectTransform layer, int index)
        {
            Image card = Piece("Airport " + (index + 1), layer, Art("hub_card"), new Vector2(460f, 236f));
            RectTransform root = card.rectTransform;
            CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;

            TMP_Text tag = UiBuilder.Text("Tag", root, "map.airport", 24f, false);
            tag.color = Gold;
            tag.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitInstaller.TitleFontPath);
            UiBuilder.Place(tag.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-110f, 87f), new Vector2(200f, 40f));
            tag.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            tag.alignment = TextAlignmentOptions.Left;

            TMP_Text code = CenteredLabel("Code", root, "DAD", 40f, new Vector2(140f, 87f), new Vector2(140f, 50f), true);
            code.color = Color.white;
            code.alignment = TextAlignmentOptions.Right;

            Image pin = Centered("Landmark", root, null, new Vector2(-136f, -28f), new Vector2(140f, 140f));

            TMP_Text name = CenteredLabel("City", root, "Da Nang", 40f, new Vector2(66f, -8f), new Vector2(240f, 56f), true);
            name.enableAutoSizing = true;
            name.fontSizeMin = 26f;
            name.fontSizeMax = 40f;

            TMP_Text progress = CenteredLabel("Flights", root, "0/10", 26f, new Vector2(56f, -62f), new Vector2(220f, 36f), false);
            progress.color = new Color32(150, 128, 118, 255);

            // Inked over the card's bottom corner, clear of the name and the flights line.
            Image stamp = Centered("ArrivalStamp", root, Art("arrival_stamp"), new Vector2(204f, -96f), new Vector2(96f, 96f));
            stamp.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -14f);

            Image lockBadge = UiBuilder.Image("Lock", root, "btn_round_cream", false);
            UiBuilder.Place(lockBadge.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-84f, -78f), new Vector2(56f, 56f));
            lockBadge.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            UiBuilder.Icon("Icon", lockBadge.transform, "lock", 34f).rectTransform.anchoredPosition = new Vector2(0f, 2f);

            root.gameObject.SetActive(false);
            HubCardView view = root.gameObject.AddComponent<HubCardView>();
            view.EditorLink(root, group, pin, code, name, progress, stamp.gameObject, lockBadge.gameObject);
            return view;
        }

        private static ChapterCardView BuildChapterCard(RectTransform layer, int index)
        {
            // A boarding pass, a little tilted, like one held up to the window; text on its body, the plane on its stub.
            Image card = UiBuilder.Image("Chapter " + (index + 1), layer, "pill_cream", false);
            card.sprite = Art("boarding_pass");
            RectTransform rect = card.rectTransform;
            UiBuilder.Place(rect, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(500f, 170f));
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localRotation = Quaternion.Euler(0f, 0f, 4f);
            var body = new Vector2(-70f, 0f);

            TMP_Text chapter = UiBuilder.Label("Chapter", rect, "Chapter 1", 28f, false);
            chapter.color = new Color32(226, 104, 88, 255);
            UiBuilder.Place(chapter.rectTransform, new Vector2(0.5f, 1f), body + new Vector2(0f, -22f), new Vector2(320f, 36f));

            TMP_Text country = UiBuilder.Label("Country", rect, "Vietnam", 50f, false);
            country.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitInstaller.TitleFontPath);
            country.enableAutoSizing = true;
            country.fontSizeMin = 30f;
            country.fontSizeMax = 50f;
            UiBuilder.Place(country.rectTransform, new Vector2(0.5f, 0.5f), body + new Vector2(0f, -2f), new Vector2(320f, 64f));

            TMP_Text subtitle = UiBuilder.Text("Subtitle", rect, "map.international", 22f, false);
            subtitle.color = new Color32(150, 128, 118, 255);
            UiBuilder.Place(subtitle.rectTransform, new Vector2(0.5f, 0f), body + new Vector2(0f, 22f), new Vector2(320f, 30f));

            card.gameObject.SetActive(false);
            ChapterCardView view = card.gameObject.AddComponent<ChapterCardView>();
            view.EditorLink(rect, chapter, country, subtitle);
            return view;
        }

        private static RouteMapDecor BuildDecor(RectTransform content)
        {
            RectTransform layer = UiBuilder.Stretch(UiBuilder.Rect("Sky", content));
            var cloudHolders = new RectTransform[CloudPool];
            var clouds = new Image[CloudPool];

            for (int i = 0; i < clouds.Length; i++)
            {
                clouds[i] = HeldPiece("Cloud " + (i + 1), layer, Art("cloud_a"), new Vector2(300f, 150f), out cloudHolders[i]);
            }

            var islandHolders = new RectTransform[IslandPool];
            var islands = new Image[IslandPool];

            for (int i = 0; i < islands.Length; i++)
            {
                islands[i] = HeldPiece("Island " + (i + 1), layer, Art(Islands[0]), new Vector2(360f, 300f), out islandHolders[i]);
            }

            var islandSprites = new Sprite[Islands.Length];

            for (int i = 0; i < islandSprites.Length; i++)
            {
                islandSprites[i] = Art(Islands[i]);
            }

            RouteMapDecor decor = layer.gameObject.AddComponent<RouteMapDecor>();
            decor.EditorLink(cloudHolders, clouds, islandHolders, islands, new[] { Art("cloud_a"), Art("cloud_b"), Art("cloud_c") },
                islandSprites);
            return decor;
        }

        /// <summary>A map piece in a holder: the map places the holder, the picture sits inside it.</summary>
        private static Image HeldPiece(string name, Transform parent, Sprite sprite, Vector2 size, out RectTransform holder)
        {
            holder = UiBuilder.Rect(name, parent);
            UiBuilder.Place(holder, new Vector2(0.5f, 0f), Vector2.zero, size);
            holder.pivot = new Vector2(0.5f, 0.5f);

            Image image = UiBuilder.Image("Picture", holder, "pill_cream", false);
            image.sprite = sprite;
            UiBuilder.Place(image.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            image.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            holder.gameObject.SetActive(false);
            return image;
        }

        /// <summary>The airline's planes crossing far off, a cat at the window: they fly over the map, not on it.</summary>
        private static AirlinerFlyby BuildFlyby(RectTransform panel)
        {
            RectTransform area = UiBuilder.Stretch(UiBuilder.Rect("Flyby", panel));
            var liveries = new Sprite[Liveries.Length];

            for (int i = 0; i < liveries.Length; i++)
            {
                liveries[i] = Import(HomeArtFolder + "/" + Liveries[i] + ".png");
            }

            Image plane = Centered("Airliner", area, liveries[0], Vector2.zero, new Vector2(260f, 172f));
            plane.gameObject.SetActive(false);

            AirlinerFlyby flyby = area.gameObject.AddComponent<AirlinerFlyby>();
            flyby.EditorLink(area, plane.rectTransform, plane, liveries);
            return flyby;
        }

        /// <summary>The airline's name over the map, within the safe area, at the top left.</summary>
        private static void BuildHeader(RectTransform panel)
        {
            RectTransform frame = UiBuilder.Stretch(UiBuilder.Rect("SafeFrame", panel));
            frame.gameObject.AddComponent<SafeArea>();

            Image plate = UiBuilder.Image("Header", frame, "pill_cream", true);
            // The top row's left, where Settings and the passport sit on the other tabs; they step down a row here.
            UiBuilder.Place(plate.rectTransform, new Vector2(0f, 1f), new Vector2(36f, -24f), new Vector2(470f, 124f));
            plate.rectTransform.localScale = Vector3.one;

            Image icon = UiBuilder.Icon("Plane", plate.transform, "plane", 52f);
            icon.color = Navy;
            icon.rectTransform.sizeDelta = new Vector2(68f, 68f);
            icon.rectTransform.anchoredPosition = new Vector2(-168f, 2f);

            TMP_Text airline = CenteredLabel("Airline", plate.transform, "AIRLINE POP", 46f, new Vector2(36f, 18f), new Vector2(340f, 58f), true);
            airline.color = Navy;

            TMP_Text subtitle = UiBuilder.Text("Routes", plate.transform, "map.title", 24f, false);
            UiBuilder.Place(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(36f, -28f), new Vector2(340f, 40f));
            subtitle.enableAutoSizing = true;
            subtitle.fontSizeMin = 16f;
            subtitle.fontSizeMax = 30f;
            subtitle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            subtitle.color = new Color32(150, 128, 118, 255);
        }
    }
}
