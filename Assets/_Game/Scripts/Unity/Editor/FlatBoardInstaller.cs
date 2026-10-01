using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Switches the gameplay board to the flat 2D look: sticker tiles, a runway drawn along
    /// the path, a sprite airplane with the cat at the controls, and a sky with drifting
    /// clouds behind a straight-down camera.
    ///
    /// The art comes from Tools/flat_art/generate_flat_art.py. The 3D scenery is switched
    /// off rather than deleted, so the old look can still be brought back. Safe to rerun.
    /// </summary>
    public static class FlatBoardInstaller
    {
        private const string ArtFolder = "Assets/_Game/Art/Flat";
        private const string GameplayPrefabPath = "Assets/_Game/Prefabs/SingleLineGameplay.prefab";
        private const string CellPrefabPath = "Assets/_Game/Prefabs/CellFlat.prefab";
        private const string ThemePath = "Assets/_Game/Config/Theme.asset";
        private const string SkinFolder = "Assets/_Game/Config/Skins";
        private const string SolidLineMaterialPath = "Assets/_Game/Asset_Resources/Material/mat_line.mat";
        private const string RouteMaterialPath = ArtFolder + "/mat_runway_dash.mat";
        private const string SurfaceMaterialPath = ArtFolder + "/mat_runway_surface.mat";

        private const float TilePixelsPerUnit = 256f;
        private const float PlanePixelsPerUnit = 512f;
        private const float CloudPixelsPerUnit = 100f;

        // Runway widths in world units against a one-unit tile: wide enough to read as a
        // runway, narrow enough to leave a good band of the tile showing either side.
        private const float RunwayEdgeWidth = 0.44f;
        private const float RunwayWidth = 0.37f;
        private const float CentreLineWidth = 0.065f;

        // The hint's paper plane against a one-unit tile, and its trail of puffs: enough
        // puffs for three squares and the step out of the plane's own.
        private const float HintGuideScale = 0.8f;
        private const float HintPuffScale = 0.75f;
        private const float HintPuffSpacing = 0.24f;
        private const int HintPuffCount = 18;

        // One run of the surface texture, a lamp each side, every this many world units.
        private const float RunwayLightSpacing = 0.5f;
        private const int RunwayRoundness = 8;

        // The passengers' outline brown, as on every other piece of the board.
        private static readonly Color OutlineBrown = new Color32(100, 56, 53, 255);

        // Neutral sky, per the "no yellow cast" rule: cool blue, near-white cream tiles.
        private static readonly Color SkyColor = new Color32(160, 212, 240, 255);
        private static readonly Color TileCream = new Color32(255, 250, 241, 255);

        [MenuItem("Tools/AirLine Pop/Install Flat Board (2D)")]
        public static void InstallFromMenu()
        {
            Debug.Log(Install());
        }

        public static string Install()
        {
            var solidLine = AssetDatabase.LoadAssetAtPath<Material>(SolidLineMaterialPath);

            if (solidLine == null)
            {
                return "Missing " + SolidLineMaterialPath + ".";
            }

            var art = new FlatArt
            {
                TileFace = Sprite("tile_face", TilePixelsPerUnit),
                TileOverlay = Sprite("tile_overlay", TilePixelsPerUnit),
                TileShadow = Sprite("tile_shadow", TilePixelsPerUnit),
                TileFlash = Sprite("tile_flash", TilePixelsPerUnit),
                StartRing = Sprite("start_ring", TilePixelsPerUnit),
                StartDot = Sprite("start_dot", TilePixelsPerUnit),
                Spark = Sprite("spark_glow", 128f),
                PaperPlane = Sprite("paper_plane", TilePixelsPerUnit),
                HintPuff = Sprite("hint_puff", TilePixelsPerUnit),
                Airplane = Sprite("airplane", PlanePixelsPerUnit),
                AirplaneShadow = Sprite("airplane_shadow", PlanePixelsPerUnit),
                Pilot = Sprite("airplane_pilot", PlanePixelsPerUnit),
                Sky = Sprite("sky_gradient", 4f),
                Clouds = new[]
                {
                    Sprite("cloud_0", CloudPixelsPerUnit),
                    Sprite("cloud_1", CloudPixelsPerUnit),
                    Sprite("cloud_2", CloudPixelsPerUnit),
                },
            };

            Texture2D dash = ImportDashTexture(ArtFolder + "/runway_dash.png");
            Material route = AssetWriter.SaveMaterial(RouteMaterialPath, new Material(solidLine) { mainTexture = dash });
            Texture2D lights = ImportDashTexture(ArtFolder + "/runway_lights.png");
            Material surface = AssetWriter.SaveMaterial(SurfaceMaterialPath, new Material(solidLine) { mainTexture = lights });

            CellView cell = BuildCellPrefab(art);
            string prefabReport = WireGameplayPrefab(art, cell, route, solidLine, surface);
            UpdateTheme();
            UpdateSkins();
            AssetDatabase.SaveAssets();

            return "Flat board installed.\n" + prefabReport;
        }

        private sealed class FlatArt
        {
            public Sprite TileFace;
            public Sprite TileOverlay;
            public Sprite TileShadow;
            public Sprite TileFlash;
            public Sprite StartRing;
            public Sprite StartDot;
            public Sprite Spark;
            public Sprite PaperPlane;
            public Sprite HintPuff;
            public Sprite Airplane;
            public Sprite AirplaneShadow;
            public Sprite Pilot;
            public Sprite Sky;
            public Sprite[] Clouds;
        }

        private static Sprite Sprite(string name, float pixelsPerUnit)
        {
            return SpriteImport.Import(ArtFolder + "/" + name + ".png", pixelsPerUnit);
        }

        private static Texture2D ImportDashTexture(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.wrapModeU = TextureWrapMode.Repeat;
            importer.wrapModeV = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // ------------------------------------------------------------------ cell

        private static CellView BuildCellPrefab(FlatArt art)
        {
            var root = new GameObject("CellFlat");

            try
            {
                var sizeRoot = new GameObject("VisualSizeRoot").transform;
                sizeRoot.SetParent(root.transform, false);

                SpriteRenderer shadow = AddSprite(sizeRoot, "Shadow", art.TileShadow, BoardSortingOrder.CellShadow);
                SpriteRenderer face = AddSprite(sizeRoot, "Face", art.TileFace, BoardSortingOrder.Cells);
                AddSprite(sizeRoot, "Overlay", art.TileOverlay, BoardSortingOrder.CellOverlay);
                SpriteRenderer flash = AddSprite(sizeRoot, "Flash", art.TileFlash, BoardSortingOrder.ConnectFlash);
                SpriteRenderer pulse = AddSprite(sizeRoot, "Start Ring", art.StartRing, BoardSortingOrder.Marker);
                SpriteRenderer dot = AddSprite(sizeRoot, "Start Dot", art.StartDot, BoardSortingOrder.Marker);

                // The lip is the bottom tenth of the tile; nudging the markers up centres
                // them on the face the player actually sees.
                pulse.transform.localPosition = new Vector3(0f, 0.04f, 0f);
                pulse.transform.localScale = Vector3.one * 0.78f;
                dot.transform.localPosition = new Vector3(0f, 0.04f, 0f);
                shadow.color = Color.white;

                var view = root.AddComponent<CellView>();
                var so = new SerializedObject(view);
                so.FindProperty("spriteRenderer").objectReferenceValue = face;
                so.FindProperty("visualSizeRoot").objectReferenceValue = sizeRoot;
                so.FindProperty("startDotRenderer").objectReferenceValue = dot;
                so.FindProperty("startPulseRenderer").objectReferenceValue = pulse;
                so.FindProperty("connectFlash").objectReferenceValue = flash;
                so.FindProperty("useConfiguredSprite").boolValue = false;
                so.FindProperty("sourceVisualSize").floatValue = 1f;
                so.FindProperty("startPulseScale").floatValue = 1.35f;
                so.FindProperty("startPulseAlpha").floatValue = 0.7f;
                so.FindProperty("flashPeakAlpha").floatValue = 0.55f;
                so.ApplyModifiedPropertiesWithoutUndo();

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, CellPrefabPath);
                return saved.GetComponent<CellView>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static SpriteRenderer AddSprite(Transform parent, string name, Sprite sprite, int order)
        {
            Transform target = AssetWriter.FindOrCreateChild(parent, name);
            var renderer = AssetWriter.GetOrAdd<SpriteRenderer>(target.gameObject);
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        // ------------------------------------------------------------------ gameplay prefab

        private static string WireGameplayPrefab(FlatArt art, CellView cell, Material route, Material solidLine, Material surface)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(GameplayPrefabPath);

            try
            {
                var board = contents.GetComponentInChildren<BoardView>(true);
                var framer = contents.GetComponentInChildren<BoardCameraFramer>(true);
                var path = contents.GetComponentInChildren<PathView>(true);
                var airplane = contents.GetComponentInChildren<AirplaneView>(true);

                if (board == null || framer == null || path == null || airplane == null)
                {
                    return "Board, camera, path or airplane missing in " + GameplayPrefabPath + "; prefab left unchanged.";
                }

                SetObject(board, "cellPrefab", cell);
                SetUpCamera(framer);
                SetUpPath(art, path, route, solidLine, surface);
                SetUpHint(art, contents);
                SetUpAirplane(art, airplane);
                SetObject(airplane, "board", board);
                SetUpSky(art, contents.transform, framer);
                SwitchOffScenery(contents.transform);

                PrefabUtility.SaveAsPrefabAsset(contents, GameplayPrefabPath);
                return GameplayPrefabPath + ": flat tiles, runway, sprite airplane and sky wired.";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void SetUpCamera(BoardCameraFramer framer)
        {
            // Straight down: a flat board has no walls to show, and an upright grid is the
            // easiest to read at a glance.
            var so = new SerializedObject(framer);
            so.FindProperty("tilt").floatValue = 0f;
            so.FindProperty("blockDepth").floatValue = 0f;
            so.ApplyModifiedPropertiesWithoutUndo();

            var camera = framer.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = SkyColor;

            var vignette = framer.GetComponentInChildren<CameraVignette>(true);

            if (vignette != null)
            {
                vignette.gameObject.SetActive(false);
            }
        }

        private static void SetUpPath(FlatArt art, PathView path, Material route, Material solidLine, Material surface)
        {
            var so = new SerializedObject(path);
            so.FindProperty("sharedMaterial").objectReferenceValue = route;
            so.FindProperty("isDashed").boolValue = true;

            LineRenderer edge = SetUpRunwayLine(
                AssetWriter.FindOrCreateChild(path.transform, "Runway Edge"), solidLine, RunwayEdgeWidth, OutlineBrown);
            // The surface carries its own colour and edge lamps; tiling keeps the lamps evenly
            // spaced however long the runway grows.
            LineRenderer asphalt = SetUpRunwayLine(
                AssetWriter.FindOrCreateChild(path.transform, "Runway"), surface, RunwayWidth, Color.white);
            asphalt.textureMode = LineTextureMode.Tile;
            asphalt.textureScale = new Vector2(1f / RunwayLightSpacing, 1f);

            SerializedProperty underlays = so.FindProperty("underlays");
            underlays.arraySize = 2;
            underlays.GetArrayElementAtIndex(0).objectReferenceValue = edge;
            underlays.GetArrayElementAtIndex(1).objectReferenceValue = asphalt;

            SerializedProperty spark = so.FindProperty("spark");

            if (spark.objectReferenceValue is SpriteRenderer sparkRenderer)
            {
                sparkRenderer.sprite = art.Spark;
                so.FindProperty("sparkSize").floatValue = 0.55f;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// The hint is a paper plane gliding the way ahead, dropping white puffs behind it.
        /// The puffs are made here, once, and the hint reuses them on every run.
        /// </summary>
        private static void SetUpHint(FlatArt art, GameObject contents)
        {
            var ghost = contents.GetComponentInChildren<HintGhostView>(true);

            if (ghost == null)
            {
                return;
            }

            var so = new SerializedObject(ghost);

            if (so.FindProperty("marker").objectReferenceValue is SpriteRenderer guide)
            {
                guide.sprite = art.PaperPlane;
                guide.sortingOrder = BoardSortingOrder.HintGuide;
                guide.transform.localScale = Vector3.one * HintGuideScale;
            }

            // The puffs live beside the guide, not under it, so turning the guide leaves them be.
            Transform holder = ghost.transform.parent != null ? ghost.transform.parent : ghost.transform;
            Transform oldTrail = holder.Find("Hint Trail");

            if (oldTrail != null)
            {
                Object.DestroyImmediate(oldTrail.gameObject);
            }

            Transform trail = AssetWriter.FindOrCreateChild(holder, "Hint Puffs");
            SerializedProperty puffs = so.FindProperty("puffs");
            puffs.arraySize = HintPuffCount;

            for (int i = 0; i < HintPuffCount; i++)
            {
                SpriteRenderer puff = AddSprite(trail, "Puff " + i, art.HintPuff, BoardSortingOrder.HintTrail);
                puff.transform.localScale = Vector3.one * HintPuffScale;
                puff.enabled = false;
                puffs.GetArrayElementAtIndex(i).objectReferenceValue = puff;
            }

            so.FindProperty("puffSpacing").floatValue = HintPuffSpacing;
            so.FindProperty("alpha").floatValue = 1f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static LineRenderer SetUpRunwayLine(Transform target, Material material, float width, Color color)
        {
            var line = AssetWriter.GetOrAdd<LineRenderer>(target.gameObject);
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.positionCount = 0;
            line.widthMultiplier = 1f;
            line.startWidth = width;
            line.endWidth = width;
            line.startColor = color;
            line.endColor = color;
            line.numCornerVertices = RunwayRoundness;
            line.numCapVertices = RunwayRoundness;
            line.alignment = LineAlignment.TransformZ;
            line.textureMode = LineTextureMode.Stretch;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        private static void SetUpAirplane(FlatArt art, AirplaneView airplane)
        {
            Transform root = airplane.transform;

            // The modelled plane stays in the prefab, switched off, with its own rig.
            foreach (MeshRenderer mesh in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                mesh.gameObject.SetActive(false);
            }

            SpriteRenderer shadow = AddSprite(root, "Sprite Shadow", art.AirplaneShadow, BoardSortingOrder.AirplaneShadow);
            SpriteRenderer body = AddSprite(root, "Sprite Body", art.Airplane, BoardSortingOrder.Airplane);
            SpriteRenderer pilot = AddSprite(root, "Sprite Pilot", art.Pilot, BoardSortingOrder.AirplanePilot);
            shadow.color = Color.white;

            var rig = AssetWriter.GetOrAdd<SpriteAirplaneRig>(root.gameObject);
            var rigObject = new SerializedObject(rig);
            rigObject.FindProperty("bodyRenderer").objectReferenceValue = body;
            rigObject.FindProperty("shadowRenderer").objectReferenceValue = shadow;
            rigObject.FindProperty("pilotRenderer").objectReferenceValue = pilot;
            rigObject.ApplyModifiedPropertiesWithoutUndo();

            if (root.TryGetComponent(out MeshAirplaneRig meshRig))
            {
                meshRig.enabled = false;
            }

            SetObject(airplane, "rig", rig);
        }

        private static void SetUpSky(FlatArt art, Transform root, BoardCameraFramer framer)
        {
            Transform sky = AssetWriter.FindOrCreateChild(root, "Sky");
            SpriteRenderer gradient = AddSprite(sky, "Gradient", art.Sky, BoardSortingOrder.Landscape);

            Transform layer = AssetWriter.FindOrCreateChild(sky, "Clouds");
            layer.localPosition = new Vector3(0f, 0f, -0.5f);

            // Laid out for a 16 unit tall view, about 9 wide on a phone. Most clouds sit
            // above and below the board; the drift carries them across the sides.
            var layout = new[]
            {
                new Vector3(-3.2f, 6.4f, 0f),
                new Vector3(3.6f, 4.3f, 0f),
                new Vector3(-4.8f, -1.2f, 0f),
                new Vector3(4.4f, -4.6f, 0f),
                new Vector3(-1.6f, -6.8f, 0f),
            };
            var sizes = new[] { 1.1f, 0.8f, 0.7f, 1.2f, 0.9f };
            var clouds = new Transform[layout.Length];

            for (int i = 0; i < layout.Length; i++)
            {
                SpriteRenderer cloud = AddSprite(layer, "Cloud " + (i + 1), art.Clouds[i % art.Clouds.Length], BoardSortingOrder.Cloud);
                cloud.transform.localPosition = layout[i];
                cloud.transform.localScale = Vector3.one * sizes[i];
                clouds[i] = cloud.transform;
            }

            var drift = AssetWriter.GetOrAdd<CloudLayer>(layer.gameObject);
            var driftObject = new SerializedObject(drift);
            SerializedProperty array = driftObject.FindProperty("clouds");
            array.arraySize = clouds.Length;

            for (int i = 0; i < clouds.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = clouds[i];
            }

            driftObject.FindProperty("speed").floatValue = 0.25f;
            driftObject.FindProperty("halfSpan").floatValue = 7.5f;
            driftObject.ApplyModifiedPropertiesWithoutUndo();

            var backdrop = AssetWriter.GetOrAdd<SkyBackdrop>(sky.gameObject);
            var so = new SerializedObject(backdrop);
            so.FindProperty("framer").objectReferenceValue = framer;
            so.FindProperty("boardCamera").objectReferenceValue = framer.GetComponent<Camera>();
            so.FindProperty("gradient").objectReferenceValue = gradient;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SwitchOffScenery(Transform root)
        {
            Transform landscape = root.Find("Landscape");

            if (landscape != null)
            {
                landscape.gameObject.SetActive(false);
            }
        }

        // ------------------------------------------------------------------ colours

        private static void UpdateTheme()
        {
            var theme = AssetDatabase.LoadAssetAtPath<ThemeSO>(ThemePath);

            if (theme == null)
            {
                return;
            }

            var so = new SerializedObject(theme);
            so.FindProperty("background").colorValue = SkyColor;
            so.FindProperty("cell").colorValue = TileCream;
            so.FindProperty("startDot").colorValue = Color.white;
            so.FindProperty("startHalo").colorValue = Color.white;
            so.FindProperty("pathWidth").floatValue = CentreLineWidth;
            so.FindProperty("cellSpacing").floatValue = 0.1f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Every skin becomes a pastel take on the same flat board: cream tiles, a white
        /// centre line, and its own hue for the start pad and the ground the runway covers.
        /// </summary>
        private static void UpdateSkins()
        {
            ApplyFlatSkin(SkinFolder + "/SkinClassic.asset", hue: 140f, hueStep: 0f, saturation: 1f);
            ApplyFlatSkin(SkinFolder + "/SkinOcean.asset", hue: 200f, hueStep: 0f, saturation: 1.1f);
            ApplyFlatSkin(SkinFolder + "/SkinSunset.asset", hue: 8f, hueStep: 0f, saturation: 1f);
            ApplyFlatSkin(SkinFolder + "/SkinAurora.asset", hue: 265f, hueStep: 0f, saturation: 0.9f);
            ApplyFlatSkin(SkinFolder + "/SkinNeon.asset", hue: 150f, hueStep: 40f, saturation: 1.3f);
        }

        private static void ApplyFlatSkin(string path, float hue, float hueStep, float saturation)
        {
            var skin = AssetDatabase.LoadAssetAtPath<SkinSO>(path);

            if (skin == null)
            {
                return;
            }

            var so = new SerializedObject(skin);
            SerializedProperty palette = so.FindProperty("palette");
            palette.FindPropertyRelative("background").colorValue = SkyColor;
            palette.FindPropertyRelative("cell").colorValue = TileCream;
            palette.FindPropertyRelative("usesLevelHue").boolValue = hueStep > 0f;
            palette.FindPropertyRelative("firstLevelHue").floatValue = hue;
            palette.FindPropertyRelative("hueStepPerLevel").floatValue = Mathf.Max(1f, hueStep);
            palette.FindPropertyRelative("startTone").vector2Value = new Vector2(0.48f * saturation, 0.8f);
            // Squares the runway covers only warm a touch: the runway itself shows the way.
            palette.FindPropertyRelative("visitedTone").vector2Value = new Vector2(0.05f * saturation, 0.98f);
            palette.FindPropertyRelative("headTone").vector2Value = new Vector2(0.07f * saturation, 0.99f);
            palette.FindPropertyRelative("pathTone").vector2Value = new Vector2(0f, 1f);
            palette.FindPropertyRelative("wonTone").vector2Value = new Vector2(0.22f * saturation, 1f);
            palette.FindPropertyRelative("pathAlpha").floatValue = 1f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObject(Object target, string property, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
