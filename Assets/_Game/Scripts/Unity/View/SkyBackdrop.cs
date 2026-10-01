using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Keeps the flat sky behind the board filling the whole view.
    ///
    /// The camera moves closer or further for every board, so the sky is re-fitted each
    /// time it is framed rather than sized once. The stage is scaled as a whole, so the
    /// clouds drifting on it keep the same size on screen whatever the board.
    /// </summary>
    public sealed class SkyBackdrop : MonoBehaviour
    {
        [SerializeField] private BoardCameraFramer framer;
        [SerializeField] private Camera boardCamera;
        [SerializeField] private SpriteRenderer gradient;

        [Tooltip("How far behind the board the sky sits, in world units.")]
        [SerializeField, Min(0.1f)] private float depth = 2f;

        [Tooltip("View height, in world units, the cloud layout was authored for. At this height the stage has a scale of one.")]
        [SerializeField, Min(1f)] private float referenceViewHeight = 16f;

        [Tooltip("Extra coverage past the screen edges, so no seam shows on odd aspects.")]
        [SerializeField, Min(1f)] private float overscan = 1.08f;

        private void Awake()
        {
            gradient.sortingOrder = BoardSortingOrder.Landscape;
        }

        private void OnEnable()
        {
            framer.OnFramed += HandleFramed;

            if (framer.HasFramed)
            {
                HandleFramed(framer.Framing);
            }
        }

        private void OnDisable()
        {
            framer.OnFramed -= HandleFramed;
        }

        private void HandleFramed(BoardFraming framing)
        {
            float distance = framing.Distance + depth;
            float viewHeight = 2f * distance * Mathf.Tan(boardCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * overscan;
            float viewWidth = viewHeight * boardCamera.aspect;

            transform.position = framing.LookAtOnPlane(depth);

            float stageScale = viewHeight / referenceViewHeight;
            transform.localScale = new Vector3(stageScale, stageScale, 1f);

            Vector2 spriteSize = gradient.sprite.bounds.size;
            gradient.transform.localScale = new Vector3(
                viewWidth / stageScale / spriteSize.x,
                viewHeight / stageScale / spriteSize.y,
                1f);
        }
    }
}
