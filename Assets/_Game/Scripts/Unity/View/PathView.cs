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
        private Color sparkColor = Color.white;
        private float sparkLeft;

        private void Reset()
        {
            line = GetComponent<LineRenderer>();
        }

        private void Awake()
        {
            line.useWorldSpace = true;
            line.numCornerVertices = 4;
            line.numCapVertices = 4;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.View;
            line.sharedMaterial = sharedMaterial;
            line.sortingOrder = BoardSortingOrder.Connector;
            line.positionCount = 0;

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
            if (session == null || session.Length == 0)
            {
                line.positionCount = 0;
                return;
            }

            for (int step = 0; step < session.Length; step++)
            {
                Vector3 position = boardView.GetCellWorldPosition(session.GetCell(step));
                position.z = -0.1f;
                points[step] = position;
            }

            line.startWidth = theme.PathWidth;
            line.endWidth = theme.PathWidth;
            line.positionCount = session.Length;
            line.SetPositions(points);

            StartGrowth(session);
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
            line.SetPosition(growIndex, growFrom);

            if (spark != null)
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
                line.SetPosition(growIndex, Vector3.LerpUnclamped(growFrom, growTo, eased));

                if (t >= 1f)
                {
                    growIndex = -1;
                }
            }

            AdvanceSpark();
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

        public void SetColor(Color color)
        {
            line.startColor = color;
            line.endColor = color;
        }

        public void Clear()
        {
            line.positionCount = 0;
            HideSpark();
            growIndex = -1;
            previousHead = LevelData.NoCell;
            previousLength = 0;
        }
    }
}
