using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The dressing on top of a square: a palm, a bush, a few rocks. Picked from the
    /// square's index, so a board looks the same every time it is played and a square
    /// taken back from the pool never flickers to a different look.
    ///
    /// A square the route crosses is cleared, the way the runway squares in the reference
    /// art carry no trees. That also keeps the airplane, which only ever rests on the
    /// route, clear of anything tall.
    /// </summary>
    public sealed class CellDecor : MonoBehaviour
    {
        [SerializeField] private MeshFilter decorFilter;
        [SerializeField] private MeshRenderer decorRenderer;

        [Tooltip("The looks to pick from. An empty slot leaves the square bare.")]
        [SerializeField] private Mesh[] variants = System.Array.Empty<Mesh>();

        private bool hasLook;

        public void Apply(int cellIndex)
        {
            Mesh mesh = null;

            if (variants.Length > 0)
            {
                // A cheap integer hash, so neighbouring squares do not step through the list in order.
                uint hash = (uint)cellIndex * 2654435761u;
                mesh = variants[(int)(hash % (uint)variants.Length)];

                // A quarter turn picked from other bits, so one look lands in different corners.
                float turn = ((hash >> 8) & 3u) * 90f;
                decorFilter.transform.localRotation = Quaternion.Euler(0f, 0f, turn);
            }

            decorFilter.sharedMesh = mesh;
            hasLook = mesh != null;
            decorRenderer.enabled = hasLook;
        }

        public void SetCleared(bool cleared)
        {
            decorRenderer.enabled = hasLook && !cleared;
        }
    }
}
