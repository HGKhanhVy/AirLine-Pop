using UnityEditor;
using UnityEngine;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Replaces the flat square art with a lit 3D block of land standing in the sea: its
    /// top in the palette colour, its walls a deeper shade of it, a soft shadow on the
    /// water round its foot, and trees and rocks on the squares the route has not crossed.
    ///
    /// The block is built to cover the same footprint the square's art did, and sits under
    /// the square's size root, so it resizes, presses and pops with the square. The old
    /// face sprite stays as the reference CellView colours, but is hidden; the colour now
    /// reaches the block through <see cref="MeshCellFace"/>. Rerunnable.
    /// </summary>
    public static class BlockTileInstaller
    {
        private const string CellPrefabPath = "Assets/_Game/Prefabs/Cell.prefab";
        private const string ArtFolder = "Assets/_Game/Art/Flight";
        private const string MeshPath = ArtFolder + "/Block.asset";
        private const string BlockMaterialPath = ArtFolder + "/mat_block.mat";
        private const string DecorMaterialPath = ArtFolder + "/mat_cell_decor.mat";
        private const string DecorMeshPathFormat = ArtFolder + "/CellDecor{0}.asset";
        private const string ShoreSpritePath = ArtFolder + "/shore.png";

        // Earlier looks, removed on the next run.
        private const string OldShadowMaterialPath = ArtFolder + "/mat_block_shadow.mat";
        private static readonly string[] OldChildNames = { "Block Shadow", "Soft Shadow" };

        private const string SizeRootName = "VisualSizeRoot";
        private const string BlockObjectName = "Block";
        private const string ShoreObjectName = "Shore";
        private const string DecorObjectName = "Decor";
        private const string GlossObjectName = "Inner (4)";

        // Nearly the full cell at a size-root scale of one, so neighbouring blocks stand
        // with only a thin seam of water between them.
        private const float BlockWidth = 0.97f;

        // A soft, toy-like tile of land: generously rounded corners and a plump bevel, with
        // walls tall enough that the tilted camera sees each block rise out of the water.
        private const float BlockHeight = 0.43f;
        private const float CornerRadius = 0.22f;
        private const float Bevel = 0.09f;

        // The top sits a hair below the board surface, so the path and the marks drawn on
        // z = 0 never fight the block top for depth.
        private const float TopInset = 0.015f;

        // The shadow on the water reaches this far out from the block, as a multiple of its width.
        private const float ShoreSpread = 1.35f;
        private const int ShoreTextureSize = 128;
        private static readonly Color ShadeNear = new Color(0.01f, 0.1f, 0.28f, 0.5f);
        private static readonly Color ShadeFar = new Color(0.01f, 0.1f, 0.28f, 0.3f);

        // Squares left bare, next to the ones that get a look.
        private const int BareSlots = 1;
        private const float PixelsPerUnit = 100f;

        public static string Install()
        {
            Shader lit = BoardMaterials.LitShader;

            if (lit == null)
            {
                return "Missing " + BoardMaterials.LitShaderPath + ".";
            }

            AssetWriter.EnsureFolder(ArtFolder);
            Mesh mesh = AssetWriter.SaveMesh(MeshPath,
                RoundedSlabMesh.Build(new Vector2(BlockWidth, BlockWidth), BlockHeight, CornerRadius, Bevel));

            Material block = BoardMaterials.CreateSolid(lit, specular: 0.12f, gloss: 12f, rim: 0.12f);
            BoardMaterials.WithGroundDetail(block, scale: 2.2f, strength: 0.18f);
            block = AssetWriter.SaveMaterial(BlockMaterialPath, block);

            Material decor = AssetWriter.SaveMaterial(DecorMaterialPath,
                BoardMaterials.CreateSolid(lit, specular: 0.05f, gloss: 10f, rim: 0.1f));

            var decorMeshes = new Mesh[CellDecorMeshBuilder.VariantCount];

            for (int i = 0; i < decorMeshes.Length; i++)
            {
                decorMeshes[i] = AssetWriter.SaveMesh(string.Format(DecorMeshPathFormat, i), CellDecorMeshBuilder.Build(i));
            }

            Sprite shore = ProceduralTextures.WriteShore(ShoreSpritePath, ShoreTextureSize,
                1f / ShoreSpread, CornerRadius / (BlockWidth * ShoreSpread), ShadeNear, ShadeFar, PixelsPerUnit);

            string result = WirePrefab(mesh, block, decor, decorMeshes, shore);

            if (AssetDatabase.LoadAssetAtPath<Material>(OldShadowMaterialPath) != null)
            {
                AssetDatabase.DeleteAsset(OldShadowMaterialPath);
            }

            AssetDatabase.SaveAssets();
            return result;
        }

        private static string WirePrefab(Mesh mesh, Material block, Material decor, Mesh[] decorMeshes, Sprite shore)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(CellPrefabPath);

            try
            {
                var cell = contents.GetComponent<CellView>();
                Transform sizeRoot = contents.transform.Find(SizeRootName);

                if (cell == null || sizeRoot == null)
                {
                    return CellPrefabPath + " has no CellView or " + SizeRootName + "; prefab left unchanged.";
                }

                var cellObject = new SerializedObject(cell);
                var face = (SpriteRenderer)cellObject.FindProperty("spriteRenderer").objectReferenceValue;

                MeshRenderer blockRenderer = SetUpMesh(sizeRoot, BlockObjectName, mesh, block, BoardSortingOrder.Cells);
                blockRenderer.transform.localPosition = new Vector3(0f, 0f, TopInset);
                SetUpShore(sizeRoot, shore);
                CellDecor cellDecor = SetUpDecor(sizeRoot, decor, decorMeshes);
                RemoveOldChildren(sizeRoot);

                var meshFace = AssetWriter.GetOrAdd<MeshCellFace>(blockRenderer.gameObject);
                var faceObject = new SerializedObject(meshFace);
                faceObject.FindProperty("blockRenderer").objectReferenceValue = blockRenderer;
                faceObject.ApplyModifiedPropertiesWithoutUndo();

                cellObject.FindProperty("meshFace").objectReferenceValue = meshFace;
                cellObject.FindProperty("decor").objectReferenceValue = cellDecor;
                cellObject.ApplyModifiedPropertiesWithoutUndo();

                if (face != null)
                {
                    HideFlatArt(face);
                }

                PrefabUtility.SaveAsPrefabAsset(contents, CellPrefabPath);
                return CellPrefabPath + ": land block, water shadow and dressing set under " + SizeRootName + ".";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        /// <summary>A soft shadow lying on the sea round the foot of the block, which seats it in the water.</summary>
        private static void SetUpShore(Transform sizeRoot, Sprite sprite)
        {
            Transform target = AssetWriter.FindOrCreateChild(sizeRoot, ShoreObjectName);
            target.localPosition = new Vector3(0f, 0f, IslandWorldInstaller.SeaDepth - 0.005f);
            target.localRotation = Quaternion.identity;
            float scale = BlockWidth * ShoreSpread / sprite.bounds.size.x;
            target.localScale = new Vector3(scale, scale, 1f);

            var renderer = AssetWriter.GetOrAdd<SpriteRenderer>(target.gameObject);
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = BoardSortingOrder.Shore;
        }

        private static CellDecor SetUpDecor(Transform sizeRoot, Material material, Mesh[] meshes)
        {
            MeshRenderer renderer = SetUpMesh(sizeRoot, DecorObjectName, null, material, BoardSortingOrder.Cells);
            renderer.transform.localPosition = new Vector3(0f, 0f, TopInset);

            var decor = AssetWriter.GetOrAdd<CellDecor>(renderer.gameObject);
            var decorObject = new SerializedObject(decor);
            decorObject.FindProperty("decorFilter").objectReferenceValue = renderer.GetComponent<MeshFilter>();
            decorObject.FindProperty("decorRenderer").objectReferenceValue = renderer;

            SerializedProperty variants = decorObject.FindProperty("variants");
            variants.arraySize = meshes.Length + BareSlots;

            for (int i = 0; i < variants.arraySize; i++)
            {
                variants.GetArrayElementAtIndex(i).objectReferenceValue = i < meshes.Length ? meshes[i] : null;
            }

            decorObject.ApplyModifiedPropertiesWithoutUndo();
            return decor;
        }

        private static void RemoveOldChildren(Transform sizeRoot)
        {
            foreach (string name in OldChildNames)
            {
                Transform old = sizeRoot.Find(name);

                if (old != null)
                {
                    Object.DestroyImmediate(old.gameObject);
                }
            }
        }

        private static MeshRenderer SetUpMesh(Transform parent, string name, Mesh mesh, Material material, int sortingOrder)
        {
            Transform target = AssetWriter.FindOrCreateChild(parent, name);
            target.localPosition = Vector3.zero;
            target.localRotation = Quaternion.identity;
            target.localScale = Vector3.one;

            AssetWriter.GetOrAdd<MeshFilter>(target.gameObject).sharedMesh = mesh;
            var renderer = AssetWriter.GetOrAdd<MeshRenderer>(target.gameObject);
            BoardMaterials.SetUpRenderer(renderer, material, sortingOrder);
            return renderer;
        }

        /// <summary>
        /// Hides the square's flat face and its gloss. The sprite is cleared as well as the
        /// renderer disabled, so an animation clip that touches the renderer cannot bring a
        /// flat square back over the block.
        /// </summary>
        private static void HideFlatArt(SpriteRenderer face)
        {
            face.enabled = false;
            face.sprite = null;

            Transform gloss = face.transform.Find(GlossObjectName);

            if (gloss != null && gloss.TryGetComponent(out SpriteRenderer glossRenderer))
            {
                glossRenderer.enabled = false;
            }
        }
    }
}
