using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Hands the board light to the shaders as globals. Set once when enabled rather than
    /// every frame, and also in the Editor so prefabs and scenes preview lit.
    /// </summary>
    [ExecuteAlways]
    public sealed class BoardLighting : MonoBehaviour
    {
        private static readonly int LightDirectionId = Shader.PropertyToID("_AirLineLightDir");
        private static readonly int LightColorId = Shader.PropertyToID("_AirLineLightColor");
        private static readonly int SkyAmbientId = Shader.PropertyToID("_AirLineSkyAmbient");
        private static readonly int GroundAmbientId = Shader.PropertyToID("_AirLineGroundAmbient");
        private static readonly int ShadowColorId = Shader.PropertyToID("_AirLineShadowColor");

        [SerializeField] private BoardLightingSO lighting;

        private void OnEnable()
        {
            Apply();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Apply();
        }
#endif

        public void Apply()
        {
            if (lighting == null)
            {
                return;
            }

            Vector3 direction = lighting.LightDirection;

            // The shadow projection needs the light to travel into the board.
            direction.z = Mathf.Max(0.05f, direction.z);

            Shader.SetGlobalVector(LightDirectionId, direction.normalized);
            Shader.SetGlobalColor(LightColorId, lighting.LightColor);
            Shader.SetGlobalColor(SkyAmbientId, lighting.SkyAmbient);
            Shader.SetGlobalColor(GroundAmbientId, lighting.GroundAmbient);
            Shader.SetGlobalColor(ShadowColorId, lighting.ShadowColor);
        }
    }
}
