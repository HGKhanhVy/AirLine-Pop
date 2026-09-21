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

        [Tooltip("Space kept between the board and the bars the HUD reports, as a share of the screen height.")]
        [SerializeField, Range(0f, 0.1f)] private float hudGap = 0.01f;

        private Vector2 framedSize;
        private float hudTop;
        private float hudBottom;
        private Vector2Int lastScreen;

        private void Reset()
        {
            boardCamera = GetComponent<Camera>();
        }

        private void Awake()
        {
            boardCamera.orthographic = true;
            boardCamera.backgroundColor = theme.Background;
        }

        private void OnEnable()
        {
            GameplayEvents.OnHudInsetsChanged += HandleHudInsetsChanged;
        }

        private void OnDisable()
        {
            GameplayEvents.OnHudInsetsChanged -= HandleHudInsetsChanged;
        }

        /// <summary>
        /// The reserves above are the floor; the bars the HUD actually drew win when they
        /// are taller, which is the case under a notch or on a squat tablet.
        /// </summary>
        private void HandleHudInsetsChanged(float topShare, float bottomShare)
        {
            if (Mathf.Approximately(topShare, hudTop) && Mathf.Approximately(bottomShare, hudBottom))
            {
                return;
            }

            hudTop = topShare;
            hudBottom = bottomShare;
            Apply();
        }

        public void Frame(Vector2 boardWorldSize)
        {
            framedSize = boardWorldSize;
            Apply();
        }

        /// <summary>
        /// Re-frames when the window changes shape.
        ///
        /// Unity raises no event for a resolution or orientation change, and the camera's
        /// aspect can still be the previous one on the frame a level is framed. Comparing
        /// two ints costs nothing, and it is what stops a portrait board being framed with
        /// a landscape aspect and running off both edges. The first Update always applies,
        /// because the stored size starts at zero.
        /// </summary>
        private void Update()
        {
            if (Screen.width == lastScreen.x && Screen.height == lastScreen.y)
            {
                return;
            }

            lastScreen = new Vector2Int(Screen.width, Screen.height);
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
            float top = Mathf.Max(topReserve, hudTop + hudGap);
            float bottom = Mathf.Max(bottomReserve, hudBottom + hudGap);
            float usable = Mathf.Max(0.1f, 1f - top - bottom);
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
            float offset = (bottom - top) * size;
            Vector3 position = boardCamera.transform.position;
            boardCamera.transform.position = new Vector3(position.x, -offset, position.z);
        }
    }
}
