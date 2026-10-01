using UnityEditor;
using UnityEngine;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Builds the Home airport (GDD 9: "3D airport in the middle") and the cat room in the
    /// same shape language as the CubeAnimals cats: everything is a soft rounded block, a
    /// big main mass carrying small layered details (trims, frames, lips) the way a cat's
    /// body carries its muzzle, ears and paws. No spheres or cones, which read as toys.
    ///
    /// The plane and clouds are separate prefabs so they can move. All static scenery is
    /// one mesh on the shared world material.
    /// </summary>
    public static class HomeDioramaBuilder
    {
        private const string Folder = "Assets/_Game/Art/World";
        public const string AirportPrefabPath = Folder + "/HomeAirport.prefab";
        public const string PlanePrefabPath = Folder + "/CubePlane.prefab";
        public const string CloudPrefabPath = Folder + "/CubeCloud.prefab";
        public const string RoomPrefabPath = Folder + "/CatRoom.prefab";

        // Island footprint; the runway runs along Z on the right, the terminal sits at the back left.
        private const float IslandWidth = 9f;
        private const float IslandDepth = 12f;
        private const float RunwayX = 2.4f;

        // Corner rounding as a share of a block's smallest side, matched to the cats' bodies.
        private const float Softness = 0.22f;

        [MenuItem("Tools/AirLine Pop/Build Home Airport")]
        public static void BuildFromMenu()
        {
            EditorUtility.DisplayDialog("AirLine Pop", Build(), "OK");
        }

        public static string Build()
        {
            AssetWriter.EnsureFolder(Folder);
            Material material = WorldMaterial.Ensure();

            SavePrefab(AirportPrefabPath, "HomeAirport", SaveMesh("HomeAirport", BuildAirport()), material);
            SavePrefab(PlanePrefabPath, "CubePlane", SaveMesh("CubePlane", BuildPlane()), material);
            SavePrefab(CloudPrefabPath, "CubeCloud", SaveMesh("CubeCloud", BuildCloud()), material);
            SavePrefab(RoomPrefabPath, "CatRoom", SaveMesh("CatRoom", BuildRoom()), material);
            AssetDatabase.SaveAssets();
            return "Home airport, room, plane and cloud built in " + Folder + ".";
        }

        private static Mesh SaveMesh(string name, PaletteMeshBuilder builder)
        {
            return AssetWriter.SaveMesh(Folder + "/" + name + ".asset", builder.Build(name));
        }

        private static void SavePrefab(string path, string name, Mesh mesh, Material material)
        {
            var root = new GameObject(name);
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            root.AddComponent<MeshRenderer>().sharedMaterial = material;
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        /// <summary>A soft block standing on <paramref name="foot"/> (its bottom centre).</summary>
        private static void Block(PaletteMeshBuilder b, Vector3 foot, Vector3 size, PaletteCell cell, float yaw = 0f)
        {
            float radius = Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * Softness;
            b.AddRoundedBox(foot + Vector3.up * (size.y * 0.5f), size, radius, cell, yaw, 5);
        }

        /// <summary>A thin flat piece lying on a surface, such as a stripe or a rug border.</summary>
        private static void Plate(PaletteMeshBuilder b, Vector3 foot, Vector2 size, float thickness, PaletteCell cell)
        {
            b.AddRoundedBox(foot + Vector3.up * (thickness * 0.5f), new Vector3(size.x, thickness, size.y), thickness * 0.45f, cell, 2);
        }

        // ---------------------------------------------------------------- airport

        private static PaletteMeshBuilder BuildAirport()
        {
            var b = new PaletteMeshBuilder();
            AddIsland(b);
            AddRunway(b);
            AddTerminal(b, new Vector3(-1.9f, 0.05f, 3.2f));
            AddTower(b, new Vector3(-3.3f, 0.05f, 1.2f));
            AddTrees(b);
            AddGroundDetails(b);
            return b;
        }

        private static void AddIsland(PaletteMeshBuilder b)
        {
            // Earth in two strata under a grass slab with a darker lip, like a cut cake.
            Block(b, new Vector3(0f, -2.0f, 0f), new Vector3(IslandWidth - 1.2f, 0.9f, IslandDepth - 1.2f), WorldPalette.Wood);
            Block(b, new Vector3(0f, -1.25f, 0f), new Vector3(IslandWidth - 0.5f, 1.0f, IslandDepth - 0.5f), WorldPalette.WoodLight);
            Block(b, new Vector3(0f, -0.45f, 0f), new Vector3(IslandWidth + 0.1f, 0.3f, IslandDepth + 0.1f), WorldPalette.Olive);
            Block(b, new Vector3(0f, -0.3f, 0f), new Vector3(IslandWidth, 0.35f, IslandDepth), WorldPalette.OliveLight);

            // Stones set into the earth face.
            Block(b, new Vector3(-2.8f, -1.1f, -IslandDepth * 0.5f + 0.1f), new Vector3(0.5f, 0.32f, 0.3f), WorldPalette.Stone);
            Block(b, new Vector3(1.2f, -1.6f, -IslandDepth * 0.5f + 0.5f), new Vector3(0.36f, 0.26f, 0.3f), WorldPalette.StoneDark);
            Block(b, new Vector3(3.4f, -1.0f, -IslandDepth * 0.5f + 0.1f), new Vector3(0.42f, 0.28f, 0.3f), WorldPalette.Stone);

            // A darker lawn and a paved path from the terminal door.
            Plate(b, new Vector3(-2.2f, 0.05f, -2.4f), new Vector2(3.2f, 3.6f), 0.05f, WorldPalette.Olive);
            Plate(b, new Vector3(-0.6f, 0.05f, 1.0f), new Vector2(1.0f, 4.4f), 0.06f, WorldPalette.RugCream);

            for (float z = -0.9f; z <= 2.9f; z += 0.55f)
            {
                Plate(b, new Vector3(-0.6f, 0.11f, z), new Vector2(0.8f, 0.4f), 0.03f, WorldPalette.CreamDark);
            }
        }

        private static void AddRunway(PaletteMeshBuilder b)
        {
            Block(b, new Vector3(RunwayX, 0.05f, -0.2f), new Vector3(2.5f, 0.12f, 10.6f), WorldPalette.RunwayDark);
            Block(b, new Vector3(RunwayX, 0.06f, -0.2f), new Vector3(2.3f, 0.13f, 10.4f), WorldPalette.Runway);

            // Edge lines and centre dashes.
            Plate(b, new Vector3(RunwayX - 1.0f, 0.19f, -0.2f), new Vector2(0.08f, 10.0f), 0.02f, WorldPalette.WarmWhite);
            Plate(b, new Vector3(RunwayX + 1.0f, 0.19f, -0.2f), new Vector2(0.08f, 10.0f), 0.02f, WorldPalette.WarmWhite);

            for (float z = -4.2f; z <= 4.4f; z += 1.2f)
            {
                Plate(b, new Vector3(RunwayX, 0.19f, z), new Vector2(0.14f, 0.6f), 0.02f, WorldPalette.WarmWhite);
            }

            for (int i = -2; i <= 2; i++)
            {
                Plate(b, new Vector3(RunwayX + i * 0.34f, 0.19f, -5.0f), new Vector2(0.18f, 0.5f), 0.02f, WorldPalette.WarmWhite);
            }

            // Edge lamps: a dark post with a lit cap.
            for (float z = -4.8f; z <= 4.6f; z += 1.2f)
            {
                AddLamp(b, new Vector3(RunwayX - 1.45f, 0.05f, z));
                AddLamp(b, new Vector3(RunwayX + 1.45f, 0.05f, z));
            }
        }

        private static void AddLamp(PaletteMeshBuilder b, Vector3 foot)
        {
            Block(b, foot, new Vector3(0.12f, 0.12f, 0.12f), WorldPalette.StoneDark);
            Block(b, foot + Vector3.up * 0.1f, new Vector3(0.1f, 0.08f, 0.1f), WorldPalette.LampYellow);
        }

        private static void AddTerminal(PaletteMeshBuilder b, Vector3 at)
        {
            // Plinth, walls, a two-step roof with overhang.
            Block(b, at, new Vector3(4.5f, 0.18f, 2.3f), WorldPalette.CreamDark);
            Block(b, at + new Vector3(0f, 0.15f, 0f), new Vector3(4.2f, 1.5f, 2.0f), WorldPalette.Cream);
            Block(b, at + new Vector3(0f, 1.6f, 0f), new Vector3(4.6f, 0.2f, 2.4f), WorldPalette.OliveDark);
            Block(b, at + new Vector3(0f, 1.75f, 0.1f), new Vector3(4.2f, 0.28f, 2.0f), WorldPalette.Olive);

            // Framed windows either side of the door.
            float[] windows = { -1.55f, -0.65f, 1.35f };

            for (int i = 0; i < windows.Length; i++)
            {
                Vector3 w = at + new Vector3(windows[i], 0.6f, -1.02f);
                Block(b, w, new Vector3(0.72f, 0.62f, 0.1f), WorldPalette.WoodLight);
                Block(b, w + new Vector3(0f, 0.07f, -0.03f), new Vector3(0.56f, 0.48f, 0.08f), WorldPalette.Glass);
                Block(b, w + new Vector3(0f, -0.06f, -0.08f), new Vector3(0.8f, 0.1f, 0.18f), WorldPalette.WoodLight);
            }

            // Door with frame and a striped awning.
            Vector3 door = at + new Vector3(0.35f, 0.15f, -1.02f);
            Block(b, door, new Vector3(0.78f, 1.08f, 0.1f), WorldPalette.WoodLight);
            Block(b, door + new Vector3(0f, 0.04f, -0.03f), new Vector3(0.6f, 0.96f, 0.08f), WorldPalette.Wood);
            Block(b, door + new Vector3(0.18f, 0.45f, -0.08f), new Vector3(0.08f, 0.08f, 0.06f), WorldPalette.LampYellow);
            Block(b, door + new Vector3(0f, 1.12f, -0.2f), new Vector3(1.1f, 0.14f, 0.5f), WorldPalette.EarthOrange);
            Block(b, door + new Vector3(0f, 1.1f, -0.42f), new Vector3(1.12f, 0.1f, 0.1f), WorldPalette.Terracotta);

            // Roof sign on two posts, and a vent box.
            Block(b, at + new Vector3(-0.5f, 1.95f, -0.45f), new Vector3(0.1f, 0.3f, 0.1f), WorldPalette.WoodDark);
            Block(b, at + new Vector3(0.9f, 1.95f, -0.45f), new Vector3(0.1f, 0.3f, 0.1f), WorldPalette.WoodDark);
            Block(b, at + new Vector3(0.2f, 2.2f, -0.45f), new Vector3(2.0f, 0.5f, 0.16f), WorldPalette.EarthOrange);
            Block(b, at + new Vector3(0.2f, 2.26f, -0.54f), new Vector3(1.76f, 0.38f, 0.06f), WorldPalette.WarmWhite);
            Block(b, at + new Vector3(-1.5f, 2.0f, 0.4f), new Vector3(0.6f, 0.36f, 0.5f), WorldPalette.Stone);
        }

        private static void AddTower(PaletteMeshBuilder b, Vector3 at)
        {
            Block(b, at, new Vector3(1.0f, 0.2f, 1.0f), WorldPalette.CreamDark);
            Block(b, at + new Vector3(0f, 0.15f, 0f), new Vector3(0.78f, 2.45f, 0.78f), WorldPalette.Cream);
            Block(b, at + new Vector3(0f, 1.1f, 0f), new Vector3(0.84f, 0.14f, 0.84f), WorldPalette.EarthOrange);
            Block(b, at + new Vector3(0f, 2.55f, 0f), new Vector3(1.3f, 0.16f, 1.3f), WorldPalette.CreamDark);
            Block(b, at + new Vector3(0f, 2.68f, 0f), new Vector3(1.12f, 0.58f, 1.12f), WorldPalette.Glass);
            Block(b, at + new Vector3(0f, 3.24f, 0f), new Vector3(1.36f, 0.2f, 1.36f), WorldPalette.RoofRed);
            Block(b, at + new Vector3(0f, 3.42f, 0f), new Vector3(0.9f, 0.16f, 0.9f), WorldPalette.Terracotta);
            Block(b, at + new Vector3(0.25f, 3.55f, 0.2f), new Vector3(0.06f, 0.5f, 0.06f), WorldPalette.WoodDark);
            Block(b, at + new Vector3(0.25f, 4.02f, 0.2f), new Vector3(0.14f, 0.14f, 0.14f), WorldPalette.RoofRed);
        }

        private static void AddTrees(PaletteMeshBuilder b)
        {
            AddPine(b, new Vector3(-3.9f, 0.05f, 5.1f), 1.1f, 10f);
            AddPine(b, new Vector3(-0.2f, 0.05f, 5.3f), 0.9f, -15f);
            AddPine(b, new Vector3(3.9f, 0.05f, 5.1f), 1.2f, 30f);
            AddPine(b, new Vector3(-4.0f, 0.05f, -1.4f), 1.0f, 5f);
            AddPine(b, new Vector3(-3.8f, 0.05f, -5.1f), 0.85f, -25f);
            AddLeafTree(b, new Vector3(0.6f, 0.05f, 4.6f), 0.9f, 20f);
            AddLeafTree(b, new Vector3(-4.0f, 0.05f, 3.2f), 0.8f, -10f);
            AddLeafTree(b, new Vector3(-0.3f, 0.05f, -5.2f), 0.75f, 35f);
            AddBush(b, new Vector3(-2.9f, 0.05f, -0.2f), 15f);
            AddBush(b, new Vector3(-1.4f, 0.05f, -4.6f), -20f);
            AddBush(b, new Vector3(0.3f, 0.05f, 2.2f), 40f);
        }

        /// <summary>Stacked blocks, each narrower and turned, in two alternating greens.</summary>
        private static void AddPine(PaletteMeshBuilder b, Vector3 at, float s, float yaw)
        {
            Block(b, at, new Vector3(0.26f, 0.5f, 0.26f) * s, WorldPalette.Trunk, yaw);
            Block(b, at + Vector3.up * 0.4f * s, new Vector3(1.2f, 0.55f, 1.2f) * s, WorldPalette.PineDark, yaw);
            Block(b, at + Vector3.up * 0.9f * s, new Vector3(0.92f, 0.5f, 0.92f) * s, WorldPalette.Pine, yaw + 45f);
            Block(b, at + Vector3.up * 1.35f * s, new Vector3(0.64f, 0.45f, 0.64f) * s, WorldPalette.PineDark, yaw);
            Block(b, at + Vector3.up * 1.75f * s, new Vector3(0.36f, 0.36f, 0.36f) * s, WorldPalette.Pine, yaw + 45f);
        }

        /// <summary>A trunk under one big canopy block with two smaller blocks tucked into it.</summary>
        private static void AddLeafTree(PaletteMeshBuilder b, Vector3 at, float s, float yaw)
        {
            Block(b, at, new Vector3(0.24f, 0.8f, 0.24f) * s, WorldPalette.Trunk, yaw);
            Block(b, at + new Vector3(0f, 0.7f, 0f) * s, new Vector3(1.15f, 0.9f, 1.15f) * s, WorldPalette.Leaf, yaw);
            Block(b, at + new Vector3(0.38f, 0.62f, -0.3f) * s, new Vector3(0.6f, 0.55f, 0.6f) * s, WorldPalette.LeafLight, yaw + 20f);
            Block(b, at + new Vector3(-0.3f, 1.35f, 0.1f) * s, new Vector3(0.62f, 0.5f, 0.62f) * s, WorldPalette.LeafLight, yaw - 15f);
        }

        private static void AddBush(PaletteMeshBuilder b, Vector3 at, float yaw)
        {
            Block(b, at, new Vector3(0.62f, 0.42f, 0.52f), WorldPalette.Leaf, yaw);
            Block(b, at + new Vector3(0.3f, 0f, 0.12f), new Vector3(0.4f, 0.32f, 0.38f), WorldPalette.LeafLight, yaw + 25f);
        }

        /// <summary>Grass tufts, flowers on stems and a few stones, all small blocks.</summary>
        private static void AddGroundDetails(PaletteMeshBuilder b)
        {
            Vector3[] flowers =
            {
                new Vector3(-2.6f, 0.1f, -3.2f), new Vector3(-2.0f, 0.1f, -2.4f), new Vector3(-1.5f, 0.1f, -3.4f),
                new Vector3(0.2f, 0.05f, -3.9f), new Vector3(-3.4f, 0.05f, 0.6f), new Vector3(0.1f, 0.05f, 3.5f),
            };

            for (int i = 0; i < flowers.Length; i++)
            {
                PaletteCell petal = i % 2 == 0 ? WorldPalette.Rose : WorldPalette.LampYellow;
                Block(b, flowers[i], new Vector3(0.04f, 0.16f, 0.04f), WorldPalette.Pine);
                Block(b, flowers[i] + Vector3.up * 0.14f, new Vector3(0.14f, 0.1f, 0.14f), petal, i * 20f);
            }

            Vector3[] tufts =
            {
                new Vector3(-3.0f, 0.05f, -4.0f), new Vector3(-0.9f, 0.05f, -2.0f), new Vector3(0.6f, 0.05f, -1.0f),
                new Vector3(-3.5f, 0.05f, 2.2f), new Vector3(0.7f, 0.05f, 1.2f),
            };

            for (int i = 0; i < tufts.Length; i++)
            {
                Block(b, tufts[i], new Vector3(0.16f, 0.14f, 0.12f), WorldPalette.Olive, i * 30f);
                Block(b, tufts[i] + new Vector3(0.1f, 0f, 0.05f), new Vector3(0.1f, 0.2f, 0.1f), WorldPalette.OliveDark, i * 30f + 20f);
            }

            Block(b, new Vector3(-1.1f, 0.05f, -5.4f), new Vector3(0.4f, 0.22f, 0.32f), WorldPalette.Stone, 20f);
            Block(b, new Vector3(0.7f, 0.05f, -3.1f), new Vector3(0.26f, 0.16f, 0.22f), WorldPalette.StoneDark, -30f);
        }

        // ---------------------------------------------------------------- cat room

        // Layout local to the room origin (floor centre). Walls stand at the back (+Z) and
        // left (-X) only, so the diagonal camera looks straight in (GDD 4).
        public const float RoomWidth = 7f;
        public const float RoomDepth = 9f;
        public static readonly Vector3 SofaPosition = new Vector3(0.7f, 0f, 3.75f);
        public static readonly Vector3 BedPosition = new Vector3(-2.3f, 0f, 3.3f);
        public static readonly Vector3 BowlPosition = new Vector3(-2.0f, 0f, -2.6f);
        public static readonly Vector3 YarnPosition = new Vector3(1.5f, 0f, -2.4f);
        public static readonly Vector3 PostPosition = new Vector3(2.4f, 0f, 1.0f);
        public static readonly Vector3 PlantPosition = new Vector3(-3.0f, 0f, 4.0f);

        /// <summary>Furniture footprints the walk area keeps cats out of.</summary>
        public static Rect[] RoomBlocked()
        {
            return new[]
            {
                new Rect(SofaPosition.x - 1.5f, SofaPosition.z - 0.65f, 3.0f, 1.3f),
                new Rect(BowlPosition.x - 0.35f, BowlPosition.z - 0.35f, 0.7f, 0.7f),
                new Rect(PostPosition.x - 0.5f, PostPosition.z - 0.5f, 1.0f, 1.0f),
                new Rect(PlantPosition.x - 0.5f, PlantPosition.z - 0.5f, 1.0f, 1.0f),
            };
        }

        private static PaletteMeshBuilder BuildRoom()
        {
            var b = new PaletteMeshBuilder();
            AddFloorAndWalls(b);
            AddRug(b, new Vector3(0f, 0.04f, -0.3f));
            AddSofa(b, SofaPosition);
            AddCatBed(b, BedPosition);
            AddBowl(b, BowlPosition);
            AddYarn(b, YarnPosition);
            AddScratchPost(b, PostPosition);
            AddPlant(b, PlantPosition);
            return b;
        }

        private static void AddFloorAndWalls(PaletteMeshBuilder b)
        {
            float halfW = RoomWidth * 0.5f;
            float halfD = RoomDepth * 0.5f;

            // The floor runs on past the front edge so it fills the screen down to the bottom bar.
            const float apron = 2.5f;
            Block(b, new Vector3(0f, -0.5f, -apron * 0.5f), new Vector3(RoomWidth, 0.5f, RoomDepth + apron), WorldPalette.Wood);

            for (int i = 0; i < 7; i++)
            {
                // Planks sit slightly apart so the darker slab shows as joints.
                Plate(b, new Vector3(-halfW + 0.5f + i, 0f, -apron * 0.5f), new Vector2(0.94f, RoomDepth + apron - 0.1f), 0.04f, WorldPalette.WoodLight);
            }

            // Walls with a skirting board and a top trim.
            Block(b, new Vector3(0f, -0.2f, halfD + 0.15f), new Vector3(RoomWidth + 0.3f, 3.8f, 0.3f), WorldPalette.Cream);
            Block(b, new Vector3(-halfW - 0.15f, -0.2f, 0f), new Vector3(0.3f, 3.8f, RoomDepth), WorldPalette.Cream);
            Block(b, new Vector3(0f, 0f, halfD - 0.03f), new Vector3(RoomWidth, 0.3f, 0.1f), WorldPalette.WoodLight);
            Block(b, new Vector3(-halfW + 0.03f, 0f, 0f), new Vector3(0.1f, 0.3f, RoomDepth), WorldPalette.WoodLight);
            Block(b, new Vector3(0f, 3.45f, halfD + 0.05f), new Vector3(RoomWidth + 0.3f, 0.16f, 0.22f), WorldPalette.WoodLight);
            Block(b, new Vector3(-halfW - 0.05f, 3.45f, 0f), new Vector3(0.22f, 0.16f, RoomDepth), WorldPalette.WoodLight);

            // Window with frame, cross bars and sill.
            Vector3 window = new Vector3(-1.6f, 1.45f, halfD - 0.02f);
            Block(b, window, new Vector3(1.9f, 1.45f, 0.1f), WorldPalette.WoodLight);
            Block(b, window + new Vector3(0f, 0.12f, -0.04f), new Vector3(1.6f, 1.2f, 0.08f), WorldPalette.Sky);
            Block(b, window + new Vector3(0f, 0.12f, -0.08f), new Vector3(0.08f, 1.2f, 0.06f), WorldPalette.WoodLight);
            Block(b, window + new Vector3(0f, 0.68f, -0.08f), new Vector3(1.6f, 0.08f, 0.06f), WorldPalette.WoodLight);
            Block(b, window + new Vector3(0f, -0.08f, -0.14f), new Vector3(2.1f, 0.12f, 0.3f), WorldPalette.WoodLight);

            // A framed picture and a small shelf on the left wall.
            Vector3 picture = new Vector3(-halfW + 0.02f, 1.6f, 1.0f);
            Block(b, picture, new Vector3(0.1f, 1.1f, 1.5f), WorldPalette.Olive);
            Block(b, picture + new Vector3(0.04f, 0.14f, 0f), new Vector3(0.08f, 0.82f, 1.22f), WorldPalette.LampYellow);
            Block(b, picture + new Vector3(0.06f, 0.26f, 0.2f), new Vector3(0.06f, 0.36f, 0.5f), WorldPalette.EarthOrange);
            Block(b, new Vector3(-halfW + 0.2f, 2.0f, -1.6f), new Vector3(0.4f, 0.1f, 1.3f), WorldPalette.WoodLight);
            Block(b, new Vector3(-halfW + 0.2f, 2.1f, -1.95f), new Vector3(0.28f, 0.3f, 0.26f), WorldPalette.Terracotta);
            Block(b, new Vector3(-halfW + 0.2f, 2.1f, -1.45f), new Vector3(0.24f, 0.4f, 0.12f), WorldPalette.Glass);
        }

        private static void AddRug(PaletteMeshBuilder b, Vector3 at)
        {
            Plate(b, at, new Vector2(3.4f, 3.0f), 0.04f, WorldPalette.RugCream);
            Plate(b, at + Vector3.up * 0.03f, new Vector2(2.9f, 2.5f), 0.04f, WorldPalette.PinkSoft);
            Plate(b, at + Vector3.up * 0.06f, new Vector2(2.3f, 1.9f), 0.03f, WorldPalette.RugCream);
            Plate(b, at + Vector3.up * 0.08f, new Vector2(2.1f, 1.7f), 0.03f, WorldPalette.PinkSoft);
        }

        private static void AddSofa(PaletteMeshBuilder b, Vector3 at)
        {
            Block(b, at + new Vector3(-1.2f, 0f, -0.4f), new Vector3(0.16f, 0.14f, 0.16f), WorldPalette.WoodDark);
            Block(b, at + new Vector3(1.2f, 0f, -0.4f), new Vector3(0.16f, 0.14f, 0.16f), WorldPalette.WoodDark);
            Block(b, at + new Vector3(0f, 0.1f, 0f), new Vector3(2.8f, 0.45f, 1.1f), WorldPalette.Olive);
            Block(b, at + new Vector3(0f, 0.4f, 0.4f), new Vector3(2.8f, 0.9f, 0.34f), WorldPalette.OliveDark);
            Block(b, at + new Vector3(-1.3f, 0.3f, 0f), new Vector3(0.36f, 0.6f, 1.14f), WorldPalette.OliveDark);
            Block(b, at + new Vector3(1.3f, 0.3f, 0f), new Vector3(0.36f, 0.6f, 1.14f), WorldPalette.OliveDark);
            Block(b, at + new Vector3(-0.56f, 0.52f, -0.08f), new Vector3(1.04f, 0.22f, 0.82f), WorldPalette.OliveLight);
            Block(b, at + new Vector3(0.56f, 0.52f, -0.08f), new Vector3(1.04f, 0.22f, 0.82f), WorldPalette.OliveLight);
            Block(b, at + new Vector3(0.7f, 0.72f, 0.12f), new Vector3(0.5f, 0.45f, 0.18f), WorldPalette.EarthOrange, -12f);
            Block(b, at + new Vector3(-0.8f, 0.72f, 0.14f), new Vector3(0.42f, 0.4f, 0.16f), WorldPalette.RugCream, 10f);
        }

        private static void AddCatBed(PaletteMeshBuilder b, Vector3 at)
        {
            Block(b, at, new Vector3(1.4f, 0.34f, 1.2f), WorldPalette.Terracotta);
            Block(b, at + new Vector3(0f, 0.12f, 0f), new Vector3(1.12f, 0.3f, 0.92f), WorldPalette.PinkSoft);
            Block(b, at + new Vector3(0f, 0.32f, 0.48f), new Vector3(1.4f, 0.26f, 0.24f), WorldPalette.Terracotta);
        }

        private static void AddBowl(PaletteMeshBuilder b, Vector3 at)
        {
            Plate(b, at + new Vector3(0.1f, 0f, 0f), new Vector2(1.0f, 0.7f), 0.03f, WorldPalette.CreamDark);
            Block(b, at, new Vector3(0.56f, 0.2f, 0.56f), WorldPalette.EarthOrange);
            Block(b, at + new Vector3(0f, 0.12f, 0f), new Vector3(0.44f, 0.1f, 0.44f), WorldPalette.WoodLight);
            Block(b, at + new Vector3(0.62f, 0f, 0.05f), new Vector3(0.36f, 0.14f, 0.36f), WorldPalette.Glass);
        }

        private static void AddYarn(PaletteMeshBuilder b, Vector3 at)
        {
            Block(b, at, new Vector3(0.38f, 0.36f, 0.38f), WorldPalette.Rose, 25f);
            Block(b, at + new Vector3(0f, 0.12f, 0f), new Vector3(0.4f, 0.08f, 0.4f), WorldPalette.PinkSoft, 25f);
            Plate(b, at + new Vector3(0.36f, 0f, 0.1f), new Vector2(0.5f, 0.05f), 0.02f, WorldPalette.Rose);
        }

        private static void AddScratchPost(PaletteMeshBuilder b, Vector3 at)
        {
            Block(b, at, new Vector3(0.9f, 0.14f, 0.9f), WorldPalette.RugCream);
            Block(b, at + Vector3.up * 0.1f, new Vector3(0.3f, 1.2f, 0.3f), WorldPalette.CreamDark);

            for (float y = 0.25f; y < 1.2f; y += 0.2f)
            {
                Block(b, at + Vector3.up * y, new Vector3(0.34f, 0.08f, 0.34f), WorldPalette.WoodLight);
            }

            Block(b, at + Vector3.up * 1.28f, new Vector3(0.74f, 0.14f, 0.74f), WorldPalette.RugCream);
            Block(b, at + new Vector3(0.28f, 0.9f, -0.2f), new Vector3(0.03f, 0.3f, 0.03f), WorldPalette.WoodDark);
            Block(b, at + new Vector3(0.28f, 0.82f, -0.2f), new Vector3(0.12f, 0.12f, 0.12f), WorldPalette.LampYellow);
        }

        private static void AddPlant(PaletteMeshBuilder b, Vector3 at)
        {
            Block(b, at, new Vector3(0.6f, 0.5f, 0.6f), WorldPalette.Terracotta);
            Block(b, at + Vector3.up * 0.42f, new Vector3(0.68f, 0.12f, 0.68f), WorldPalette.Terracotta);
            Block(b, at + Vector3.up * 0.5f, new Vector3(0.7f, 0.55f, 0.7f), WorldPalette.Leaf, 20f);
            Block(b, at + new Vector3(0.12f, 0.95f, -0.05f), new Vector3(0.46f, 0.45f, 0.46f), WorldPalette.LeafLight, -15f);
        }

        // ---------------------------------------------------------------- props

        /// <summary>A toy-scale propeller plane facing +Z, wheels on the ground at y = 0.</summary>
        private static PaletteMeshBuilder BuildPlane()
        {
            var b = new PaletteMeshBuilder();
            b.AddRoundedBox(new Vector3(0f, 0.62f, 0f), new Vector3(0.78f, 0.74f, 2.2f), 0.26f, WorldPalette.WarmWhite, 5);
            b.AddRoundedBox(new Vector3(0f, 0.6f, 0.2f), new Vector3(0.82f, 0.18f, 1.6f), 0.07f, WorldPalette.EarthOrange, 4);
            b.AddRoundedBox(new Vector3(0f, 1.0f, 0.35f), new Vector3(0.56f, 0.3f, 0.6f), 0.1f, WorldPalette.Glass, 4);

            // Wings, tail and stabiliser.
            b.AddRoundedBox(new Vector3(0f, 0.55f, 0.15f), new Vector3(3.0f, 0.14f, 0.62f), 0.06f, WorldPalette.EarthOrange, 4);
            b.AddRoundedBox(new Vector3(-1.35f, 0.56f, 0.15f), new Vector3(0.3f, 0.15f, 0.64f), 0.06f, WorldPalette.WarmWhite, 4);
            b.AddRoundedBox(new Vector3(1.35f, 0.56f, 0.15f), new Vector3(0.3f, 0.15f, 0.64f), 0.06f, WorldPalette.WarmWhite, 4);
            b.AddRoundedBox(new Vector3(0f, 1.15f, -0.9f), new Vector3(0.12f, 0.62f, 0.46f), 0.05f, WorldPalette.EarthOrange, 4);
            b.AddRoundedBox(new Vector3(0f, 0.72f, -0.95f), new Vector3(1.2f, 0.1f, 0.36f), 0.04f, WorldPalette.EarthOrange, 4);

            // Nose cone block, hub and blades.
            b.AddRoundedBox(new Vector3(0f, 0.62f, 1.12f), new Vector3(0.5f, 0.5f, 0.2f), 0.1f, WorldPalette.RoofRed, 4);
            b.AddRoundedBox(new Vector3(0f, 0.62f, 1.24f), new Vector3(0.16f, 0.16f, 0.1f), 0.04f, WorldPalette.StoneDark, 3);
            b.AddRoundedBox(new Vector3(0f, 0.62f, 1.28f), new Vector3(1.0f, 0.12f, 0.04f), 0.02f, WorldPalette.WoodDark, 2);

            // Wheels on struts.
            b.AddRoundedBox(new Vector3(-0.42f, 0.14f, 0.35f), new Vector3(0.12f, 0.28f, 0.28f), 0.08f, WorldPalette.RunwayDark, 4);
            b.AddRoundedBox(new Vector3(0.42f, 0.14f, 0.35f), new Vector3(0.12f, 0.28f, 0.28f), 0.08f, WorldPalette.RunwayDark, 4);
            b.AddRoundedBox(new Vector3(0f, 0.1f, -0.8f), new Vector3(0.1f, 0.2f, 0.2f), 0.06f, WorldPalette.RunwayDark, 4);
            b.AddRoundedBox(new Vector3(-0.42f, 0.3f, 0.35f), new Vector3(0.05f, 0.25f, 0.05f), 0.02f, WorldPalette.StoneDark, 2);
            b.AddRoundedBox(new Vector3(0.42f, 0.3f, 0.35f), new Vector3(0.05f, 0.25f, 0.05f), 0.02f, WorldPalette.StoneDark, 2);
            return b;
        }

        /// <summary>A cloud of soft blocks, white on top with a shaded underside.</summary>
        private static PaletteMeshBuilder BuildCloud()
        {
            var b = new PaletteMeshBuilder();
            Block(b, new Vector3(0f, -0.5f, 0f), new Vector3(2.6f, 0.45f, 1.1f), WorldPalette.CloudShade);
            Block(b, new Vector3(0f, -0.35f, 0f), new Vector3(1.4f, 1.0f, 1.0f), WorldPalette.Cloud, 8f);
            Block(b, new Vector3(0.85f, -0.4f, 0.05f), new Vector3(1.0f, 0.72f, 0.9f), WorldPalette.Cloud, -12f);
            Block(b, new Vector3(-0.85f, -0.42f, 0.1f), new Vector3(0.9f, 0.62f, 0.8f), WorldPalette.Cloud, 15f);
            return b;
        }
    }
}
