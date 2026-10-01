using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Bakes one prefab per <see cref="CatBreedSO"/> from the CubeAnimals base model.
    ///
    /// The pack paints every face by pointing its UVs at a swatch of one shared atlas, so a
    /// new coat is only a UV shift: the rig, the animations, the material and the draw call
    /// batching all stay those of the original asset. Meshes are copied, never edited in
    /// place, so the vendor folder stays untouched apart from its animation import list.
    /// </summary>
    public static class CubeCatBuilder
    {
        private const string PackFolder = "Assets/CuteMagic_CubeAnimals_Free/CubeAnimals_Free";
        private const string BasePrefabPath = PackFolder + "/Prefab_1/Fox.prefab";
        private const string BreedFolder = "Assets/_Game/Config/Cats";
        private const string OutputFolder = "Assets/_Game/Art/Cats";

        // Swatches the base model paints with; a breed maps each onto its own coat.
        private static readonly PaletteCell BaseCoat = new PaletteCell(4, 9);
        private static readonly PaletteCell BaseShade = new PaletteCell(3, 9);
        private static readonly PaletteCell BaseEarEdge = new PaletteCell(3, 12);
        private static readonly PaletteCell BaseAccent = new PaletteCell(11, 13);

        [MenuItem("Tools/AirLine Pop/Build Cube Cats")]
        public static void BuildFromMenu()
        {
            EditorUtility.DisplayDialog("AirLine Pop", Build(), "OK");
        }

        public static string Build()
        {
            AssetWriter.EnsureFolder(BreedFolder);
            AssetWriter.EnsureFolder(OutputFolder);

            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefabPath);

            if (basePrefab == null)
            {
                return "Missing base model at " + BasePrefabPath;
            }

            var controllerBuilder = new CatAnimatorBuilder(PackFolder, OutputFolder);
            var controller = controllerBuilder.Build();
            List<CatBreedSO> breeds = LoadOrSeedBreeds();

            foreach (CatBreedSO breed in breeds)
            {
                breed.EditorSetPrefab(BakeBreed(basePrefab, breed, controller));
                EditorUtility.SetDirty(breed);
            }

            AssetDatabase.SaveAssets();
            return "Baked " + breeds.Count + " cats into " + OutputFolder + ".";
        }

        private static CatView BakeBreed(GameObject basePrefab, CatBreedSO breed, RuntimeAnimatorController controller)
        {
            string meshPath = OutputFolder + "/Cat_" + breed.Id + "_Meshes.asset";
            AssetDatabase.DeleteAsset(meshPath);

            var meshHolder = new Mesh { name = "Cat_" + breed.Id };
            AssetDatabase.CreateAsset(meshHolder, meshPath);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.name = "Cat_" + breed.Id;

            // Cats never sample the repainted rows, so they draw correctly on the world atlas.
            Material worldMaterial = WorldMaterial.Ensure();
            var hidden = new HashSet<string>(breed.HiddenParts);
            var preserved = new HashSet<string>(breed.PreservedParts);

            foreach (SkinnedMeshRenderer part in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (hidden.Contains(part.name))
                {
                    Object.DestroyImmediate(part.gameObject);
                    continue;
                }

                Mesh coat = Object.Instantiate(part.sharedMesh);
                coat.name = part.name;

                if (!preserved.Contains(part.name))
                {
                    Repaint(coat, breed.Remaps);
                }

                AssetDatabase.AddObjectToAsset(coat, meshHolder);
                part.sharedMesh = coat;
                part.sharedMaterial = worldMaterial;
            }

            // The pack's own prefab has no controller; ours drives locomotion and actions.
            Animator animator = instance.GetComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            // A box round the body is enough to tap; the view links it so nothing is looked up at runtime.
            BoxCollider tap = instance.AddComponent<BoxCollider>();
            tap.center = new Vector3(0f, 0.25f, 0f);
            tap.size = new Vector3(0.6f, 0.5f, 0.6f);
            CatView view = instance.AddComponent<CatView>();
            view.EditorLink(animator, tap);

            string prefabPath = OutputFolder + "/Cat_" + breed.Id + ".prefab";
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Object.DestroyImmediate(instance);
            return saved.GetComponent<CatView>();
        }

        private static void Repaint(Mesh mesh, PaletteCellRemap[] remaps)
        {
            Vector2[] uvs = mesh.uv;

            for (int i = 0; i < uvs.Length; i++)
            {
                PaletteCell cell = PaletteCell.FromUv(uvs[i]);

                for (int r = 0; r < remaps.Length; r++)
                {
                    if (remaps[r].From.Equals(cell))
                    {
                        uvs[i] += cell.OffsetTo(remaps[r].To);
                        break;
                    }
                }
            }

            mesh.uv = uvs;
        }

        private static List<CatBreedSO> LoadOrSeedBreeds()
        {
            var breeds = new List<CatBreedSO>();

            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(CatBreedSO), new[] { BreedFolder }))
            {
                breeds.Add(AssetDatabase.LoadAssetAtPath<CatBreedSO>(AssetDatabase.GUIDToAssetPath(guid)));
            }

            if (breeds.Count > 0)
            {
                return breeds;
            }

            // One starter regular, then four more who join as flights are flown.
            breeds.Add(SeedBreed("muop", "Tabby", 0, true, new PaletteCell(6, 2), new PaletteCell(4, 2), new PaletteCell(10, 2)));
            breeds.Add(SeedBreed("tro", "Ash", 6, false, new PaletteCell(8, 13), new PaletteCell(6, 13), new PaletteCell(11, 13)));
            breeds.Add(SeedBreed("kem", "Cream", 15, false, new PaletteCell(9, 2), new PaletteCell(2, 2), new PaletteCell(10, 2)));
            breeds.Add(SeedBreed("mun", "Ebony", 30, false, new PaletteCell(3, 13), new PaletteCell(1, 13), new PaletteCell(11, 13)));
            breeds.Add(SeedBreed("bo", "Butter", 50, false, new PaletteCell(11, 13), new PaletteCell(7, 2), new PaletteCell(12, 0)));
            return breeds;
        }

        private static CatBreedSO SeedBreed(string id, string name, int arrivalFlight, bool isStarter, PaletteCell coat, PaletteCell shade, PaletteCell accent)
        {
            var breed = ScriptableObject.CreateInstance<CatBreedSO>();
            breed.EditorConfigure(id, name, arrivalFlight, isStarter, new[]
            {
                new PaletteCellRemap(BaseCoat, coat),
                new PaletteCellRemap(BaseShade, shade),
                new PaletteCellRemap(BaseEarEdge, shade),
                new PaletteCellRemap(BaseAccent, accent),
            });
            AssetDatabase.CreateAsset(breed, BreedFolder + "/CatBreed_" + id + ".asset");
            return breed;
        }
    }
}
