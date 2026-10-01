using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Builds the low poly world round the board: the board's blocks rise straight out of a
    /// tropical sea, faceted sandy islands with groves of cone pines, rock islets and a
    /// lighthouse frame it from the edges of the view, chunky clouds drift over the top and
    /// bottom corners, and a soft vignette darkens the frame. The camera looks down almost from overhead.
    ///
    /// The sea and the clouds hang off a <see cref="LandscapeStage"/>, which keeps them
    /// framed the same on every level. Rerunnable; objects are reused rather than
    /// duplicated, and the earlier backdrops, the floating platform and the depth of field
    /// set-up are removed.
    /// </summary>
    public static class IslandWorldInstaller
    {
        private const string GameplayPrefabPath = "Assets/_Game/Prefabs/SingleLineGameplay.prefab";
        private const string ArtFolder = "Assets/_Game/Art/Flight";
        private const string LandscapeMeshPath = ArtFolder + "/Landscape.asset";
        private const string SeaMeshPath = ArtFolder + "/Sea.asset";
        private const string SeaMaterialPath = ArtFolder + "/mat_sea.mat";
        private const string LandscapeMaterialPath = ArtFolder + "/mat_landscape.mat";
        private const string CloudMaterialPath = ArtFolder + "/mat_cloud.mat";
        private const string CloudMeshPathFormat = ArtFolder + "/Cloud{0}.asset";
        private const string SoftShadowSpritePath = ArtFolder + "/soft_shadow.png";
        private const string VignetteSpritePath = ArtFolder + "/vignette.png";
        private const string LightingPath = "Assets/_Game/Config/BoardLighting.asset";

        private const string StageObjectName = "Landscape";
        private const string GroundObjectName = "Ground";
        private const string SeaObjectName = "Sea";
        private const string CloudsObjectName = "Clouds";
        private const string VignetteObjectName = "Vignette";
        private static readonly string[] OldRootChildren = { "Airfield", "Depth FX" };
        private static readonly string[] OldCameraChildren = { "Sea", "Sky", "Clouds" };
        private static readonly string[] OldBoardChildren = { "Apron", "Platform" };
        private static readonly string[] OldAssets =
        {
            ArtFolder + "/Airfield.asset",
            ArtFolder + "/mat_airfield.mat",
            ArtFolder + "/sky_gradient.png",
            ArtFolder + "/apron.png",
            ArtFolder + "/mat_platform.mat",
            ArtFolder + "/bg_sea.png",
            ArtFolder + "/cloud_a.png",
            ArtFolder + "/cloud_b.png",
            ArtFolder + "/tex_water_ripples.png",
            "Assets/_Game/Config/GameplayDepthFX.asset"
        };

        /// <summary>The camera distance the scenery is laid out for, about that of a mid-sized board.</summary>
        public const float ReferenceDistance = 20f;

        /// <summary>Depth of the sea below the block tops. The blocks' feet stand just under it.</summary>
        public const float SeaDepth = 0.38f;

        // The block walls reach this far down, so the framer fits the near walls too.
        private const float BlockFoot = 0.445f;

        // A three-quarter view from high up: enough tilt to see the walls of the blocks and
        // the cliffs of the islands, not so much that the far rows shrink.
        private const float CameraTilt = 35f;

        // The islands are drawn as if seen from this tilt, lower than the board's camera:
        // the land is sheared back by the difference, so its cliffs and trees show more of
        // their sides while the board keeps its flatter, easier to read view.
        private const float SceneryTilt = 45f;
        private const float CameraFieldOfView = 32f;

        private const int CloudVariants = 3;
        private const float CloudShadowLift = 0.02f;

        // A portrait phone; wider screens see more of the sides.
        private const float ReferenceAspect = 9f / 16f;
        private const float PixelsPerUnit = 100f;

        private static readonly Color CloudShadow = new Color(0.02f, 0.12f, 0.3f, 0.14f);
        private static readonly Color VignetteRim = new Color(0.01f, 0.07f, 0.2f, 0.4f);

        // Where each cloud sits on screen, how far above the board, how big and which
        // shape. Big soft clouds over the corners, only in the bands above and below the
        // board: they drift sideways, so one never crosses the board itself.
        private static readonly (Vector2 viewport, float height, float scale, int variant)[] CloudSlots =
        {
            (new Vector2(-0.02f, 0.86f), 2.6f, 0.8f, 0),
            (new Vector2(0.02f, 0.2f), 2.4f, 0.85f, 2),
            (new Vector2(1.02f, 0.05f), 2.8f, 0.75f, 1),
            (new Vector2(-0.6f, 0.93f), 2.8f, 0.6f, 1),
            (new Vector2(-0.7f, 0.1f), 2.6f, 0.65f, 0)
        };

        public static string Install()
        {
            Shader lit = BoardMaterials.LitShader;
            Sprite softShadow = SpriteImport.Import(SoftShadowSpritePath, PixelsPerUnit);
            var lighting = AssetDatabase.LoadAssetAtPath<BoardLightingSO>(LightingPath);

            if (lit == null || softShadow == null || lighting == null)
            {
                return "Missing " + BoardMaterials.LitShaderPath + ", " + SoftShadowSpritePath + " or " + LightingPath + ".";
            }

            AssetWriter.EnsureFolder(ArtFolder);

            // Plain vertex colour: the facets carry the shading, a grain would only muddy them.
            Material landscapeMaterial = AssetWriter.SaveMaterial(LandscapeMaterialPath,
                BoardMaterials.CreateSolid(lit, specular: 0.05f, gloss: 14f, rim: 0.04f));

            // A touch of gloss, so the open water glints where the light catches it.
            Material seaMaterial = AssetWriter.SaveMaterial(SeaMaterialPath,
                BoardMaterials.CreateSolid(lit, specular: 0.12f, gloss: 30f, rim: 0.02f));

            // A light rim softens the edges of the clouds without washing out the pale blue
            // their undersides are painted with.
            Material cloudMaterial = AssetWriter.SaveMaterial(CloudMaterialPath,
                BoardMaterials.CreateSolid(lit, specular: 0f, gloss: 8f, rim: 0.15f));

            var cloudMeshes = new Mesh[CloudVariants];

            for (int i = 0; i < CloudVariants; i++)
            {
                cloudMeshes[i] = AssetWriter.SaveMesh(string.Format(CloudMeshPathFormat, i), CloudMeshBuilder.Build(i));
            }

            Sprite vignette = ProceduralTextures.WriteVignette(VignetteSpritePath, 256, VignetteRim, 0.55f, PixelsPerUnit);
            GameObject contents = PrefabUtility.LoadPrefabContents(GameplayPrefabPath);

            try
            {
                var framer = contents.GetComponentInChildren<BoardCameraFramer>(true);
                var board = contents.GetComponentInChildren<BoardView>(true);

                if (framer == null || board == null)
                {
                    return "No BoardCameraFramer or BoardView in " + GameplayPrefabPath + "; prefab left unchanged.";
                }

                LandscapeLayoutView view = SetUpView(framer);
                var (seaMesh, landMesh) = LandscapeMeshBuilder.Build(view, SceneryTilt - CameraTilt);
                Mesh sea = AssetWriter.SaveMesh(SeaMeshPath, seaMesh);
                Mesh land = AssetWriter.SaveMesh(LandscapeMeshPath, landMesh);

                RemoveChildren(contents.transform, OldRootChildren);
                RemoveChildren(framer.transform, OldCameraChildren);
                RemoveChildren(board.transform, OldBoardChildren);

                Transform stage = SetUpStage(contents.transform, framer);
                SetUpMesh(stage, SeaObjectName, sea, seaMaterial);
                SetUpMesh(stage, GroundObjectName, land, landscapeMaterial);
                SetUpClouds(stage, view, cloudMeshes, cloudMaterial, softShadow, lighting.LightDirection);
                SetUpVignette(framer, vignette);

                PrefabUtility.SaveAsPrefabAsset(contents, GameplayPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            DeleteOldAssets();
            AssetDatabase.SaveAssets();
            return GameplayPrefabPath + ": island sea, clouds and vignette set round the board.";
        }

        /// <summary>
        /// Sets the camera's view, tells the framer how deep the block walls reach, and
        /// turns post processing off: nothing in this look needs it, and it costs a full
        /// screen pass on a phone.
        /// </summary>
        private static LandscapeLayoutView SetUpView(BoardCameraFramer framer)
        {
            var framerObject = new SerializedObject(framer);
            framerObject.FindProperty("blockDepth").floatValue = BlockFoot;
            framerObject.FindProperty("tilt").floatValue = CameraTilt;
            framerObject.FindProperty("fieldOfView").floatValue = CameraFieldOfView;
            framerObject.ApplyModifiedPropertiesWithoutUndo();

            var cameraData = framer.GetComponent<UniversalAdditionalCameraData>();

            if (cameraData != null)
            {
                cameraData.renderPostProcessing = false;
                cameraData.requiresDepthOption = CameraOverrideOption.UsePipelineSettings;
            }

            return new LandscapeLayoutView(CameraTilt, CameraFieldOfView, ReferenceAspect, ReferenceDistance, SeaDepth);
        }

        private static Transform SetUpStage(Transform root, BoardCameraFramer framer)
        {
            Transform stageTransform = AssetWriter.FindOrCreateChild(root, StageObjectName);
            stageTransform.localRotation = Quaternion.identity;

            var stage = AssetWriter.GetOrAdd<LandscapeStage>(stageTransform.gameObject);
            var stageObject = new SerializedObject(stage);
            stageObject.FindProperty("framer").objectReferenceValue = framer;
            stageObject.FindProperty("stage").objectReferenceValue = stageTransform;
            stageObject.FindProperty("referenceDistance").floatValue = ReferenceDistance;
            stageObject.FindProperty("groundDepth").floatValue = SeaDepth;
            stageObject.ApplyModifiedPropertiesWithoutUndo();
            return stageTransform;
        }

        private static void SetUpMesh(Transform stage, string name, Mesh mesh, Material material)
        {
            Transform target = AssetWriter.FindOrCreateChild(stage, name);
            ResetLocal(target);
            AssetWriter.GetOrAdd<MeshFilter>(target.gameObject).sharedMesh = mesh;
            BoardMaterials.SetUpRenderer(AssetWriter.GetOrAdd<MeshRenderer>(target.gameObject), material, BoardSortingOrder.Landscape);
        }

        /// <summary>
        /// Each cloud is an empty that drifts, holding the puff itself and its soft shadow
        /// on the sea straight down the light from it, so the two move together.
        /// </summary>
        private static void SetUpClouds(Transform stage, LandscapeLayoutView view, Mesh[] meshes, Material material,
            Sprite shadowSprite, Vector3 light)
        {
            Transform layer = AssetWriter.FindOrCreateChild(stage, CloudsObjectName);
            ResetLocal(layer);

            // Earlier layouts had more clouds; drop the spares.
            for (int i = layer.childCount - 1; i >= CloudSlots.Length; i--)
            {
                Object.DestroyImmediate(layer.GetChild(i).gameObject);
            }

            light.z = Mathf.Max(0.05f, light.z);
            var clouds = new Transform[CloudSlots.Length];

            for (int i = 0; i < CloudSlots.Length; i++)
            {
                var (viewport, height, scale, variant) = CloudSlots[i];
                Transform cloud = AssetWriter.FindOrCreateChild(layer, "Cloud " + (i + 1));
                cloud.localPosition = view.OnPlane(viewport.x, viewport.y, -height);
                cloud.localRotation = Quaternion.identity;
                cloud.localScale = Vector3.one;

                Transform puff = AssetWriter.FindOrCreateChild(cloud, "Puff");
                ResetLocal(puff);
                puff.localScale = Vector3.one * scale;
                AssetWriter.GetOrAdd<MeshFilter>(puff.gameObject).sharedMesh = meshes[variant];
                BoardMaterials.SetUpRenderer(AssetWriter.GetOrAdd<MeshRenderer>(puff.gameObject), material, BoardSortingOrder.Cloud);

                // The cloud's height above the sea, and where the light carries its shadow.
                float drop = -cloud.localPosition.z - CloudShadowLift;
                Transform shadow = AssetWriter.FindOrCreateChild(cloud, "Shadow");
                shadow.localPosition = new Vector3(light.x / light.z * drop, light.y / light.z * drop, drop);
                shadow.localRotation = Quaternion.identity;
                float shadowScale = scale * 2.2f / Mathf.Max(0.01f, shadowSprite.bounds.size.x);
                shadow.localScale = new Vector3(shadowScale, shadowScale * 0.7f, 1f);

                var shadowRenderer = AssetWriter.GetOrAdd<SpriteRenderer>(shadow.gameObject);
                shadowRenderer.sprite = shadowSprite;
                shadowRenderer.color = CloudShadow;
                shadowRenderer.sortingOrder = BoardSortingOrder.CloudShadow;

                clouds[i] = cloud;
            }

            var drift = AssetWriter.GetOrAdd<CloudLayer>(layer.gameObject);
            var driftObject = new SerializedObject(drift);
            driftObject.FindProperty("halfSpan").floatValue = 7f;
            driftObject.FindProperty("speed").floatValue = 0.05f;
            SerializedProperty cloudsProperty = driftObject.FindProperty("clouds");
            cloudsProperty.arraySize = clouds.Length;

            for (int i = 0; i < clouds.Length; i++)
            {
                cloudsProperty.GetArrayElementAtIndex(i).objectReferenceValue = clouds[i];
            }

            driftObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetUpVignette(BoardCameraFramer framer, Sprite sprite)
        {
            Transform overlay = AssetWriter.FindOrCreateChild(framer.transform, VignetteObjectName);
            ResetLocal(overlay);

            var renderer = AssetWriter.GetOrAdd<SpriteRenderer>(overlay.gameObject);
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = BoardSortingOrder.Vignette;

            var fit = AssetWriter.GetOrAdd<CameraVignette>(overlay.gameObject);
            var fitObject = new SerializedObject(fit);
            fitObject.FindProperty("targetCamera").objectReferenceValue = framer.GetComponent<Camera>();
            fitObject.FindProperty("overlay").objectReferenceValue = renderer;
            fitObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RemoveChildren(Transform parent, string[] names)
        {
            foreach (string name in names)
            {
                Transform child = parent.Find(name);

                if (child != null)
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private static void DeleteOldAssets()
        {
            foreach (string path in OldAssets)
            {
                if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }
        }

        private static void ResetLocal(Transform target)
        {
            target.localPosition = Vector3.zero;
            target.localRotation = Quaternion.identity;
            target.localScale = Vector3.one;
        }
    }
}
