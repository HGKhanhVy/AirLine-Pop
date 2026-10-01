using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Tilts a perspective camera over the board and pulls it back until the board fills
    /// the space between the HUD bars.
    ///
    /// Boards run from three cells to ninety and from square to tall, and phone screens
    /// vary as much again, so a fixed camera distance would either crop the big boards or
    /// leave the small ones lost in the middle of the screen. GDD 9.2 asks for the board
    /// to take 55 to 70 percent of the usable height, which on a portrait phone means
    /// sizing to height first and only falling back to width when the board is wide.
    ///
    /// The tilt is what makes the board read as blocks standing on the ground rather than
    /// a flat grid: the near walls show and the far rows recede. Perspective turns the
    /// board into a trapezoid on screen, so the fit is solved numerically on its projected
    /// corners rather than in closed form. It only runs when a level or the screen changes.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class BoardCameraFramer : MonoBehaviour
    {
        private const int DistanceSearchSteps = 24;
        private const int CentringPasses = 3;
        private const float MinDistance = 1f;
        private const float MaxDistance = 400f;
        private const float NearClip = 1f;

        [SerializeField] private ThemeSO theme;
        [SerializeField] private Camera boardCamera;

        [Header("View")]
        [Tooltip("Degrees the camera leans back from looking straight down on the board.")]
        [SerializeField, Range(0f, 60f)] private float tilt = 34f;

        [Tooltip("Vertical field of view. Narrow keeps far rows close in size to near ones.")]
        [SerializeField, Range(10f, 60f)] private float fieldOfView = 30f;

        [Tooltip("How deep the block walls reach below the board surface, so the near walls are fitted too.")]
        [SerializeField, Min(0f)] private float blockDepth = 0.4f;

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

        /// <summary>Raised whenever the camera is re-framed, for scenery that follows the view.</summary>
        public event System.Action<BoardFraming> OnFramed;

        public bool HasFramed { get; private set; }

        public BoardFraming Framing { get; private set; }

        private void Reset()
        {
            boardCamera = GetComponent<Camera>();
        }

        private void Awake()
        {
            boardCamera.orthographic = false;
            boardCamera.fieldOfView = fieldOfView;
            boardCamera.backgroundColor = theme.Background;

            // Nothing comes near the lens, and the world far below needs the depth precision.
            boardCamera.nearClipPlane = NearClip;
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
            float bandCentre = bottom + usable * 0.5f;

            var view = new ViewProjection(Quaternion.Euler(-tilt, 0f, 0f), fieldOfView, boardCamera.aspect);
            var limits = new FitLimits(targetShare, MarginShare(), Mathf.Clamp(theme.MaxCellWidthFraction, 0.05f, 1f));

            Vector3 lookAt = Vector3.zero;
            float distance = MaxDistance;

            // Moving the look-at point changes the perspective slightly, so the distance
            // and the centring are refined together; a few passes settle under a pixel.
            for (int pass = 0; pass < CentringPasses; pass++)
            {
                distance = SolveDistance(view, lookAt, limits);
                BoardBounds bounds = Measure(view, lookAt, distance);
                float perUnit = bounds.Height / Mathf.Max(0.01f, framedSize.y);
                lookAt.y += (bounds.CentreY - bandCentre) / Mathf.Max(0.001f, perUnit);
            }

            boardCamera.orthographic = false;
            boardCamera.fieldOfView = fieldOfView;
            boardCamera.transform.SetPositionAndRotation(view.CameraPosition(lookAt, distance), view.Rotation);

            Framing = new BoardFraming(lookAt, view.Forward, distance, framedSize);
            HasFramed = true;
            OnFramed?.Invoke(Framing);
        }

        private float MarginShare()
        {
            float margin = theme.BoardMargin * theme.CellPitch;
            return margin / Mathf.Max(0.01f, framedSize.x + 2f * margin);
        }

        /// <summary>The closest distance at which the board meets every limit. Each limit only loosens with distance.</summary>
        private float SolveDistance(in ViewProjection view, Vector3 lookAt, in FitLimits limits)
        {
            float near = MinDistance;
            float far = MaxDistance;

            for (int step = 0; step < DistanceSearchSteps; step++)
            {
                float middle = Mathf.Sqrt(near * far);

                if (Fits(Measure(view, lookAt, middle), limits))
                {
                    far = middle;
                }
                else
                {
                    near = middle;
                }
            }

            return far;
        }

        private bool Fits(in BoardBounds bounds, in FitLimits limits)
        {
            // A four by four board asked to fill the width ends up with cells twice the
            // size of a ten by ten one, which reads as a different game rather than an
            // easier level. Capping how large the nearest cell may appear keeps a cell
            // roughly the same size everywhere; on big boards the cap never binds.
            float cellShare = bounds.NearEdgeWidth * theme.CellPitch / Mathf.Max(0.01f, framedSize.x);

            return bounds.Height <= limits.HeightShare
                && bounds.MinX >= limits.SideMargin
                && bounds.MaxX <= 1f - limits.SideMargin
                && cellShare <= limits.MaxCellShare;
        }

        /// <summary>
        /// Viewport extent of the board seen from <paramref name="distance"/>. The top of it
        /// is the far edge of the block tops; the bottom is the foot of the near wall.
        /// </summary>
        private BoardBounds Measure(in ViewProjection view, Vector3 lookAt, float distance)
        {
            Vector3 cameraPosition = view.CameraPosition(lookAt, distance);
            Vector2 half = framedSize * 0.5f;

            Vector2 farLeft = view.ToViewport(new Vector3(-half.x, half.y, 0f), cameraPosition);
            Vector2 farRight = view.ToViewport(new Vector3(half.x, half.y, 0f), cameraPosition);
            Vector2 nearLeft = view.ToViewport(new Vector3(-half.x, -half.y, 0f), cameraPosition);
            Vector2 nearRight = view.ToViewport(new Vector3(half.x, -half.y, 0f), cameraPosition);
            Vector2 nearFoot = view.ToViewport(new Vector3(0f, -half.y, blockDepth), cameraPosition);

            return new BoardBounds(
                minX: Mathf.Min(nearLeft.x, farLeft.x),
                maxX: Mathf.Max(nearRight.x, farRight.x),
                minY: Mathf.Min(nearFoot.y, nearLeft.y),
                maxY: Mathf.Max(farLeft.y, farRight.y),
                nearEdgeWidth: nearRight.x - nearLeft.x);
        }

        /// <summary>A pinhole projection that can be evaluated without moving the camera.</summary>
        private readonly struct ViewProjection
        {
            private readonly Quaternion inverse;
            private readonly float tanHalfHeight;
            private readonly float tanHalfWidth;

            public ViewProjection(Quaternion rotation, float fieldOfView, float aspect)
            {
                Rotation = rotation;
                Forward = rotation * Vector3.forward;
                inverse = Quaternion.Inverse(rotation);
                tanHalfHeight = Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
                tanHalfWidth = tanHalfHeight * Mathf.Max(0.01f, aspect);
            }

            public Quaternion Rotation { get; }

            public Vector3 Forward { get; }

            public Vector3 CameraPosition(Vector3 lookAt, float distance)
            {
                return lookAt - Forward * distance;
            }

            public Vector2 ToViewport(Vector3 world, Vector3 cameraPosition)
            {
                Vector3 local = inverse * (world - cameraPosition);
                float depth = Mathf.Max(0.001f, local.z);
                return new Vector2(
                    0.5f + 0.5f * local.x / (depth * tanHalfWidth),
                    0.5f + 0.5f * local.y / (depth * tanHalfHeight));
            }
        }

        private readonly struct FitLimits
        {
            public FitLimits(float heightShare, float sideMargin, float maxCellShare)
            {
                HeightShare = heightShare;
                SideMargin = sideMargin;
                MaxCellShare = maxCellShare;
            }

            public float HeightShare { get; }

            public float SideMargin { get; }

            public float MaxCellShare { get; }
        }

        private readonly struct BoardBounds
        {
            public BoardBounds(float minX, float maxX, float minY, float maxY, float nearEdgeWidth)
            {
                MinX = minX;
                MaxX = maxX;
                MinY = minY;
                MaxY = maxY;
                NearEdgeWidth = nearEdgeWidth;
            }

            public float MinX { get; }

            public float MaxX { get; }

            public float MinY { get; }

            public float MaxY { get; }

            public float NearEdgeWidth { get; }

            public float Height => MaxY - MinY;

            public float CentreY => (MinY + MaxY) * 0.5f;
        }
    }
}
