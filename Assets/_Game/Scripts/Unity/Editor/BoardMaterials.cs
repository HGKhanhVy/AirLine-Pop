using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>The materials the board's 3D models are drawn with.</summary>
    public static class BoardMaterials
    {
        public const string LitShaderPath = "Assets/_Game/Shaders/StylizedLit.shader";
        public const string ShadowShaderPath = "Assets/_Game/Shaders/PlanarShadow.shader";

        private const string GroundDetailPath = "Assets/_Game/Art/Flight/tex_ground_detail.png";
        private const int DetailSize = 256;
        private const int GroundDetailSeed = 7;

        // The stencil bit the airplane's shadow darkens each pixel once with. Other planar
        // shadows should take bits of their own so they do not block each other.
        public const int AirplaneShadowStencil = 64;

        public static Shader LitShader => AssetDatabase.LoadAssetAtPath<Shader>(LitShaderPath);

        public static Shader ShadowShader => AssetDatabase.LoadAssetAtPath<Shader>(ShadowShaderPath);

        /// <summary>Depth tested and depth writing, so the walls of blocks tuck under the block in front.</summary>
        public static Material CreateSolid(Shader lit, float specular, float gloss, float rim)
        {
            var material = new Material(lit);
            material.SetFloat("_Specular", specular);
            material.SetFloat("_Gloss", gloss);
            material.SetFloat("_Rim", rim);
            material.SetFloat("_ZWrite", 1f);
            material.SetFloat("_ZTest", (float)CompareFunction.LessEqual);
            material.SetFloat("_Cull", (float)CullMode.Back);
            return material;
        }

        /// <summary>
        /// Gives a material the shared ground grain, which shades its upward faces so they
        /// read as grass, sand or water rather than flat paint.
        /// </summary>
        /// <param name="scale">Repeats per world unit; larger is finer grain.</param>
        /// <param name="strength">How far the grain darkens and lightens, from 0 to 1.</param>
        public static Material WithGroundDetail(Material material, float scale, float strength)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(GroundDetailPath);

            if (texture == null)
            {
                texture = ProceduralTextures.WriteDetail(GroundDetailPath, DetailSize, GroundDetailSeed);
            }

            return WithDetail(material, texture, scale, strength);
        }

        private static Material WithDetail(Material material, Texture2D texture, float scale, float strength)
        {
            material.SetTexture("_DetailTex", texture);
            material.SetFloat("_DetailScale", scale);
            material.SetFloat("_DetailStrength", strength);
            return material;
        }

        public static Material CreateShadow(Shader shadow, float planeDepth, int stencilBit)
        {
            var material = new Material(shadow);
            material.SetFloat("_PlaneDepth", planeDepth);
            material.SetFloat("_StencilBit", stencilBit);
            return material;
        }

        public static void SetUpRenderer(MeshRenderer renderer, Material material, int sortingOrder)
        {
            renderer.sharedMaterial = material;
            renderer.sortingOrder = sortingOrder;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }
    }
}
