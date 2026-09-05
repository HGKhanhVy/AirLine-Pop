using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The dust a cell gives off when a board is finished.
    ///
    /// One system serves the whole board rather than one per cell. Unity already recycles
    /// particles inside a system, so bursting at ninety positions allocates nothing and
    /// still draws in one batch. GDD 10 asks for a celebration that is pooled rather than
    /// spawned, and a system that is never created or destroyed is the cheapest form of
    /// that.
    ///
    /// The colour is handed in per burst instead of read from the theme, so each puff
    /// carries the colour of the square it left and the board stays the only thing that
    /// decides what a cell looks like.
    /// </summary>
    public sealed class CellDustView : MonoBehaviour
    {
        [SerializeField] private ParticleSystem particles;

        [Tooltip("Motes thrown from one cell.")]
        [SerializeField, Min(1)] private int motesPerCell = 14;

        [Tooltip("How much lighter than the cell the dust reads, so it stays visible over it.")]
        [SerializeField, Range(0f, 1f)] private float lift = 0.35f;

        private ParticleSystem.EmitParams emitParams;

        private void Awake()
        {
            // The shape module spreads the motes around the point handed in below.
            emitParams.applyShapeToPosition = true;
        }

        /// <summary>Puffs one cell's worth of dust, in that cell's own colour.</summary>
        public void Burst(Vector3 worldPosition, Color cellColor)
        {
            if (particles == null)
            {
                return;
            }

            emitParams.position = worldPosition;
            emitParams.startColor = Color.Lerp(cellColor, Color.white, lift);

            particles.Emit(emitParams, motesPerCell);
        }
    }
}
