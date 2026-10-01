using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Colours a square's 3D block: the top takes the palette colour, the walls a deeper
    /// shade of it, so a grass square has dark green sides and a gold one dark gold.
    /// The colours go through a property block so every square shares one material; the
    /// block is reused from the pool, so the property block is created once per square.
    /// </summary>
    public sealed class MeshCellFace : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int WallColorId = Shader.PropertyToID("_WallColor");

        [SerializeField] private MeshRenderer blockRenderer;

        [Tooltip("How dark the walls are next to the top, as a share of the top colour.")]
        [SerializeField, Range(0f, 1f)] private float wallShade = 0.72f;

        private MaterialPropertyBlock properties;

        public void SetColor(Color color)
        {
            properties ??= new MaterialPropertyBlock();
            properties.SetColor(BaseColorId, color);

            var wall = new Color(color.r * wallShade, color.g * wallShade, color.b * wallShade, color.a);
            properties.SetColor(WallColorId, wall);
            blockRenderer.SetPropertyBlock(properties);
        }
    }
}
