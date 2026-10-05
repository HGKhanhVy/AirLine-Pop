using ASTeams.SingleLine.Core;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The line the player has drawn so far.
    ///
    /// Redrawn from the session rather than tracked incrementally: a path is at most
    /// ninety points, rebuilding it costs nothing, and a single source of truth cannot
    /// drift out of step with the model after an undo or a restart.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class PathView : MonoBehaviour
    {
        [SerializeField] private ThemeSO theme;
        [SerializeField] private LineRenderer line;
        [SerializeField] private BoardView boardView;
        [SerializeField] private Material sharedMaterial;

        [Tooltip("Draws the path as a flight route: the material's texture is repeated along " +
                 "the line instead of stretched over it. One texture tile spans as many path " +
                 "widths as the texture is wider than tall, so its dashes keep their shape.")]
        [SerializeField] private bool isDashed;

        [Tooltip("Lines drawn under the path through the same points, bottom first, such as the " +
                 "runway the path is the centre line of. Their width, colour and material are " +
                 "their own; only the points and visibility follow the path.")]
        [SerializeField] private LineRenderer[] underlays = System.Array.Empty<LineRenderer>();

        [Tooltip("Draw the line back into the squares during the win wave. Off keeps the " +
                 "finished route on the board.")]
        [SerializeField] private bool retractsOnWin = true;

        [Tooltip("How long the newest segment takes to reach the cell it just entered. GDD 10 asks for 80 to 120 ms.")]
        [SerializeField, Min(0.01f)] private float connectDuration = 0.1f;

        [Header("Spark")]
        [Tooltip("Rides the tip of the newest segment. Leave empty to draw the line without one.")]
        [SerializeField] private SpriteRenderer spark;

        [SerializeField, Min(0f)] private float sparkSize = 0.85f;

        [Tooltip("How much lighter than the path the spark reads. Near white separates it from the line.")]
        [SerializeField, Range(0f, 1f)] private float sparkLift = 0.85f;

        [Tooltip("Extra time the spark lingers on the cell after landing, so the arrival is seen.")]
        [SerializeField, Min(0f)] private float sparkAfterglow = 0.11f;

        [Tooltip("Draws the mirror of the route, for the wingman of a formation flight. " +
                 "On any other flight a mirrored line stays empty.")]
        [SerializeField] private bool isMirrored;

        private Vector3[] points;

        // The newest segment grows instead of appearing whole, so the eye can see which
        // two cells just joined. Without it the line jumps and the squares read as
        // separate things that happen to be lit.
        private Vector3 growFrom;
        private Vector3 growTo;
        private float grownFor;
        private int growIndex = -1;
        private int previousHead = LevelData.NoCell;
        private int previousLength;

        // On a win the line is drawn back into the squares behind the celebration wave,
        // so the finished board ends up showing only its squares. The controller repaints
        // the line after the win is raised, so the retraction has to outlast that repaint.
        private bool isHiddenForWin;
        private float retractElapsed = -1f;
        private float retractStepSeconds;
        private float retractDelay;
        private Color sparkColor = Color.white;
        private float sparkLeft;

        private void Reset()
        {
            line = GetComponent<LineRenderer>();
        }

        private void Awake()
        {
            line.useWorldSpace = true;

            // Ten a corner and ten a cap, the same as the Line the reference block ships
            // with. Four left visible facets on every turn of the path.
            line.numCornerVertices = 10;

            // A dash already carries its own rounded ends; a round cap would add a blob of
            // solid line at both ends of the route.
            line.numCapVertices = isDashed ? 0 : 10;
            line.textureMode = isDashed ? LineTextureMode.Tile : LineTextureMode.Stretch;
            // Flat on the block tops rather than turned to face the tilted camera, or half
            // the line would sink into the blocks.
            line.alignment = LineAlignment.TransformZ;
            line.sharedMaterial = sharedMaterial;
            line.sortingOrder = BoardSortingOrder.Connector;
            line.positionCount = 0;

            for (int i = 0; i < underlays.Length; i++)
            {
                underlays[i].useWorldSpace = true;
                underlays[i].alignment = LineAlignment.TransformZ;
                underlays[i].sortingOrder = BoardSortingOrder.Underlay + i;
                underlays[i].positionCount = 0;
            }

            if (spark != null)
            {
                // Above the squares, unlike the connector: the spark is the light at the
                // player's fingertip and has to stay visible as it crosses a square.
                spark.sortingOrder = BoardSortingOrder.Spark;
                spark.transform.localScale = Vector3.one * sparkSize;
                spark.enabled = false;
            }
        }

        public void Prepare(int cellCount)
        {
            if (points == null || points.Length < cellCount)
            {
                points = new Vector3[cellCount];
            }

            Clear();
        }

        public void Rebuild(PathSession session)
        {
            // A board that leaves the won state without being cleared, such as a Restart
            // pressed during the celebration, gets its line back.
            if (IsWinHidePending && (session == null || session.State != PathState.Won))
            {
                ShowLine();
            }

            if (session == null || session.Length == 0 || (isMirrored && !session.Level.IsFormation))
            {
                SetPointCount(0);
                return;
            }

            for (int step = 0; step < session.Length; step++)
            {
                int cell = session.GetCell(step);
                Vector3 position = boardView.GetCellWorldPosition(isMirrored ? session.Level.MirrorOf(cell) : cell);
                position.z = -0.1f;
                points[step] = position;
            }

            line.startWidth = theme.PathWidth;
            line.endWidth = theme.PathWidth;
            ApplyDashScale();
            SetPointCount(session.Length);
            SetAllPoints();

            StartGrowth(session);
        }

        /// <summary>The moving front of the path, where the newest segment has grown to so far.</summary>
        public bool HasTip => line.enabled && line.positionCount > 0;

        public Vector3 TipPosition => line.GetPosition(line.positionCount - 1);

        /// <summary>
        /// Direction of the newest segment, taken from the cells it joins rather than from
        /// the growing tip, so it is already correct on the frame the segment starts.
        /// </summary>
        public bool TryGetTipDirection(out Vector2 direction)
        {
            int count = line.positionCount;

            if (count < 2)
            {
                direction = Vector2.zero;
                return false;
            }

            direction = points[count - 1] - points[count - 2];
            return direction.sqrMagnitude > 0f;
        }

        private void ApplyDashScale()
        {
            if (!isDashed || sharedMaterial == null || sharedMaterial.mainTexture == null)
            {
                return;
            }

            Texture dash = sharedMaterial.mainTexture;
            float tileLength = theme.PathWidth * dash.width / dash.height;
            line.textureScale = new Vector2(1f / tileLength, 1f);
        }

        /// <summary>
        /// Starts the newest segment from the cell before it, so it extends into place.
        ///
        /// Only a step forward grows. Undo and the rewind walk backwards, and a segment
        /// crawling after a path the player is deleting reads as lag rather than polish.
        /// </summary>
        private void StartGrowth(PathSession session)
        {
            int head = session.GetCell(session.Length - 1);
            bool stepped = session.Length == previousLength + 1 && previousHead != LevelData.NoCell;

            previousLength = session.Length;
            previousHead = head;

            if (!stepped || session.Length < 2)
            {
                growIndex = -1;
                return;
            }

            growIndex = session.Length - 1;
            growFrom = points[growIndex - 1];
            growTo = points[growIndex];
            grownFor = 0f;
            SetPoint(growIndex, growFrom);

            if (spark != null && !isHiddenForWin)
            {
                sparkColor = Color.Lerp(line.startColor, Color.white, sparkLift);
                sparkLeft = connectDuration + sparkAfterglow;
                spark.transform.position = growFrom;
                spark.transform.localScale = Vector3.one * sparkSize;
                spark.color = sparkColor;
                spark.enabled = true;
            }
        }

        private void Update()
        {
            if (growIndex >= 0)
            {
                grownFor += Time.deltaTime;
                float t = Mathf.Clamp01(grownFor / connectDuration);

                // Fast out, so the segment leaves the previous cell immediately and only
                // the last of the travel is soft. Easing the start makes it feel sticky.
                float eased = 1f - (1f - t) * (1f - t);
                SetPoint(growIndex, Vector3.LerpUnclamped(growFrom, growTo, eased));

                if (t >= 1f)
                {
                    growIndex = -1;
                }
            }

            AdvanceSpark();

            AdvanceRetract();
        }

        /// <summary>
        /// Pulls the tail of the line along the path at the wave's pace. Every point the
        /// tail has passed is folded onto the tail, so the line shortens from its start
        /// and the round cap slides with it rather than the line being cut.
        /// </summary>
        private void AdvanceRetract()
        {
            if (retractElapsed < 0f)
            {
                return;
            }

            retractElapsed += Time.deltaTime;
            int last = line.positionCount - 1;
            float travelled = (retractElapsed - retractDelay) / retractStepSeconds;

            if (travelled <= 0f)
            {
                return;
            }

            if (last < 1 || travelled >= last)
            {
                HideNow();
                return;
            }

            int passed = (int)travelled;
            Vector3 tail = Vector3.Lerp(points[passed], points[passed + 1], travelled - passed);

            for (int step = 0; step <= passed; step++)
            {
                SetPoint(step, tail);
            }
        }

        /// <summary>
        /// The spark rides the tip of the segment, then stays on the square it reached
        /// and swells out as it fades. Cutting it off the instant it arrives left nothing
        /// to see, which is the whole point of the pulse.
        /// </summary>
        private void AdvanceSpark()
        {
            if (spark == null || sparkLeft <= 0f)
            {
                return;
            }

            sparkLeft -= Time.deltaTime;

            if (sparkLeft <= 0f)
            {
                HideSpark();
                return;
            }

            float travel = Mathf.Clamp01(grownFor / connectDuration);
            float eased = 1f - (1f - travel) * (1f - travel);
            spark.transform.position = Vector3.LerpUnclamped(growFrom, growTo, eased);

            if (travel < 1f)
            {
                // Full brightness on the way over.
                spark.transform.localScale = Vector3.one * sparkSize;
                spark.color = sparkColor;
                return;
            }

            float glow = sparkAfterglow <= 0f ? 0f : Mathf.Clamp01(sparkLeft / sparkAfterglow);
            Color colour = sparkColor;
            colour.a = glow;
            spark.color = colour;
            spark.transform.localScale = Vector3.one * sparkSize * (1f + (1f - glow) * 0.6f);
        }

        private void HideSpark()
        {
            sparkLeft = 0f;

            if (spark != null)
            {
                spark.enabled = false;
            }
        }

        /// <summary>Swaps the line's dashes for another set, such as a theme's runway marks.</summary>
        public void SetDashMaterial(Material material)
        {
            sharedMaterial = material;
            line.sharedMaterial = material;
            ApplyDashScale();
        }

        public void SetColor(Color color)
        {
            line.startColor = color;
            line.endColor = color;
        }

        /// <summary>
        /// Draws the finished line back into its squares in step with the win wave: the
        /// tail leaves the first square after <paramref name="delay"/> and moves on one
        /// square every <paramref name="stepSeconds"/>, so each stretch of line goes as
        /// its square swells. Once the tail reaches the last square the line is gone, and
        /// it comes back on the next <see cref="Clear"/>.
        /// </summary>
        public void RetractForWin(float stepSeconds, float delay)
        {
            if (!retractsOnWin)
            {
                return;
            }

            if (stepSeconds <= 0f)
            {
                HideNow();
                return;
            }

            retractStepSeconds = stepSeconds;
            retractDelay = delay;
            retractElapsed = 0f;
        }

        private void HideNow()
        {
            retractElapsed = -1f;
            isHiddenForWin = true;
            SetLinesEnabled(false);
            HideSpark();
        }

        private bool IsWinHidePending => isHiddenForWin || retractElapsed >= 0f;

        private void ShowLine()
        {
            retractElapsed = -1f;
            isHiddenForWin = false;
            SetLinesEnabled(true);
        }

        private void SetPointCount(int count)
        {
            line.positionCount = count;

            for (int i = 0; i < underlays.Length; i++)
            {
                underlays[i].positionCount = count;
            }
        }

        private void SetAllPoints()
        {
            line.SetPositions(points);

            for (int i = 0; i < underlays.Length; i++)
            {
                underlays[i].SetPositions(points);
            }
        }

        private void SetPoint(int index, Vector3 position)
        {
            line.SetPosition(index, position);

            for (int i = 0; i < underlays.Length; i++)
            {
                underlays[i].SetPosition(index, position);
            }
        }

        private void SetLinesEnabled(bool isEnabled)
        {
            line.enabled = isEnabled;

            for (int i = 0; i < underlays.Length; i++)
            {
                underlays[i].enabled = isEnabled;
            }
        }

        public void Clear()
        {
            if (IsWinHidePending)
            {
                ShowLine();
            }

            SetPointCount(0);
            HideSpark();
            growIndex = -1;
            previousHead = LevelData.NoCell;
            previousLength = 0;
        }
    }
}
