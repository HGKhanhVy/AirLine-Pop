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

        [Tooltip("How long the newest segment takes to reach the cell it just entered. GDD 10 asks for 80 to 120 ms.")]
        [SerializeField, Min(0.01f)] private float connectDuration = 0.1f;

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

        private void Reset()
        {
            line = GetComponent<LineRenderer>();
        }

        private void Awake()
        {
            if (line == null)
            {
                line = GetComponent<LineRenderer>();
            }

            line.useWorldSpace = true;
            line.numCornerVertices = 4;
            line.numCapVertices = 4;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.View;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.sortingOrder = BoardSortingOrder.Path;
            line.positionCount = 0;
        }

        public void Rebuild(PathSession session)
        {
            if (session == null || session.Length == 0)
            {
                line.positionCount = 0;
                return;
            }

            if (points == null || points.Length < session.Length)
            {
                points = new Vector3[session.Level.ActiveCellCount];
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
        }

        private void Update()
        {
            if (growIndex < 0)
            {
                return;
            }

            grownFor += Time.deltaTime;
            float t = Mathf.Clamp01(grownFor / connectDuration);

            // Fast out, so the segment leaves the previous cell immediately and only the
            // last of the travel is soft. Easing the start instead makes it feel sticky.
            float eased = 1f - (1f - t) * (1f - t);
            line.SetPosition(growIndex, Vector3.LerpUnclamped(growFrom, growTo, eased));

            if (t >= 1f)
            {
                growIndex = -1;
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
            growIndex = -1;
            previousHead = LevelData.NoCell;
            previousLength = 0;
        }
    }
}
