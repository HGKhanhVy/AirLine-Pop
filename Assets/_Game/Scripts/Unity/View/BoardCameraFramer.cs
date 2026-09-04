using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Sizes and positions the orthographic camera so the board fills the space between
    /// the HUD bars.
    ///
    /// Boards run from three cells to ninety and from square to tall, and phone screens
    /// vary as much again, so a fixed camera size would either crop the big boards or
    /// leave the small ones lost in the middle of the screen. GDD 9.2 asks for the board
    /// to take 55 to 70 percent of the usable height, which on a portrait phone means
    /// sizing to height first and only falling back to width when the board is wide.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class BoardCameraFramer : MonoBehaviour
    {
        [SerializeField] private ThemeSO theme;
        [SerializeField] private Camera boardCamera;

        [Header("Space taken by the HUD")]
        [Tooltip("Share of the screen height reserved at the top, where the level readout sits.")]
        [SerializeField, Range(0f, 0.4f)] private float topReserve = 0.14f;

        [Tooltip("Share of the screen height reserved at the bottom, where the buttons sit.")]
        [SerializeField, Range(0f, 0.4f)] private float bottomReserve = 0.16f;

        private Vector2 framedSize;

        private void Reset()
        {
            boardCamera = GetComponent<Camera>();
        }

        private void Awake()
        {
            if (boardCamera == null)
            {
                boardCamera = GetComponent<Camera>();
            }

            boardCamera.orthographic = true;
            boardCamera.backgroundColor = theme.Background;
        }

        public void Frame(Vector2 boardWorldSize)
        {
            framedSize = boardWorldSize;
            Apply();
        }

        /// <summary>Re-frames when the window changes shape, on a rotate or an editor resize.</summary>
        private void OnRectTransformDimensionsChange()
        {
            Apply();
        }

        private void Apply()
        {
            if (framedSize.x <= 0f || framedSize.y <= 0f || boardCamera == null)
            {
                return;
            }

            // GDD 9.2 measures the board against the usable screen, not against the gap
            // left between the bars, so the wanted share is of the whole height and the
            // band only caps it.
            float usable = Mathf.Max(0.1f, 1f - topReserve - bottomReserve);
            float targetShare = Mathf.Min(Mathf.Clamp(theme.BoardHeightFraction, 0.1f, 1f), usable * 0.98f);

            // Half height the camera needs for the board to take the wanted share of the
            // screen, and the half height it needs for the board plus margins to fit
            // across. Whichever is larger wins, so the board never overflows sideways.
            float fromHeight = framedSize.y / (2f * targetShare);
            float margin = theme.BoardMargin * theme.CellPitch;
            float fromWidth = (framedSize.x * 0.5f + margin) / Mathf.Max(0.01f, boardCamera.aspect);

            // A four by four board asked to fill the width ends up with cells twice the
            // size of a ten by ten one, which reads as a different game rather than an
            // easier level. Capping how large one cell may appear keeps a cell roughly the
            // same size everywhere; on big boards the cap never binds.
            float maxCell = Mathf.Clamp(theme.MaxCellWidthFraction, 0.05f, 1f);
            float fromCellCap = theme.CellPitch / (2f * Mathf.Max(0.01f, boardCamera.aspect) * maxCell);

            float size = Mathf.Max(Mathf.Max(fromHeight, fromWidth), fromCellCap);
            boardCamera.orthographicSize = size;

            // Centre the board between the reserved bars rather than on the screen, or a
            // tall HUD would push it off centre.
            float offset = (bottomReserve - topReserve) * size;
            Vector3 position = boardCamera.transform.position;
            boardCamera.transform.position = new Vector3(position.x, -offset, position.z);
        }
    }
}
