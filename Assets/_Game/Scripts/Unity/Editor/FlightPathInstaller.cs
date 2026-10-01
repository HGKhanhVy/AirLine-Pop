using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Turns the drawn path into a flight route: a dashed line with a 3D airplane flying
    /// its front.
    ///
    /// Creates, or refreshes, the dash texture, the route and airplane materials and the
    /// airplane mesh under <see cref="ArtFolder"/>, then wires them into the gameplay
    /// prefab. Rerunnable: existing assets are overwritten in place so their references
    /// hold, and the airplane object is reused rather than duplicated.
    /// </summary>
    public static class FlightPathInstaller
    {
        private const string ArtFolder = "Assets/_Game/Art/Flight";
        private const string GameplayPrefabPath = "Assets/_Game/Prefabs/SingleLineGameplay.prefab";
        private const string SolidLineMaterialPath = "Assets/_Game/Asset_Resources/Material/mat_line.mat";

        private const string DashTexturePath = ArtFolder + "/tex_flight_dash.png";
        private const string RouteMaterialPath = ArtFolder + "/mat_flight_route.mat";
        private const string AirplaneMaterialPath = ArtFolder + "/mat_airplane.mat";
        private const string ShadowMaterialPath = ArtFolder + "/mat_airplane_shadow.mat";
        private const string MeshPath = ArtFolder + "/Airplane.asset";
        private const string OldPropellerMeshPath = ArtFolder + "/Propeller.asset";

        private const string AirplaneObjectName = "Airplane";
        private const string OldPropellerObjectName = "Propeller";

        // A jet about as wide as a square, like the one parked in the reference art.
        private const float JetWingspan = 1.25f;

        // One tile of the dash texture spans this many path widths: a dash of 1.8 widths
        // and a gap of 1.2. The texture is drawn at the same aspect so the rounded ends
        // stay round. Long and thin like a runway centre line rather than a pipe.
        private const int DashTextureHeight = 32;
        private const float DashTileWidths = 3f;
        private const float DashLengthWidths = 1.8f;
        private const float DashThickness = 0.5f;

        // The road the route is the centre line of, in world units: a board square is
        // about 1.1 across, so a rim of the square's own colour still shows either side.
        private const float RunwayEdgeWidth = 0.82f;
        private const float RunwayWidth = 0.74f;
        private const int RunwayRoundness = 8;

        // Dark tarmac with a white edge line, so the road stands out on both the green and
        // the golden squares.
        private static readonly Color RunwayEdgeColor = new Color(0.95f, 0.95f, 0.97f);
        private static readonly Color AsphaltColor = new Color(0.25f, 0.27f, 0.31f);

        // A white passenger jet with red wing tips, fin and engines, and a dark windscreen.
        private static readonly AirplanePalette Palette = new AirplanePalette(
            body: new Color(0.98f, 0.98f, 1f),
            wing: new Color(0.92f, 0.93f, 0.96f),
            accent: new Color(0.9f, 0.18f, 0.2f),
            glass: new Color(0.12f, 0.2f, 0.36f),
            engine: new Color(0.85f, 0.2f, 0.22f));

        public static string Install()
        {
            Shader lit = BoardMaterials.LitShader;
            Shader shadowShader = BoardMaterials.ShadowShader;
            var solidLine = AssetDatabase.LoadAssetAtPath<Material>(SolidLineMaterialPath);

            if (lit == null || shadowShader == null || solidLine == null)
            {
                return "Missing " + BoardMaterials.LitShaderPath + ", " + BoardMaterials.ShadowShaderPath
                    + " or " + SolidLineMaterialPath + ".";
            }

            AssetWriter.EnsureFolder(ArtFolder);

            Texture2D dash = WriteDashTexture();
            Material route = AssetWriter.SaveMaterial(RouteMaterialPath, new Material(solidLine) { mainTexture = dash });

            // Glossier than the blocks: painted metal. Depth tested, so under the tilted
            // camera its near side covers its far side. Its shadow falls on the block tops.
            Material airplane = AssetWriter.SaveMaterial(AirplaneMaterialPath,
                BoardMaterials.CreateSolid(lit, specular: 0.4f, gloss: 40f, rim: 0.18f));
            Material shadow = AssetWriter.SaveMaterial(ShadowMaterialPath,
                BoardMaterials.CreateShadow(shadowShader, planeDepth: 0f, BoardMaterials.AirplaneShadowStencil));
            Mesh mesh = AssetWriter.SaveMesh(MeshPath, AirplaneMeshBuilder.Build(Palette));

            string prefabResult = WirePrefab(route, solidLine, airplane, shadow, mesh);

            if (AssetDatabase.LoadMainAssetAtPath(OldPropellerMeshPath) != null)
            {
                AssetDatabase.DeleteAsset(OldPropellerMeshPath);
            }

            AssetDatabase.SaveAssets();

            return "Assets written to " + ArtFolder + ".\n" + prefabResult;
        }

        private static Texture2D WriteDashTexture()
        {
            int height = DashTextureHeight;
            int width = Mathf.RoundToInt(height * DashTileWidths);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];

            float radius = DashThickness * 0.5f;
            float centre = DashTileWidths * 0.5f;
            float halfSegment = DashLengthWidths * 0.5f - radius;
            float feather = 1.5f / height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Pixel centre in path widths: one texel is the same size both ways.
                    float u = (x + 0.5f) / height;
                    float v = (y + 0.5f) / height;
                    float along = Mathf.Max(0f, Mathf.Abs(u - centre) - halfSegment);
                    float distance = Mathf.Sqrt(along * along + (v - 0.5f) * (v - 0.5f));
                    float alpha = Mathf.Clamp01((radius - distance) / feather + 0.5f);
                    pixels[y * width + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            System.IO.File.WriteAllBytes(DashTexturePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(DashTexturePath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(DashTexturePath);
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.wrapModeU = TextureWrapMode.Repeat;
            importer.wrapModeV = TextureWrapMode.Clamp;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Texture2D>(DashTexturePath);
        }

        private static string WirePrefab(Material route, Material solidLine, Material airplane, Material shadow,
            Mesh mesh)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(GameplayPrefabPath);

            try
            {
                var path = contents.GetComponentInChildren<PathView>(true);

                if (path == null)
                {
                    return "No PathView in " + GameplayPrefabPath + "; prefab left unchanged.";
                }

                var pathObject = new SerializedObject(path);
                pathObject.FindProperty("sharedMaterial").objectReferenceValue = route;
                pathObject.FindProperty("isDashed").boolValue = true;
                pathObject.FindProperty("retractsOnWin").boolValue = false;

                LineRenderer edge = SetUpRunwayLine(
                    AssetWriter.FindOrCreateChild(path.transform, "Runway Edge"), solidLine, RunwayEdgeWidth, RunwayEdgeColor);
                LineRenderer asphalt = SetUpRunwayLine(
                    AssetWriter.FindOrCreateChild(path.transform, "Runway"), solidLine, RunwayWidth, AsphaltColor);

                SerializedProperty underlays = pathObject.FindProperty("underlays");
                underlays.arraySize = 2;
                underlays.GetArrayElementAtIndex(0).objectReferenceValue = edge;
                underlays.GetArrayElementAtIndex(1).objectReferenceValue = asphalt;
                pathObject.ApplyModifiedPropertiesWithoutUndo();

                Transform parent = path.transform.parent;
                Transform root = AssetWriter.FindOrCreateChild(parent, AirplaneObjectName);
                MeshRenderer body = SetUpMesh(
                    AssetWriter.FindOrCreateChild(root, "Body"), mesh, airplane, BoardSortingOrder.Airplane);
                MeshRenderer shadowRenderer = SetUpMesh(
                    AssetWriter.FindOrCreateChild(root, "Shadow"), mesh, shadow, BoardSortingOrder.AirplaneShadow);

                // The jet has no propeller; the toy plane that came before did.
                Transform oldPropeller = body.transform.Find(OldPropellerObjectName);

                if (oldPropeller != null)
                {
                    Object.DestroyImmediate(oldPropeller.gameObject);
                }

                // An empty anchor on the flight deck for whoever ends up flying the plane.
                Transform seat = AssetWriter.FindOrCreateChild(body.transform, "Pilot Seat");
                seat.localPosition = AirplaneMeshBuilder.PilotSeat;

                var rig = AssetWriter.GetOrAdd<MeshAirplaneRig>(root.gameObject);
                var rigObject = new SerializedObject(rig);
                rigObject.FindProperty("bodyRenderer").objectReferenceValue = body;
                rigObject.FindProperty("shadowRenderer").objectReferenceValue = shadowRenderer;
                rigObject.FindProperty("wingspan").floatValue = JetWingspan;
                rigObject.ApplyModifiedPropertiesWithoutUndo();

                var view = AssetWriter.GetOrAdd<AirplaneView>(root.gameObject);

                var viewObject = new SerializedObject(view);
                viewObject.FindProperty("path").objectReferenceValue = path;
                viewObject.FindProperty("input").objectReferenceValue = contents.GetComponentInChildren<BoardInput>(true);
                viewObject.FindProperty("rig").objectReferenceValue = rig;
                viewObject.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, GameplayPrefabPath);
                return GameplayPrefabPath + ": dashed route and airplane wired under '" + parent.name + "'.";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static LineRenderer SetUpRunwayLine(Transform target, Material material, float width, Color color)
        {
            if (!target.TryGetComponent(out LineRenderer line))
            {
                line = target.gameObject.AddComponent<LineRenderer>();
            }

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

        private static MeshRenderer SetUpMesh(Transform target, Mesh mesh, Material material, int sortingOrder)
        {
            AssetWriter.GetOrAdd<MeshFilter>(target.gameObject).sharedMesh = mesh;
            var renderer = AssetWriter.GetOrAdd<MeshRenderer>(target.gameObject);
            BoardMaterials.SetUpRenderer(renderer, material, sortingOrder);

            // The body used to be flattened into a shadow by its scale; the shader does that now.
            target.localScale = Vector3.one;
            return renderer;
        }
    }
}
