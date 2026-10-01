using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Builds the Home dioramas in the flat 2D style, as a paper theatre: ground pieces lie
    /// on the floor and upright pieces stand on it, leaned back to face the camera so they
    /// read front-on. Upright pieces share one sorting order and are sorted by distance
    /// from their feet, so cats walk in front of and behind furniture correctly.
    ///
    /// Writes to the same prefab paths as <see cref="HomeDioramaBuilder"/>, so the Home
    /// scene builder places them unchanged. Art comes from Tools/flat_art/generate_home.py.
    /// </summary>
    public static class FlatDioramaBuilder
    {
        private const string ArtFolder = "Assets/_Game/Art/FlatHome";
        private const string BoardArtFolder = "Assets/_Game/Art/Flat";
        private const float PixelsPerUnit = 128f;

        // Camera pitch of the Home views (see HomeSceneBuilder's anchors). Upright pieces
        // lean back by this much so they face the lens.
        public const float RoomPitch = 30f;

        // The airport is framed as a picture with depth, like a real one seen from the
        // apron: in front, our plane waits at stand A1 with its stairs down, the passengers
        // queueing at the gate desk on the left and the baggage on its way on the right;
        // behind it the terminal, another airline at the next stand, then the runway, the
        // tower and the city.
        public const float AirportPitch = 30f;
        public const float AirportDistance = 26f;
        public static readonly Vector3 AirportLookAt = new Vector3(0f, 0f, 2.2f);

        public static Vector3 AirportCameraPosition =>
            AirportLookAt - Quaternion.Euler(AirportPitch, 0f, 0f) * Vector3.forward * AirportDistance;

        // Parked at stand A1, side-on with its nose to the left: it stands on its wheels like
        // the passengers stand on their feet, so it reads at their scale instead of lying flat.
        public static readonly Vector3 PlaneSpot = new Vector3(0.7f, 0f, -0.9f);
        public const float PlaneYaw = 0f;

        // About three cats tall: the heart of the picture.
        private const float PlaneScale = 1.15f;

        // Where waiting passengers mill about: the apron in front of the gate desk.
        public static readonly Vector3 QueueCentre = new Vector3(-2.5f, 0.05f, -2.2f);
        public static readonly Vector2 QueueSize = new Vector2(2.6f, 1.2f);

        // Clouds in the sky above the skyline, drifting behind the title.
        public static readonly Vector3[] CloudSpots =
        {
            new Vector3(-4.2f, 3.2f, 18f), new Vector3(4.6f, 4.0f, 18.2f), new Vector3(0.8f, 5.0f, 18.4f),
        };

        private const float RunwayZ = 9.0f;

        private const int GroundOrder = -30;
        private const int WallOrder = -29;
        private const int RugOrder = -28;
        private const int ShadowOrder = -27;
        private const int StandingOrder = 0;

        [MenuItem("Tools/AirLine Pop/Build Flat Home Dioramas (2D)")]
        public static void BuildFromMenu()
        {
            Debug.Log(Build());
        }

        public static string Build()
        {
            BuildRoom();
            BuildAirport();
            BuildPlane();
            BuildCloud();
            AssetDatabase.SaveAssets();
            return "Flat dioramas written over " + HomeDioramaBuilder.RoomPrefabPath + " and the airport, plane and cloud prefabs.";
        }

        // ------------------------------------------------------------------ room

        // The departure lounge where regular passengers wait. It borrows the gameplay
        // board's look: cream tiles underfoot and the board's sky through the window.
        private static readonly Vector3 SeatsSpot = new Vector3(0.6f, 0f, 3.7f);
        private static readonly Vector3 GateDeskSpot = new Vector3(-2.2f, 0f, 2.4f);
        private static readonly Vector3 TrolleySpot = new Vector3(2.6f, 0f, 1.2f);
        private static readonly Vector3 BowlSpot = new Vector3(-2.0f, 0f, -2.6f);
        private static readonly Vector3 YarnSpot = new Vector3(1.5f, 0f, -2.4f);

        /// <summary>Floor the lounge furniture stands on, in the lounge's local x/z, for the cats' walk area.</summary>
        public static Rect[] LoungeBlocked()
        {
            return new[]
            {
                new Rect(SeatsSpot.x - 1.65f, SeatsSpot.z - 0.55f, 3.3f, 1.1f),
                new Rect(TrolleySpot.x - 0.55f, TrolleySpot.z - 0.4f, 1.1f, 0.8f),
                new Rect(BowlSpot.x - 0.35f, BowlSpot.z - 0.35f, 0.7f, 0.7f),
                new Rect(GateDeskSpot.x - 0.5f, GateDeskSpot.z - 0.35f, 1.0f, 0.7f),
            };
        }

        private static void BuildRoom()
        {
            var root = new GameObject("CatRoom");

            try
            {
                float halfDepth = HomeDioramaBuilder.RoomDepth * 0.5f;

                // The floor runs on past the front edge so it fills the screen to the bottom bar.
                Flat(root.transform, "Floor", Art("lounge_floor", Center), new Vector3(0f, 0f, -1.25f), GroundOrder);
                Signed(Upright(root.transform, "Window Wall", Art("lounge_wall", Foot), new Vector3(0f, 0f, halfDepth), 0f, WallOrder), "lounge_wall");
                AddDeparturesBoard(root.transform, halfDepth);
                Flat(root.transform, "Rug", Art("lounge_rug", Center), new Vector3(0f, 0.01f, -0.3f), RugOrder);

                Prop(root.transform, "Seats", "lounge_seats", SeatsSpot, 3.2f);
                Signed(Prop(root.transform, "Gate Desk", "gate_podium", GateDeskSpot, 1.0f).GetComponent<SpriteRenderer>(), "gate_podium");
                Prop(root.transform, "Trolley", "trolley", TrolleySpot, 1.2f);
                Prop(root.transform, "Bowl", "bowl", BowlSpot, 0.8f);
                Transform yarn = Prop(root.transform, "Yarn", "yarn", YarnSpot, 0.6f);

                AddCatSpots(root.transform, yarn);

                PrefabUtility.SaveAsPrefabAsset(root, HomeDioramaBuilder.RoomPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>
        /// Live text over the departures panel painted on the wall. The panel's three row
        /// stripes sit 102, 136 and 170 px down a 512 px (4 unit) wall, between 5.0 and
        /// 8.6 units across it; see lounge_wall() in generate_home.py.
        /// </summary>
        private static void AddDeparturesBoard(Transform room, float wallZ)
        {
            var board = new GameObject("Departures");
            board.transform.SetParent(room, false);
            board.transform.localPosition = new Vector3(1.8f, 4f - 136f / 128f, wallZ - 0.02f);

            TextMeshPro text = board.AddComponent<TextMeshPro>();
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitInstaller.TitleFontPath);
            text.rectTransform.sizeDelta = new Vector2(3.3f, 0.8f);
            text.enableAutoSizing = true;
            text.fontSizeMin = 0.5f;
            text.fontSizeMax = 2.4f;
            text.alignment = TextAlignmentOptions.Left;
            text.color = new Color32(255, 250, 241, 255);
            text.text = "AP 001<pos=30%>DA LAT<pos=64%>BOARDING\nAP 002<pos=30%>DA LAT<pos=64%>ON TIME\nAP 003<pos=30%>DA LAT<pos=64%>ON TIME";
            text.GetComponent<MeshRenderer>().sortingOrder = WallOrder + 1;

            board.AddComponent<DeparturesBoardView>().EditorLink(text);
        }

        /// <summary>
        /// The places cats go to on purpose: roll about with the yarn, wait by the gate desk,
        /// the trolley and the ends of the seats. Each stands just clear of the
        /// furniture's blocked floor so a cat can actually reach it.
        /// </summary>
        private static void AddCatSpots(Transform room, Transform yarn)
        {
            YarnToyView toy = yarn.gameObject.AddComponent<YarnToyView>();
            toy.EditorLink(yarn);

            Spot(room, "Spot Yarn", YarnSpot, CatSpotKind.Toy, 0.62f, 0, toy);
            Spot(room, "Spot Gate Desk", GateDeskSpot + new Vector3(0f, 0f, -0.5f), CatSpotKind.Furniture, 0.7f, 1, null);
            Spot(room, "Spot Trolley", TrolleySpot, CatSpotKind.Furniture, 0.82f, -1, null);
            Spot(room, "Spot Seats Left", SeatsSpot + new Vector3(-1.65f, 0f, -0.2f), CatSpotKind.Furniture, 0.42f, -1, null);
            Spot(room, "Spot Seats Right", SeatsSpot + new Vector3(1.65f, 0f, -0.2f), CatSpotKind.Furniture, 0.42f, 1, null);
        }

        private static void Spot(Transform room, string name, Vector3 position, CatSpotKind kind, float standOff, int side, YarnToyView toy)
        {
            var spot = new GameObject(name);
            spot.transform.SetParent(room, false);
            spot.transform.localPosition = position;
            spot.AddComponent<CatSpot>().EditorConfigure(kind, standOff, side, toy);
        }

        /// <summary>Art with words painted on it swaps to its Vietnamese drawing ("_vi") when the language does.</summary>
        private static void Signed(SpriteRenderer renderer, string art)
        {
            renderer.gameObject.AddComponent<LocalizedSprite>()
                .EditorLink(renderer, renderer.sprite, Art(art + "_vi", renderer.sprite.pivot / renderer.sprite.rect.size));
        }

        private static Transform Prop(Transform parent, string name, string art, Vector3 foot, float shadowWidth)
        {
            Transform prop = Upright(parent, name, Art(art, Foot), foot, RoomPitch, StandingOrder).transform;
            Shadow(parent, name + " Shadow", foot, shadowWidth);
            prop.SetAsLastSibling();
            return prop;
        }

        // ------------------------------------------------------------------ airport

        private static void BuildAirport()
        {
            var root = new GameObject("HomeAirport");

            try
            {
                // The lounge's palette outside too: the board's sky behind, pale concrete all
                // the way to the horizon, and green only in thin strips beside the runway.
                SpriteRenderer sky = Upright(root.transform, "Sky", Art("airport_sky", Foot), new Vector3(0f, -2f, 19f), 0f, GroundOrder - 1);
                sky.transform.localScale = new Vector3(2000f, 16f, 1f);

                SpriteRenderer ground = Flat(root.transform, "Ground", Art("apron", Center), new Vector3(0f, 0f, 8f), GroundOrder);
                ground.drawMode = SpriteDrawMode.Tiled;
                ground.size = new Vector2(70f, 40f);
                // Stand A1's paint: the stop bar sits under the plane's nose wheel.
                Flat(root.transform, "Stand A1", Art("apron_marks", Center), new Vector3(PlaneSpot.x + 0.6f, 0.008f, PlaneSpot.z + 0.1f), GroundOrder + 2);

                // A pale city on the horizon closes off the ground under the sky. Kept low so a
                // band of sky stays open behind the Home title.
                Upright(root.transform, "Skyline", Art("skyline", Foot), new Vector3(0f, 0f, 17f), 0f, WallOrder)
                    .transform.localScale = new Vector3(1.6f, 0.8f, 1f);

                // The runway crosses the view left to right, beyond the terminal.
                SpriteRenderer runway = Flat(root.transform, "Runway", Art("runway_home", Center), new Vector3(0f, 0.01f, RunwayZ), RugOrder);
                runway.transform.localRotation = Quaternion.Euler(0f, 90f, 0f) * Quaternion.Euler(90f, 0f, 0f);
                runway.transform.localScale = new Vector3(1f, 2f, 1f);

                foreach (float side in new[] { -1f, 1f })
                {
                    SpriteRenderer verge = Flat(root.transform, "Verge", Art("grass", Center), new Vector3(0f, 0.006f, RunwayZ + side * 1.55f), GroundOrder + 2);
                    verge.drawMode = SpriteDrawMode.Tiled;
                    verge.size = new Vector2(30f, 0.45f);
                }

                // Right behind the apron: the terminal with the airline's name, and another
                // airline's plane at the next stand. Beyond the runway: tower and hangar.
                Standing(root.transform, "Terminal", "terminal", new Vector3(-1.2f, 0f, 5.0f), 6.4f);
                Standing(root.transform, "Next Stand", "plane_mint", new Vector3(4.9f, 0f, 3.2f), 3.4f)
                    .transform.localScale = new Vector3(0.8f, 0.8f, 1f);
                Standing(root.transform, "Tower", "tower", new Vector3(4.6f, 0f, 11.0f), 1.3f)
                    .transform.localScale = new Vector3(0.85f, 0.85f, 1f);
                Standing(root.transform, "Hangar", "hangar", new Vector3(-6.0f, 0f, 11.4f), 3.0f);
                Standing(root.transform, "Windsock", "windsock", new Vector3(6.2f, 0f, 8.0f), 0.5f);
                Standing(root.transform, "Tree 1", "tree_round", new Vector3(-5.6f, 0f, 6.2f), 1.1f);
                Standing(root.transform, "Tree 2", "tree_round", new Vector3(2.2f, 0f, 6.4f), 1.1f);
                Standing(root.transform, "Light 1", "light_pole", new Vector3(-4.4f, 0f, 1.6f), 0.5f);
                Standing(root.transform, "Light 2", "light_pole", new Vector3(3.0f, 0f, 2.4f), 0.5f);
                Standing(root.transform, "Taxi Sign", "taxi_sign", new Vector3(-3.6f, 0f, 3.0f), 1.2f);

                // On the apron: stairs at the plane's door, cones by the wing and tail, the
                // gate desk and queue line for the passengers, and the baggage train.
                // The stairs' platform meets the plane's front door.
                Standing(root.transform, "Airstairs", "airstairs", PlaneSpot + new Vector3(-0.3f, 0f, -0.5f), 1.6f);
                Standing(root.transform, "Cone 1", "cone", PlaneSpot + new Vector3(-3.1f, 0f, 0.3f), 0.4f);
                Standing(root.transform, "Cone 2", "cone", PlaneSpot + new Vector3(3.2f, 0f, -0.6f), 0.4f);
                Signed(Standing(root.transform, "Gate Desk", "gate_podium", new Vector3(-3.3f, 0f, -0.9f), 1.0f), "gate_podium");
                Standing(root.transform, "Queue Line", "queue_rope", new Vector3(-2.0f, 0f, -1.3f), 2.4f);
                Standing(root.transform, "Baggage", "baggage_train", new Vector3(3.2f, 0f, -2.8f), 3.0f);

                PrefabUtility.SaveAsPrefabAsset(root, HomeDioramaBuilder.AirportPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static SpriteRenderer Standing(Transform parent, string name, string art, Vector3 foot, float shadowWidth)
        {
            SpriteRenderer renderer = Upright(parent, name, Art(art, Foot), foot, AirportPitch, StandingOrder);
            Shadow(parent, name + " Shadow", foot, shadowWidth);
            return renderer;
        }

        private static void BuildPlane()
        {
            // Our plane at the stand, standing side-on like everything else on the apron.
            var root = new GameObject("FlatPlane");

            try
            {
                Shadow(root.transform, "Shadow", Vector3.zero, 4.6f);
                SpriteRenderer body = Upright(root.transform, "Body", Art("plane_parked", Foot), Vector3.zero, AirportPitch, StandingOrder);
                body.transform.localScale = new Vector3(PlaneScale, PlaneScale, 1f);
                PrefabUtility.SaveAsPrefabAsset(root, HomeDioramaBuilder.PlanePrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void BuildCloud()
        {
            var root = new GameObject("FlatCloud");

            try
            {
                Sprite cloud = SpriteImport.Import(BoardArtFolder + "/cloud_0.png", 100f);
                SpriteRenderer renderer = Upright(root.transform, "Cloud", cloud, Vector3.zero, AirportPitch, StandingOrder);
                renderer.transform.localScale = Vector3.one * 0.8f;
                PrefabUtility.SaveAsPrefabAsset(root, HomeDioramaBuilder.CloudPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ------------------------------------------------------------------ helpers

        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 Foot = new Vector2(0.5f, 0f);

        private static Sprite Art(string name, Vector2 pivot)
        {
            string path = ArtFolder + "/" + name + ".png";
            SpriteImport.Import(path, PixelsPerUnit);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);

            if (name == "grass")
            {
                importer.wrapMode = TextureWrapMode.Repeat;
            }

            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>A piece lying on the ground, its top edge pointing away from the camera.</summary>
        private static SpriteRenderer Flat(Transform parent, string name, Sprite sprite, Vector3 position, int order)
        {
            SpriteRenderer renderer = Renderer(parent, name, sprite, order);
            renderer.transform.localPosition = position;
            renderer.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            return renderer;
        }

        /// <summary>A piece standing on its feet, leaned back by <paramref name="pitch"/> to face the camera.</summary>
        private static SpriteRenderer Upright(Transform parent, string name, Sprite sprite, Vector3 foot, float pitch, int order)
        {
            SpriteRenderer renderer = Renderer(parent, name, sprite, order);
            renderer.transform.localPosition = foot;
            renderer.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            renderer.spriteSortPoint = SpriteSortPoint.Pivot;
            return renderer;
        }

        private static void Shadow(Transform parent, string name, Vector3 foot, float width)
        {
            Sprite sprite = SpriteImport.Import(ArtFolder + "/prop_shadow.png", 256f);
            SpriteRenderer renderer = Flat(parent, name, sprite, foot + new Vector3(0f, 0.005f, 0.05f), ShadowOrder);
            renderer.transform.localScale = new Vector3(width, width, 1f);
        }

        private static SpriteRenderer Renderer(Transform parent, string name, Sprite sprite, int order)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }
    }
}
