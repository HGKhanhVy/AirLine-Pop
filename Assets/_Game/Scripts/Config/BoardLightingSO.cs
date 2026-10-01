using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The one light the board's 3D models are lit by, and the shadows they cast.
    ///
    /// Directions are in board space: +X right, +Y away from the camera across the board,
    /// +Z down into the ground.
    /// </summary>
    [CreateAssetMenu(menuName = "Single Line/Board Lighting", fileName = "BoardLighting")]
    public sealed class BoardLightingSO : ScriptableObject
    {
        [Tooltip("The way the light travels. The default comes from the upper left, in front " +
                 "of the board, so shadows fall to the lower right.")]
        [SerializeField] private Vector3 lightDirection = new Vector3(0.45f, -0.6f, 1f);

        [SerializeField] private Color lightColor = new Color(1f, 0.96f, 0.9f);
        [SerializeField, Range(0f, 2f)] private float lightIntensity = 0.6f;

        [Tooltip("Ambient light on faces turned up to the sky.")]
        [SerializeField] private Color skyAmbient = new Color(0.5f, 0.49f, 0.58f);

        [Tooltip("Ambient light on faces turned down, such as the underside of a wing.")]
        [SerializeField] private Color groundAmbient = new Color(0.36f, 0.3f, 0.46f);

        [Tooltip("Colour and opacity of the airplane's shadow on the blocks.")]
        [SerializeField] private Color shadowColor = new Color(0.16f, 0.12f, 0.43f, 0.25f);

        public Vector3 LightDirection => lightDirection;

        public Color LightColor => lightColor * lightIntensity;

        public Color SkyAmbient => skyAmbient;

        public Color GroundAmbient => groundAmbient;

        public Color ShadowColor => shadowColor;
    }
}
