using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The paper thrown when a board is finished.
    ///
    /// One system serves the whole board rather than one per cell. Unity already recycles
    /// particles inside a system, so bursting at ninety positions allocates nothing and
    /// still draws in one batch. GDD 10 asks for confetti that is pooled rather than
    /// spawned, and a system that is never created or destroyed is the cheapest form of
    /// that.
    /// </summary>
    public sealed class ConfettiView : MonoBehaviour
    {
        [SerializeField] private ParticleSystem particles;
        [SerializeField] private ThemeSO theme;

        [Tooltip("Pieces thrown from one cell, split evenly between the theme colours.")]
        [SerializeField, Min(4)] private int piecesPerCell = 12;

        private ParticleSystem.EmitParams emitParams;
        private Color[] palette;

        private void Awake()
        {
            // Built once. A win must not allocate while the wave is running.
            palette = new[] { theme.Won, theme.Head, theme.Start, Color.white };

            // The shape module spreads the pieces around the point handed in below.
            emitParams.applyShapeToPosition = true;
        }

        /// <summary>Throws one cell's worth of paper from a point on the board.</summary>
        public void Burst(Vector3 worldPosition)
        {
            if (particles == null)
            {
                return;
            }

            emitParams.position = worldPosition;
            int perColour = Mathf.Max(1, piecesPerCell / palette.Length);

            // A colour at a time, because one EmitParams carries one start colour. Four
            // small bursts at the same point read as a single mixed handful.
            for (int i = 0; i < palette.Length; i++)
            {
                emitParams.startColor = palette[i];
                particles.Emit(emitParams, perColour);
            }
        }
    }
}
