using Crystal;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Builds the gameplay HUD of GDD 9 in the AirLine Pop kit and installs it in the
    /// gameplay scene: Pause top left, the level in the middle, Undo top right, Restart at
    /// the bottom, plus the pause card and the win card with the companion cat.
    ///
    /// The old HUD is switched off, not deleted, and the old auto-advance after a win is
    /// switched off because the win card now owns that moment. Ads, store and skip code
    /// stay in the project for later (they are simply not on this HUD).
    /// </summary>
    public static class GameplayHudBuilder
    {
        public const string PrefabPath = "Assets/_Game/UI/AirlineHud.prefab";
        private const string ScenePath = "Assets/_SDK/Template/Scenes/Gameplay.unity";
        private const string GameplayPrefabPath = "Assets/_Game/Prefabs/SingleLineGameplay.prefab";
        private const string VipGuestPrefabPath = "Assets/_Game/Art/FlatCats/CatFlat_vip.prefab";
        private const string OldHudName = "Gameplay HUD";
        private const string WinRevealClipPath = "Assets/_Game/Asset_Resources/AudioClip/sfx_reward.ogg";
        private const string WinMilestoneClipPath = "Assets/_Game/Asset_Resources/AudioClip/gold completion.ogg";
        private const string WinCoinClipPath = "Assets/_Game/Asset_Resources/AudioClip/gold 1_01.ogg";

        private static readonly Vector2 Reference = new Vector2(1080f, 1920f);
        private static readonly Vector3 StagePosition = new Vector3(0f, -500f, 0f);

        [MenuItem("Tools/AirLine Pop/Build Gameplay HUD")]
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
            UiKitInstaller.Install();
            LocalizationInstaller.Install();
            var catalog = AssetDatabase.LoadAssetAtPath<CatCatalogSO>("Assets/_Game/Config/CatCatalog.asset");

            GameObject root = BuildHud(catalog);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            ConfigureGameplayPrefab();
            InstallInScene();
            return "Gameplay HUD built at " + PrefabPath + " and installed in " + ScenePath + ".";
        }

        private static GameObject BuildHud(CatCatalogSO catalog)
        {
            var root = new GameObject("Airline HUD");

            var canvasObject = new GameObject("Canvas");
            canvasObject.layer = LayerMask.NameToLayer("UI");
            canvasObject.transform.SetParent(root.transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = Reference;
            scaler.matchWidthOrHeight = 0.35f;
            canvasObject.AddComponent<GraphicRaycaster>();

            RectTransform safe = UiBuilder.Stretch(UiBuilder.Rect("SafeArea", canvasObject.transform));
            safe.gameObject.AddComponent<SafeArea>();

            RectTransform top = BuildTopBar(safe, out Button pauseButton, out TMP_Text levelLabel);
            CanvasGroup stuck = BuildStuckBanner(safe);
            TMP_Text hintLabel = BuildHintLabel(safe);
            RectTransform bottom = BuildBottomBar(safe, out Button restartButton, out Button hintButton);

            PausePanelView pausePanel = BuildPausePanel(canvasObject.transform);
            RuleIntroBuilder.Build(canvasObject.transform);
            canvasObject.AddComponent<VipFlightRecorder>();
            WinPanelView winPanel = BuildWinPanel(canvasObject.transform);
            CatPortraitStage stage = BuildStage(root.transform);

            GameplayHud hud = canvasObject.AddComponent<GameplayHud>();
            var hudData = new SerializedObject(hud);
            hudData.FindProperty("levelLabel").objectReferenceValue = levelLabel;
            hudData.FindProperty("progressLabel").objectReferenceValue = hintLabel;
            hudData.FindProperty("restartButton").objectReferenceValue = restartButton;
            hudData.FindProperty("hintButton").objectReferenceValue = hintButton;
            hudData.FindProperty("stuckBanner").objectReferenceValue = stuck;
            hudData.ApplyModifiedPropertiesWithoutUndo();

            HudInsetReporter insets = canvasObject.AddComponent<HudInsetReporter>();
            var insetData = new SerializedObject(insets);
            insetData.FindProperty("topBar").objectReferenceValue = top;
            insetData.FindProperty("bottomBar").objectReferenceValue = bottom;
            insetData.ApplyModifiedPropertiesWithoutUndo();

            PauseController pause = root.AddComponent<PauseController>();
            pause.EditorLink(pauseButton, pausePanel);
            WinSequenceController win = root.AddComponent<WinSequenceController>();
            win.EditorLink(winPanel, pause);
            DestinationInstaller.LinkWinRoutes(win, DestinationInstaller.BuildCatalog());
            root.AddComponent<WinCardAudio>().EditorLink(winPanel, Clip(WinRevealClipPath), Clip(WinMilestoneClipPath), Clip(WinCoinClipPath));
            GameplayHudBootstrap bootstrap = root.AddComponent<GameplayHudBootstrap>();
            bootstrap.EditorLink(catalog, pause, win, stage, AssetDatabase.LoadAssetAtPath<CatView>(VipGuestPrefabPath));
            return root;
        }

        private static AudioClip Clip(string path)
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        // ---------------------------------------------------------------- HUD bars

        // No Undo button: the player steps back by dragging back along the line.
        private static RectTransform BuildTopBar(RectTransform safe, out Button pause, out TMP_Text level)
        {
            RectTransform bar = UiBuilder.Rect("TopBar", safe);
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.offsetMin = new Vector2(0f, -170f);
            bar.offsetMax = Vector2.zero;

            pause = RoundButton("PauseButton", bar, "pause", new Vector2(0f, 0.5f), new Vector2(36f, -6f));

            Image pill = UiBuilder.Image("LevelPill", bar, "pill_cream", true);
            UiBuilder.Place(pill.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -6f), new Vector2(340f, 104f));
            level = UiBuilder.Label("Level", pill.transform, "Flight 1", 60f, false);
            level.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitInstaller.TitleFontPath);
            UiBuilder.Place(level.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(320f, 90f));
            // A special flight adds its picture's name, so the label shrinks to keep it in the pill.
            level.enableAutoSizing = true;
            level.fontSizeMin = 30f;
            level.fontSizeMax = 60f;

            PassengerInstaller.BuildCounter(bar);
            return bar;
        }

        private static Button RoundButton(string name, Transform parent, string icon, Vector2 anchor, Vector2 position)
        {
            Button button = UiBuilder.Button(name, parent, "btn_round_cream", new Vector2(124f, 124f));
            UiBuilder.Place((RectTransform)button.transform, anchor, position, new Vector2(124f, 124f));
            Image glyph = UiBuilder.Icon("Icon", button.transform, icon, 80f);
            glyph.color = UiBuilder.IconBlue;
            glyph.rectTransform.anchoredPosition = new Vector2(0f, 4f);
            return button;
        }

        private static CanvasGroup BuildStuckBanner(RectTransform safe)
        {
            Image pill = UiBuilder.Image("StuckBanner", safe, "pill_cream", true);
            // Below the passenger counter that hangs under the top bar, so the two never overlap.
            UiBuilder.Place(pill.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -270f), new Vector2(760f, 96f));
            TMP_Text text = UiBuilder.Text("Text", pill.transform, "hud.stuck", 40f, false);
            UiBuilder.Place(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(720f, 80f));
            CanvasGroup group = UiBuilder.Group(pill.rectTransform);
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            return group;
        }

        private static TMP_Text BuildHintLabel(RectTransform safe)
        {
            TMP_Text label = UiBuilder.Label("HintLabel", safe, string.Empty, 40f, false);
            UiBuilder.Place(label.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 250f), new Vector2(900f, 60f));
            return label;
        }

        /// <summary>Restart and Hint side by side; Hint wears the sky blue that marks a helping hand.</summary>
        private static RectTransform BuildBottomBar(RectTransform safe, out Button restart, out Button hint)
        {
            RectTransform bar = UiBuilder.Rect("BottomBar", safe);
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.offsetMin = Vector2.zero;
            bar.offsetMax = new Vector2(0f, 220f);

            restart = BarButton(bar, "RestartButton", "btn_cream", "restart", "common.restart", -196f, false);
            hint = BarButton(bar, "HintButton", "btn_sky", "bulb", "hud.hint", 196f, true);
            return bar;
        }

        private static Button BarButton(RectTransform bar, string name, string face, string icon, string labelKey, float x, bool isOnColour)
        {
            Button button = UiBuilder.Button(name, bar, face, new Vector2(360f, 140f));
            UiBuilder.Place((RectTransform)button.transform, new Vector2(0.5f, 0.5f), new Vector2(x, 10f), new Vector2(360f, 140f));
            Image glyph = UiBuilder.Icon("Icon", button.transform, icon, 76f);
            glyph.color = isOnColour ? Color.white : UiBuilder.IconBlue;
            UiBuilder.Place(glyph.rectTransform, new Vector2(0f, 0.5f), new Vector2(36f, 5f), new Vector2(76f, 76f));
            TMP_Text text = UiBuilder.Text("Label", button.transform, labelKey, 52f, isOnColour);
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitInstaller.TitleFontPath);
            UiBuilder.Place(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(38f, 6f), new Vector2(240f, 90f));
            return button;
        }

        // ---------------------------------------------------------------- cards

        /// <summary>Space a popup keeps from the screen's edges when it has to shrink to fit.</summary>
        public const float FitMargin = 24f;

        /// <summary>A popup: the dimmed backdrop and a cream card that springs up. Shared by every screen's popups.</summary>
        public static RectTransform Card(string name, Transform parent, Vector2 size, out ModalPanel modal)
        {
            RectTransform root = UiBuilder.Stretch(UiBuilder.Rect(name, parent));
            CanvasGroup group = UiBuilder.Group(root);

            UiBuilder.Backdrop(root, UiBuilder.ModalDim);

            // The wash covers the whole screen; the card keeps clear of notches. It sits in a
            // holder of its own size that shrinks to fit short or narrow screens, so the
            // card's own spring-in scale stays free.
            RectTransform safe = UiBuilder.SafeLayer(root);
            RectTransform fit = UiBuilder.Rect("Fit", safe);
            UiBuilder.Place(fit, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            safe.gameObject.AddComponent<ContentScaleFitter>().EditorLink(safe, fit, FitMargin);

            Image card = UiBuilder.Image("Card", fit, "panel_cream", true);
            UiBuilder.Place(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            card.raycastTarget = true;

            modal = root.gameObject.AddComponent<ModalPanel>();
            modal.EditorLink(group, card.rectTransform);
            return card.rectTransform;
        }

        private static Button WideButton(string name, Transform parent, string face, string labelKey, float y, bool isPrimary)
        {
            Vector2 size = isPrimary ? new Vector2(600f, 170f) : new Vector2(600f, 140f);
            Button button = UiBuilder.Button(name, parent, face, size);
            UiBuilder.Place((RectTransform)button.transform, new Vector2(0.5f, 0f), new Vector2(0f, y), size);
            TMP_Text text = UiBuilder.Text("Label", button.transform, labelKey, isPrimary ? 72f : 54f, isPrimary);

            if (!isPrimary)
            {
                text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitInstaller.TitleFontPath);
            }

            UiBuilder.Place(text.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), size - new Vector2(40f, 40f));
            return button;
        }

        private static PausePanelView BuildPausePanel(Transform canvas)
        {
            RectTransform card = Card("PausePanel", canvas, new Vector2(820f, 1300f), out ModalPanel modal);

            TMP_Text title = UiBuilder.Text("Title", card, "pause.title", 88f, false);
            title.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitInstaller.TitleFontPath);
            UiBuilder.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(760f, 130f));

            SettingToggleView music = ToggleRow(card, "MusicRow", "music", "settings.music", -210f);
            SettingToggleView sound = ToggleRow(card, "SoundRow", "sound", "settings.sound", -330f);
            SettingToggleView haptic = ToggleRow(card, "HapticRow", "vibrate", "settings.vibration", -450f);
            LanguageRow(card, -570f);

            // Orange is the one primary action colour, the same as Home's Play and the win's Continue.
            Button resume = WideButton("ResumeButton", card, "btn_orange", "pause.resume", 380f, true);
            Button restart = WideButton("RestartButton", card, "btn_cream", "common.restart", 215f, false);
            Button home = WideButton("HomeButton", card, "btn_cream", "common.backToAirport", 55f, false);

            PausePanelView view = modal.gameObject.AddComponent<PausePanelView>();
            view.EditorLink(modal, resume, restart, home, music, sound, haptic);
            return view;
        }

        /// <summary>
        /// The settings row that switches language: its name, and a button naming the current
        /// language in its own words. Shared by the pause screen and Home's settings.
        /// </summary>
        public static LanguageRowView LanguageRow(RectTransform card, float y)
        {
            RectTransform row = UiBuilder.Rect("LanguageRow", card);
            UiBuilder.Place(row, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(660f, 100f));

            Image glyph = UiBuilder.Icon("Icon", row, "globe", 72f);
            glyph.color = UiBuilder.IconBlue;
            UiBuilder.Place(glyph.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(72f, 72f));

            TMP_Text text = UiBuilder.Text("Label", row, "settings.language", 50f, false);
            text.alignment = TextAlignmentOptions.Left;
            UiBuilder.Place(text.rectTransform, new Vector2(0f, 0.5f), new Vector2(100f, 2f), new Vector2(300f, 80f));

            Button button = UiBuilder.Button("LanguageButton", row, "btn_sky", new Vector2(250f, 84f));
            UiBuilder.Place((RectTransform)button.transform, new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(250f, 84f));
            TMP_Text value = UiBuilder.Label("Value", button.transform, "English", 40f, true);
            UiBuilder.Place(value.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 5f), new Vector2(230f, 70f));
            value.enableAutoSizing = true;
            value.fontSizeMin = 26f;
            value.fontSizeMax = 40f;

            LanguageRowView view = row.gameObject.AddComponent<LanguageRowView>();
            view.EditorLink(button, value);
            return view;
        }

        /// <summary>One settings switch row: icon, name, switch. Shared by the pause screen and Home's settings.</summary>
        public static SettingToggleView ToggleRow(RectTransform card, string name, string icon, string labelKey, float y)
        {
            RectTransform row = UiBuilder.Rect(name, card);
            UiBuilder.Place(row, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(660f, 100f));

            Image glyph = UiBuilder.Icon("Icon", row, icon, 72f);
            glyph.color = UiBuilder.IconBlue;
            UiBuilder.Place(glyph.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(72f, 72f));

            TMP_Text text = UiBuilder.Text("Label", row, labelKey, 50f, false);
            text.alignment = TextAlignmentOptions.Left;
            UiBuilder.Place(text.rectTransform, new Vector2(0f, 0.5f), new Vector2(100f, 2f), new Vector2(360f, 80f));

            // A switch drawn whole, track and knob each at their own size, so nothing is
            // stretched or squeezed.
            Image track = UiBuilder.Image("Track", row, "toggle_on", false);
            track.raycastTarget = true;
            UiBuilder.Place(track.rectTransform, new Vector2(1f, 0.5f), new Vector2(-6f, 0f), new Vector2(136f, 72f));
            Image knob = UiBuilder.Image("Knob", track.transform, "toggle_knob", false);
            UiBuilder.Place(knob.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 3f), new Vector2(58f, 58f));

            Toggle toggle = track.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = track;
            toggle.transition = Selectable.Transition.None;

            SettingToggleView view = row.gameObject.AddComponent<SettingToggleView>();
            var data = new SerializedObject(view);
            data.FindProperty("toggle").objectReferenceValue = toggle;
            data.FindProperty("track").objectReferenceValue = track;
            data.FindProperty("knob").objectReferenceValue = knob;
            data.FindProperty("trackOn").objectReferenceValue = track.sprite;
            data.FindProperty("trackOff").objectReferenceValue = UiBuilder.Sprite("toggle_off");
            data.FindProperty("knobTravel").floatValue = 32f;
            data.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static WinPanelView BuildWinPanel(Transform canvas)
        {
            // Centred on the screen together with the cat standing on its top edge: the card
            // sits a little below the middle so the cat above it balances the pair.
            RectTransform card = Card("WinPanel", canvas, new Vector2(860f, 620f), out ModalPanel modal);
            card.anchoredPosition = new Vector2(0f, -110f);

            // The cat stands on the card's top edge, half out of it.
            RectTransform portraitRect = UiBuilder.Rect("CatPortrait", card);
            UiBuilder.Place(portraitRect, new Vector2(0.5f, 1f), new Vector2(0f, 300f), new Vector2(520f, 520f));
            RawImage portrait = portraitRect.gameObject.AddComponent<RawImage>();
            portrait.raycastTarget = false;

            TMP_Text title = UiBuilder.Label("Title", card, "Flight 1 is off!", 72f, false);
            title.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitInstaller.TitleFontPath);
            // Tall enough for two lines: the flight and destination, then the stamps.
            UiBuilder.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -176f), new Vector2(820f, 160f));

            Image row = UiBuilder.Image("RewardRow", card, "panel_sky", true);
            UiBuilder.Place(row.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -346f), new Vector2(440f, 150f));
            Image coin = UiBuilder.Icon("Coin", row.transform, "coin", 120f);
            UiBuilder.Place(coin.rectTransform, new Vector2(0f, 0.5f), new Vector2(30f, 4f), new Vector2(120f, 120f));
            TMP_Text reward = UiBuilder.Label("Amount", row.transform, "+80", 96f, true);
            UiBuilder.Place(reward.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(60f, 6f), new Vector2(280f, 130f));

            // Under the coins, on a replay that pays nothing.
            TMP_Text note = UiBuilder.Text("ReplayNote", card, "win.replayNote", 34f, false);
            UiBuilder.Place(note.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -510f), new Vector2(780f, 60f));

            WinPanelView view = modal.gameObject.AddComponent<WinPanelView>();
            view.EditorLink(modal, portrait, title, row.gameObject, reward, note);
            DestinationInstaller.BuildWinPostcard(card, portrait.rectTransform, view);
            return view;
        }

        // ---------------------------------------------------------------- cat stage

        private static CatPortraitStage BuildStage(Transform root)
        {
            var stageObject = new GameObject("CatPortraitStage");
            stageObject.transform.SetParent(root, false);
            stageObject.transform.position = StagePosition;

            Transform anchor = new GameObject("CatAnchor").transform;
            anchor.SetParent(stageObject.transform, false);
            anchor.localRotation = Quaternion.Euler(0f, 180f, 0f);

            var cameraObject = new GameObject("StageCamera");
            cameraObject.transform.SetParent(stageObject.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 1.3f, -5.0f);
            cameraObject.transform.LookAt(stageObject.transform.position + new Vector3(0f, 0.55f, 0f));
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 24f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 10f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            camera.enabled = false;

            // A local light so the cat is lit the same whatever the board scene uses.
            AddStageLight(stageObject.transform, "KeyLight", new Vector3(-1.0f, 2.4f, -2.4f), 6f);
            AddStageLight(stageObject.transform, "FillLight", new Vector3(1.6f, 1.2f, -2.2f), 2.5f);

            CatPortraitStage stage = stageObject.AddComponent<CatPortraitStage>();
            // Sized for the flat cat, which sits about 0.6 units tall at scale one.
            stage.EditorLink(camera, anchor, 2.6f);
            return stage;
        }

        private static void AddStageLight(Transform stage, string name, Vector3 localPosition, float intensity)
        {
            var lightObject = new GameObject(name);
            lightObject.transform.SetParent(stage, false);
            lightObject.transform.localPosition = localPosition;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 12f;
            light.intensity = intensity;
            light.color = new Color32(255, 251, 244, 255);
        }

        // ---------------------------------------------------------------- install

        /// <summary>The win card now advances levels and the economy pays first clears only.</summary>
        private static void ConfigureGameplayPrefab()
        {
            GameObject prefab = PrefabUtility.LoadPrefabContents(GameplayPrefabPath);

            foreach (WinAdvancePresenter presenter in prefab.GetComponentsInChildren<WinAdvancePresenter>(true))
            {
                presenter.enabled = false;
            }

            var economy = AssetDatabase.LoadAssetAtPath<EconomyConfigSO>("Assets/_Game/Config/EconomyConfig.asset");

            foreach (LevelBootstrap bootstrap in prefab.GetComponentsInChildren<LevelBootstrap>(true))
            {
                var data = new SerializedObject(bootstrap);
                data.FindProperty("economy").objectReferenceValue = economy;
                data.FindProperty("advancesAutomatically").boolValue = false;
                data.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(prefab, GameplayPrefabPath);
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        private static void InstallInScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.name == OldHudName)
                {
                    rootObject.SetActive(false);
                }
                else if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(rootObject) == PrefabPath)
                {
                    // Matched by source rather than name: saving a prefab renames its root.
                    Object.DestroyImmediate(rootObject);
                }
            }

            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
