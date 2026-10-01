using UnityEditor;
using UnityEngine;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// The one material every cat and prop is drawn with: the CubeAnimals shader on the
    /// world atlas. Sharing it is what lets the whole scene batch.
    /// </summary>
    public static class WorldMaterial
    {
        public const string Folder = "Assets/_Game/Art/World";
        public const string AtlasPath = Folder + "/WorldPalette.png";
        public const string MaterialPath = Folder + "/mat_cube_world.mat";
        private const string ShaderName = "AirLine Pop/Cube World";

        public static Material Ensure()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(AtlasPath);

            // Swatches are flat colour, so a small texture without mipmaps loses nothing
            // and keeps distant faces from bleeding into the black grid lines.
            if (importer.maxTextureSize != 512 || importer.mipmapEnabled)
            {
                importer.maxTextureSize = 512;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }

            var material = new Material(Shader.Find(ShaderName)) { name = "mat_cube_world" };
            material.SetTexture("_AlbedoMap", AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath));
            material.SetFloat("_MetMult", 0f);
            material.SetFloat("_RoughMult", 1f);
            material.enableInstancing = true;
            return AssetWriter.SaveMaterial(MaterialPath, material);
        }
    }
}
