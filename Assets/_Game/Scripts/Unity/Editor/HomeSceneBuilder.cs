using Crystal;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Builds the new Home scene (GDD 9): one 3D world holding the airport and the cat
    /// room, a camera that glides between them, and the Home UI (coins on top, Play at the
    /// bottom, Home / Cats / Shop bar), all wired to <see cref="HomeBootstrap"/>.
    ///
    /// Rerunning rebuilds the scene from scratch, so hand edits to it are lost; change the
    /// builder instead. Config assets are created once and kept.
    /// </summary>
    public static class HomeSceneBuilder
    {
        public const string ScenePath = "Assets/_Game/Scenes/Home.unity";
        private const string ConfigFolder = "Assets/_Game/Config";
        private const string ManagersPrefabPath = "Assets/_SDK/Content/Prefabs/MANAGERS.prefab";

        // Far enough from the airport that its wide lawn never shows above the lounge wall.
        private static readonly Vector3 RoomOrigin = new Vector3(90f, 0f, 0f);
        private static readonly Color SkyColor = new Color32(184, 224, 242, 255);
        private static readonly Vector2 Reference = new Vector2(1080f, 1920f);

        [MenuItem("Tools/AirLine Pop/Build Home Scene")]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorUtility.DisplayDialog("AirLine Pop", Build(), "OK");
        }

        public static string Build()
        {
            LocalizationInstaller.Install();
            ShopInstaller.Install();
            ThemeInstaller.Install();
            AccessoryInstaller.Install();
            CatHeadAnchorBaker.Bake();
            FlatDioramaBuilder.Build();
            FlatCatBuilder.Build();
            UiKitInstaller.Install();

            var economy = LoadOrCreate<EconomyConfigSO>(ConfigFolder + "/EconomyConfig.asset");
            var behaviour = LoadOrCreate<CatBehaviourConfigSO>(ConfigFolder + "/CatBehaviourConfig.asset");
            CatCatalogSO catalog = BuildCatalog();
            ConfigureRegulars(catalog);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ManagersPrefabPath));
            BuildLighting();

            Transform rig = BuildCamera(out Transform viewer);
            Transform airportAnchor = Anchor("AirportView", FlatDioramaBuilder.AirportCameraPosition, FlatDioramaBuilder.AirportLookAt);
            // Flatter and further back than a top-down view, so the lounge's window wall fills
            // the top of the screen. Its pitch must match FlatDioramaBuilder.RoomPitch.
            Transform roomAnchor = Anchor("RoomView", RoomOrigin + new Vector3(0f, 10.2f, -15.2f), RoomOrigin + new Vector3(0f, 0.9f, 1.0f));

            // Flat cats turn to face the camera.
            CatRoster airportCats = BuildAirport(behaviour, viewer);
            CatRoster roomCats = BuildRoom(behaviour, viewer, out DeparturesBoardView departures);

            HomeUi ui = BuildCanvas();
            BuildEventSystem();

            var tabs = new GameObject("HomeTabs").AddComponent<HomeTabController>();
            tabs.EditorLink(rig, ui.Nav, new[]
            {
                new HomeTabStage(HomeTab.Airport, airportAnchor, ui.AirportPanel),
                new HomeTabStage(HomeTab.Cats, roomAnchor, ui.RoomPanel),
                new HomeTabStage(HomeTab.Shop, airportAnchor, ui.ShopPanel),
                new HomeTabStage(HomeTab.Map, airportAnchor, ui.MapPanel),
            });
            rig.SetPositionAndRotation(airportAnchor.position, airportAnchor.rotation);

            // On the route map the airline's header takes the top row; Settings and the stamp album step down a row.
            StepDownOnMap(tabs, ui.SettingsButton, new Vector2(36f, -168f));
            StepDownOnMap(tabs, ui.AlbumButton, new Vector2(164f, -168f));

            var bootstrap = new GameObject("HomeBootstrap").AddComponent<HomeBootstrap>();
            bootstrap.EditorLink(economy, catalog, ui.Coins, ui.Airport, tabs, airportCats, roomCats);
            bootstrap.EditorLinkRoutes(ui.Routes, ui.Album);
            bootstrap.EditorLinkRouteMap(ui.RouteMap);
            bootstrap.EditorLinkSettings(ui.Settings);
            bootstrap.EditorLinkShop(ui.Shop, AssetDatabase.LoadAssetAtPath<LiveryCatalogSO>(ShopInstaller.CatalogPath),
                AssetDatabase.LoadAssetAtPath<SkinCatalogSO>(ThemeInstaller.CatalogPath));

            // Tapping a regular in the lounge opens its card.
            var lounge = new GameObject("LoungeCare");
            CatTapInput tapInput = lounge.AddComponent<CatTapInput>();
            tapInput.EditorLink(viewer.GetComponent<Camera>(), roomCats, tabs);
            CatCarePresenter care = lounge.AddComponent<CatCarePresenter>();
            ui.CareBubble.EditorLinkCamera(viewer.GetComponent<Camera>());
            ui.CatMenu.EditorLinkCamera(viewer.GetComponent<Camera>());
            care.EditorLink(tapInput, ui.CatMenu, tabs, economy, viewer, ui.CareBubble);
            BuildTouchReactor(lounge, tapInput, viewer);
            BuildPerks(lounge, bootstrap, care, tapInput, viewer, ui, roomCats, airportCats);
            BuildVipGuest(roomCats.transform.parent, viewer, bootstrap);
            bootstrap.EditorLinkLounge(care, ui.ArrivalToast, departures);
            GameAudioInstaller.AddMusic(new GameObject("Music"), AssetDatabase.LoadAssetAtPath<AudioClip>(GameAudioInstaller.MusicPath));

            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterInBuild();
            return "Home scene built at " + ScenePath + " and set as the Home scene in Build Settings.";
        }

        /// <summary>
        /// The loyalty perks: accessories the regulars wear and choose from the menu's Dress
        /// up, and the daily gift box over a regular with coins for the player.
        /// </summary>
        private static void BuildPerks(GameObject lounge, HomeBootstrap bootstrap, CatCarePresenter care, CatTapInput tapInput,
            Transform viewer, HomeUi ui, CatRoster roomCats, CatRoster airportCats)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<AccessoryCatalogSO>(AccessoryInstaller.CatalogPath);

            LoungeWardrobePresenter dresser = lounge.AddComponent<LoungeWardrobePresenter>();
            dresser.EditorLink(new[] { roomCats, airportCats });

            CatWardrobePickerView picker = LoungeUiBuilder.BuildWardrobePicker(ui.CatMenu);
            CatWardrobePresenter wardrobe = lounge.AddComponent<CatWardrobePresenter>();
            wardrobe.EditorLink(care, ui.CatMenu, picker, UiBuilder.Sprite("icon_close"));

            const int boxCount = 4;
            Sprite boxArt = AssetDatabase.LoadAssetAtPath<Sprite>(AccessoryInstaller.GiftBoxArt);
            var holder = new GameObject("GiftBoxes");
            holder.transform.SetParent(lounge.transform, false);
            var boxes = new SpriteRenderer[boxCount];

            for (int i = 0; i < boxCount; i++)
            {
                var box = new GameObject("Gift " + (i + 1));
                box.transform.SetParent(holder.transform, false);
                boxes[i] = box.AddComponent<SpriteRenderer>();
                boxes[i].sprite = boxArt;
                boxes[i].sortingOrder = 150;
                boxes[i].enabled = false;
            }

            CatGiftMarkersView markers = holder.AddComponent<CatGiftMarkersView>();
            markers.EditorLink(boxes, viewer);
            CatGiftPopView pop = LoungeUiBuilder.BuildGiftPop(ui.Safe, viewer.GetComponent<Camera>());
            CatGiftPresenter gifts = lounge.AddComponent<CatGiftPresenter>();
            gifts.EditorLink(tapInput, roomCats, markers, pop);

            bootstrap.EditorLinkPerks(catalog, dresser, wardrobe, gifts);
        }

        /// <summary>
        /// A touched cat rolls, rubs, purrs or hops, with hearts rising over it. The hearts are
        /// made here once and reused, sized to a cat's head whatever the icon's own size.
        /// </summary>
        private const string VipGuestId = "vip";
        private const string VipSealArt = "Assets/_Game/Art/Flat/vip_seal.png";

        /// <summary>
        /// The VIP guest who visits the room after a VIP flight: the crowned cat standing near
        /// the front of the room, clear of the regulars' usual spots, with a gold seal over it.
        /// </summary>
        private static void BuildVipGuest(Transform room, Transform viewer, HomeBootstrap bootstrap)
        {
            CatView prefab = FlatCatBuilder.BuildGuest(VipGuestId);

            if (prefab == null)
            {
                Debug.LogWarning("No VIP guest drawings; run Tools/flat_art/import_craftpix_cats.py.");
                return;
            }

            var holder = new GameObject("VipGuest");
            holder.transform.SetParent(room, false);
            holder.transform.localPosition = new Vector3(HomeDioramaBuilder.RoomWidth * 0.5f - 1.5f, 0.04f, -HomeDioramaBuilder.RoomDepth * 0.5f + 2.2f);

            var guest = (CatView)PrefabUtility.InstantiatePrefab(prefab, holder.transform);
            guest.transform.localScale = Vector3.one * 2.2f;

            var seal = new GameObject("Seal");
            seal.transform.SetParent(guest.transform.Find("Visual"), false);
            seal.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            seal.transform.localScale = Vector3.one * 0.15f;
            SpriteRenderer sealRenderer = seal.AddComponent<SpriteRenderer>();
            sealRenderer.sprite = SpriteImport.Import(VipSealArt, 128f);
            sealRenderer.sortingOrder = 20;

            VipGuestView view = holder.AddComponent<VipGuestView>();
            view.EditorLink(guest, seal.transform, viewer);
            bootstrap.EditorLinkVipGuest(view);
        }

        private static void BuildTouchReactor(GameObject lounge, CatTapInput tapInput, Transform viewer)
        {
            const int heartCount = 8;
            const float heartWorldSize = 0.42f;
            Sprite heartArt = UiBuilder.Sprite("icon_heart");
            var holder = new GameObject("Hearts");
            holder.transform.SetParent(lounge.transform, false);
            var hearts = new SpriteRenderer[heartCount];

            for (int i = 0; i < heartCount; i++)
            {
                var heart = new GameObject("Heart " + (i + 1));
                heart.transform.SetParent(holder.transform, false);
                hearts[i] = heart.AddComponent<SpriteRenderer>();
                hearts[i].sprite = heartArt;
                hearts[i].sortingOrder = 200;
                hearts[i].enabled = false;
            }

            float scale = heartArt != null && heartArt.bounds.size.x > 0f ? heartWorldSize / heartArt.bounds.size.x : 1f;
            lounge.AddComponent<CatTouchReactor>().EditorLink(tapInput, viewer, hearts, scale);
        }

        private static void StepDownOnMap(HomeTabController tabs, Button button, Vector2 mapPosition)
        {
            var rect = (RectTransform)button.transform;
            button.gameObject.AddComponent<TabPlacement>().EditorLink(tabs, rect, HomeTab.Map, mapPosition);
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);

            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static CatCatalogSO BuildCatalog()
        {
            var catalog = LoadOrCreate<CatCatalogSO>(ConfigFolder + "/CatCatalog.asset");
            var breeds = new System.Collections.Generic.List<CatBreedSO>();

            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(CatBreedSO), new[] { ConfigFolder + "/Cats" }))
            {
                breeds.Add(AssetDatabase.LoadAssetAtPath<CatBreedSO>(AssetDatabase.GUIDToAssetPath(guid)));
            }

            // The starter first, then in the order regulars join the lounge.
            breeds.Sort((a, b) => a.IsOwnedByDefault != b.IsOwnedByDefault
                ? (a.IsOwnedByDefault ? -1 : 1)
                : a.ArrivesAfterFlight.CompareTo(b.ArrivesAfterFlight));
            catalog.EditorSetBreeds(breeds.ToArray());
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        private static void BuildLighting()
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color32(255, 251, 244, 255);
            sun.intensity = 1.05f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.45f;
            sun.transform.rotation = Quaternion.Euler(52f, -32f, 0f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color32(160, 182, 204, 255);
            RenderSettings.ambientEquatorColor = new Color32(146, 150, 148, 255);
            RenderSettings.ambientGroundColor = new Color32(92, 90, 86, 255);
        }

        private static Transform BuildCamera(out Transform viewer)
        {
            var rig = new GameObject("CameraRig").transform;
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(rig, false);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 34f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 120f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = SkyColor;
            cameraObject.AddComponent<AudioListener>();
            viewer = cameraObject.transform;
            return rig;
        }

        private static Transform Anchor(string name, Vector3 position, Vector3 lookAt)
        {
            Transform anchor = new GameObject(name).transform;
            anchor.position = position;
            anchor.LookAt(lookAt);
            return anchor;
        }

        private static GameObject Spawn(string prefabPath, Transform parent, Vector3 localPosition, float yaw = 0f)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath), parent);
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return instance;
        }

        private static CatRoster BuildAirport(CatBehaviourConfigSO behaviour, Transform viewer)
        {
            Transform root = new GameObject("Airport").transform;
            Spawn(HomeDioramaBuilder.AirportPrefabPath, root, Vector3.zero).isStatic = true;
            Spawn(HomeDioramaBuilder.PlanePrefabPath, root, FlatDioramaBuilder.PlaneSpot, FlatDioramaBuilder.PlaneYaw);

            Vector3[] clouds = FlatDioramaBuilder.CloudSpots;

            for (int i = 0; i < clouds.Length; i++)
            {
                GameObject cloud = Spawn(HomeDioramaBuilder.CloudPrefabPath, root, clouds[i]);
                cloud.transform.localScale = Vector3.one * (1.3f + 0.15f * (i % 3));

                // Each cloud drifts its own way and pace, wide enough to see from the apron.
                cloud.AddComponent<CloudDrift>().EditorConfigure((i % 2 == 0 ? 1.6f : -1.4f), 12f + 3f * i);
            }

            // Passengers wait on the apron in front of the gate desk.
            WalkArea area = new GameObject("GateQueue").AddComponent<WalkArea>();
            area.transform.SetParent(root, false);
            area.transform.localPosition = FlatDioramaBuilder.QueueCentre;
            area.EditorConfigure(FlatDioramaBuilder.QueueSize, new Rect[0]);

            // The airport is seen from further away than the room, so its cats are drawn larger.
            return BuildRoster("AirportCats", root, area, behaviour, 1.9f, 2, viewer);
        }

        private static CatRoster BuildRoom(CatBehaviourConfigSO behaviour, Transform viewer, out DeparturesBoardView departures)
        {
            Transform root = new GameObject("CatRoom").transform;
            root.position = RoomOrigin;
            GameObject lounge = Spawn(HomeDioramaBuilder.RoomPrefabPath, root, Vector3.zero);
            lounge.isStatic = true;
            departures = lounge.GetComponentInChildren<DeparturesBoardView>(true);
            CatSpot[] spots = lounge.GetComponentsInChildren<CatSpot>(true);

            WalkArea area = new GameObject("RoomFloor").AddComponent<WalkArea>();
            area.transform.SetParent(root, false);
            area.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            area.EditorConfigure(new Vector2(HomeDioramaBuilder.RoomWidth - 2.4f, HomeDioramaBuilder.RoomDepth - 1.0f), FlatDioramaBuilder.LoungeBlocked());

            CatRoster roster = BuildRoster("RoomCats", root, area, behaviour, 2.2f, 4, viewer);
            roster.EditorLinkSpots(spots);
            return roster;
        }

        private static CatRoster BuildRoster(string name, Transform root, WalkArea area, CatBehaviourConfigSO behaviour, float scale, int limit,
            Transform viewer)
        {
            Transform container = new GameObject(name).transform;
            container.SetParent(root, false);
            CatRoster roster = container.gameObject.AddComponent<CatRoster>();
            roster.EditorLink(area, behaviour, container, scale, limit, viewer);
            return roster;
        }

        private static void BuildEventSystem()
        {
            var events = new GameObject("EventSystem");
            events.AddComponent<EventSystem>();
            events.AddComponent<InputSystemUIInputModule>();
        }

        /// <summary>References the builders below hand back to the scene wiring.</summary>
        private sealed class HomeUi
        {
            public CoinCounterView Coins;
            public AirportPanelView Airport;
            public BottomNavView Nav;
            public CanvasGroup AirportPanel;
            public CanvasGroup RoomPanel;
            public CanvasGroup ShopPanel;
            public ShopPresenter Shop;
            public DestinationCatalogSO Routes;
            public StampAlbumView Album;
            public Button SettingsButton;
            public Button AlbumButton;
            public CanvasGroup MapPanel;
            public RouteMapView RouteMap;
            public SettingsPanelView Settings;
            public CatMenuView CatMenu;
            public ArrivalToastView ArrivalToast;
            public CatCareBubbleView CareBubble;
            public CanvasGroup LoungeHint;
            public RectTransform Safe;
        }

        /// <summary>
        /// When each regular starts coming to the lounge, in flights flown. The starter is
        /// there from the beginning; Captain Butter, the airline's mascot, drops in last.
        /// </summary>
        private static void ConfigureRegulars(CatCatalogSO catalog)
        {
            var arrivals = new System.Collections.Generic.Dictionary<string, int>
            {
                { "muop", 0 }, { "tro", 6 }, { "kem", 15 }, { "mun", 30 }, { "bo", 50 },
            };

            foreach (CatBreedSO breed in catalog.Breeds)
            {
                if (breed != null && arrivals.TryGetValue(breed.Id, out int afterFlight))
                {
                    breed.EditorSetArrival(afterFlight);
                    EditorUtility.SetDirty(breed);
                }
            }

            AssetDatabase.SaveAssets();
        }

        private static HomeUi BuildCanvas()
        {
            var canvasObject = new GameObject("HomeCanvas");
            canvasObject.layer = LayerMask.NameToLayer("UI");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Reference;
            scaler.matchWidthOrHeight = 0.35f;
            canvasObject.AddComponent<GraphicRaycaster>();

            RectTransform safe = UiBuilder.Stretch(UiBuilder.Rect("SafeArea", canvasObject.transform));
            safe.gameObject.AddComponent<SafeArea>();

            // Popups go on their own full-screen layer over everything, so their wash covers
            // the notch and home bar too; each keeps its card inside the safe area itself.
            RectTransform modals = UiBuilder.Stretch(UiBuilder.Rect("Modals", canvasObject.transform));

            var ui = new HomeUi { Safe = safe };
            ui.AirportPanel = BuildAirportPanel(safe, ui);
            ui.RoomPanel = BuildRoomPanel(safe, ui);
            ui.ShopPanel = ShopUiBuilder.Build(safe, AssetDatabase.LoadAssetAtPath<LiveryCatalogSO>(ShopInstaller.CatalogPath),
                AssetDatabase.LoadAssetAtPath<SkinCatalogSO>(ThemeInstaller.CatalogPath),
                AssetDatabase.LoadAssetAtPath<EconomyConfigSO>(ShopInstaller.EconomyPath), out ui.Shop);
            ui.Coins = BuildCoinPill(safe);
            Button settingsButton = BuildSettingsButton(safe);
            ui.SettingsButton = settingsButton;
            ui.Nav = BuildNav(safe);
            ui.CatMenu = LoungeUiBuilder.BuildCatMenu(modals);
            ui.CatMenu.EditorLinkHints(new[] { ui.LoungeHint });
            ui.ArrivalToast = LoungeUiBuilder.BuildArrivalToast(safe);
            ui.CareBubble = LoungeUiBuilder.BuildCareBubble(safe);
            ui.Routes = DestinationInstaller.BuildCatalog();
            ui.Album = StampAlbumUiBuilder.Build(safe, modals, ui.Routes, out ui.AlbumButton);

            // Behind the safe area, so its sky fills the whole screen under the coins and the bar.
            ui.MapPanel = RouteMapUiBuilder.Build(canvasObject.transform, out ui.RouteMap);

            // Built last so it sits over everything, on whichever tab is showing.
            ui.Settings = BuildSettingsPanel(modals, settingsButton);
            modals.SetAsLastSibling();
            return ui;
        }

        private static CoinCounterView BuildCoinPill(RectTransform safe)
        {
            Image pill = UiBuilder.Image("CoinPill", safe, "pill_cream", true);
            UiBuilder.Place(pill.rectTransform, new Vector2(1f, 1f), new Vector2(-36f, -36f), new Vector2(300f, 96f));

            Image icon = UiBuilder.Icon("Icon", pill.transform, "coin", 104f);
            UiBuilder.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(-26f, 4f), new Vector2(104f, 104f));

            TMP_Text amount = UiBuilder.Label("Amount", pill.transform, "0", 58f, false);
            UiBuilder.Place(amount.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(34f, 4f), new Vector2(200f, 80f));
            amount.alignment = TextAlignmentOptions.Center;
            amount.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitInstaller.TitleFontPath);

            CoinCounterView view = pill.gameObject.AddComponent<CoinCounterView>();
            view.EditorLink(amount, icon.rectTransform);
            return view;
        }

        private static Button BuildSettingsButton(RectTransform safe)
        {
            Button button = UiBuilder.Button("SettingsButton", safe, "btn_round_cream", new Vector2(112f, 112f));
            UiBuilder.Place((RectTransform)button.transform, new Vector2(0f, 1f), new Vector2(36f, -28f), new Vector2(112f, 112f));
            Image icon = UiBuilder.Icon("Icon", button.transform, "settings", 76f);
            icon.color = UiBuilder.IconBlue;
            icon.rectTransform.anchoredPosition = new Vector2(0f, 4f);
            return button;
        }

        /// <summary>Music, sound and vibration, as on the pause screen, behind the gear.</summary>
        private static SettingsPanelView BuildSettingsPanel(RectTransform modals, Button open)
        {
            // The view lives on an always-active holder: the popup itself is switched off while
            // closed, and a view on it would never hear the gear.
            RectTransform holder = UiBuilder.Stretch(UiBuilder.Rect("Settings", modals));
            RectTransform card = GameplayHudBuilder.Card("SettingsPanel", holder, new Vector2(820f, 820f), out ModalPanel modal);
            Button backdrop = modal.transform.Find("Backdrop").gameObject.AddComponent<Button>();
            backdrop.transition = Selectable.Transition.None;

            TMP_Text title = UiBuilder.Text("Title", card, "settings.title", 88f, false);
            title.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitInstaller.TitleFontPath);
            UiBuilder.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(600f, 130f));

            Button close = UiBuilder.Button("CloseButton", card, "btn_round_cream", new Vector2(100f, 100f));
            UiBuilder.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(-28f, -28f), new Vector2(100f, 100f));
            UiBuilder.Icon("Icon", close.transform, "close", 60f).color = UiBuilder.IconBlue;

            SettingToggleView music = GameplayHudBuilder.ToggleRow(card, "MusicRow", "music", "settings.music", -230f);
            SettingToggleView sound = GameplayHudBuilder.ToggleRow(card, "SoundRow", "sound", "settings.sound", -370f);
            SettingToggleView haptic = GameplayHudBuilder.ToggleRow(card, "HapticRow", "vibrate", "settings.vibration", -510f);
            GameplayHudBuilder.LanguageRow(card, -650f);

            SettingsPanelView view = holder.gameObject.AddComponent<SettingsPanelView>();
            view.EditorLink(modal, open, close, backdrop, music, sound, haptic);
            return view;
        }

        // The kit's pink face tinted towards the airline's coral.
        private static readonly Color TitleBannerTint = new Color(1f, 0.62f, 0.55f, 1f);

        private static CanvasGroup BuildAirportPanel(RectTransform safe, HomeUi ui)
        {
            RectTransform panel = UiBuilder.Stretch(UiBuilder.Rect("AirportPanel", safe));
            CanvasGroup group = UiBuilder.Group(panel);

            // The game's name on a banner in the airline's coral, on its own row under the top bar's
            // buttons, so it stands clear of them and of the pale airport behind.
            Image banner = UiBuilder.Image("TitleBanner", panel, "btn_pink", true);
            banner.color = TitleBannerTint;
            UiBuilder.Place(banner.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -210f), new Vector2(720f, 146f));
            TMP_Text title = UiBuilder.Label("Title", panel, "AirLine Pop", 118f, true);
            UiBuilder.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -201f), new Vector2(900f, 160f));

            // Play is a boarding pass: the flight on the ticket, a plane on its stub.
            Button play = UiBuilder.Button("PlayButton", panel, UiBuilder.TicketFace, new Vector2(620f, 210f));
            UiBuilder.Place((RectTransform)play.transform, new Vector2(0.5f, 0f), new Vector2(0f, 250f), new Vector2(620f, 210f));

            RectTransform content = UiBuilder.Stretch(UiBuilder.Rect("Content", play.transform));
            TMP_Text playLabel = UiBuilder.Text("Label", content, "home.fly", 96f, true);
            UiBuilder.Place(playLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-82f, 30f), new Vector2(420f, 110f));
            TMP_Text level = UiBuilder.Label("Level", content, "Flight 1", 46f, false);
            UiBuilder.Place(level.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-82f, -48f), new Vector2(420f, 64f));
            level.enableAutoSizing = true;
            level.fontSizeMin = 30f;
            level.fontSizeMax = 46f;
            level.color = UiBuilder.TextCream;
            Image stub = UiBuilder.Icon("Plane", content, "plane", 104f);
            stub.rectTransform.anchoredPosition = new Vector2(229f, 8f);
            stub.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -45f);

            AirportPanelView view = panel.gameObject.AddComponent<AirportPanelView>();
            view.EditorLink(play, level, content);
            ui.Airport = view;
            return group;
        }

        private static CanvasGroup BuildRoomPanel(RectTransform safe, HomeUi ui)
        {
            RectTransform panel = UiBuilder.Stretch(UiBuilder.Rect("RoomPanel", safe));
            CanvasGroup group = UiBuilder.Group(panel);

            TMP_Text title = UiBuilder.Text("Title", panel, "lounge.title", 96f, true);
            UiBuilder.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(900f, 130f));

            Image hint = UiBuilder.Image("TapHint", panel, "pill_cream", true);
            UiBuilder.Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 300f), new Vector2(600f, 96f));
            ui.LoungeHint = UiBuilder.Group(hint.rectTransform);
            Image paw = UiBuilder.Icon("Paw", hint.transform, "paw", 64f);
            paw.color = new Color32(242, 160, 170, 255);
            UiBuilder.Place(paw.rectTransform, new Vector2(0f, 0.5f), new Vector2(34f, 4f), new Vector2(64f, 64f));
            TMP_Text text = UiBuilder.Text("Text", hint.transform, "lounge.hint", 46f, false);
            UiBuilder.Place(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(40f, 4f), new Vector2(460f, 70f));
            return group;
        }

        private static BottomNavView BuildNav(RectTransform safe)
        {
            Image bar = UiBuilder.Image("BottomNav", safe, "nav_bar", true);
            RectTransform rect = bar.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(48f, 28f);
            rect.offsetMax = new Vector2(-48f, 172f);
            bar.raycastTarget = true;

            (HomeTab tab, string icon, string label)[] items =
            {
                (HomeTab.Airport, "airport", "nav.airport"),
                (HomeTab.Cats, "paw", "nav.lounge"),
                (HomeTab.Shop, "shop", "nav.shop"),
                (HomeTab.Map, "map", "nav.map"),
            };

            var buttons = new NavTabButton[items.Length];

            for (int i = 0; i < items.Length; i++)
            {
                buttons[i] = BuildNavTab(bar.transform, items[i].tab, items[i].icon, items[i].label, i, items.Length);
            }

            BottomNavView nav = bar.gameObject.AddComponent<BottomNavView>();
            nav.EditorLink(buttons);
            return nav;
        }

        // An idle tab's icon fades back into the bar rather than changing colour.
        private static readonly Color IdleTabIcon = new Color(1f, 1f, 1f, 0.55f);

        private static NavTabButton BuildNavTab(Transform bar, HomeTab tab, string icon, string labelKey, int index, int count)
        {
            RectTransform slot = UiBuilder.Rect(tab + "Tab", bar);
            slot.anchorMin = new Vector2(index / (float)count, 0f);
            slot.anchorMax = new Vector2((index + 1) / (float)count, 1f);
            slot.offsetMin = Vector2.zero;
            slot.offsetMax = Vector2.zero;

            Image hit = slot.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            Button button = slot.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;

            RectTransform content = UiBuilder.Rect("Content", slot);
            UiBuilder.Place(content, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(196f, 112f));

            // The menu icons are drawn in their own colours and never tinted.
            Image glyph = UiBuilder.Icon("Icon", content, "nav_" + icon, 72f);
            glyph.rectTransform.anchoredPosition = new Vector2(0f, 26f);

            TMP_Text text = UiBuilder.Text("Label", content, labelKey, 28f, false);
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitInstaller.TitleFontPath);
            // A clear gap under the icon, so the name never touches it.
            UiBuilder.Place(text.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 2f), new Vector2(196f, 40f));

            // Selected: a small unframed marker under the name, so no frame sits in the bar's frame.
            Image plate = UiBuilder.Image("Marker", content, "tab_marker", false);
            UiBuilder.Place(plate.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, -10f), new Vector2(44f, 10f));

            NavTabButton tabButton = slot.gameObject.AddComponent<NavTabButton>();
            tabButton.EditorLink(tab, button, plate, content, text, glyph, Color.white, IdleTabIcon, UiBuilder.IconBlue, UiBuilder.IconIdle);
            return tabButton;
        }

        /// <summary>Puts the new Home in the build in place of the template's Home scene, which keeps its file.</summary>
        private static void RegisterInBuild()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            bool isListed = false;

            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == ScenePath)
                {
                    scenes[i].enabled = true;
                    isListed = true;
                }
                else if (System.IO.Path.GetFileNameWithoutExtension(scenes[i].path) == "Home")
                {
                    scenes[i].enabled = false;
                }
            }

            if (!isListed)
            {
                var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes);
                list.Insert(Mathf.Min(1, list.Count), new EditorBuildSettingsScene(ScenePath, true));
                scenes = list.ToArray();
            }

            EditorBuildSettings.scenes = scenes;
        }
    }
}
