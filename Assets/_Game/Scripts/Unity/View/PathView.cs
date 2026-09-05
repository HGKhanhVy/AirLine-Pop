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

        private Vector3[] points;

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
        }

        public void SetColor(Color color)
        {
            line.startColor = color;
            line.endColor = color;
        }

        public void Clear()
        {
            line.positionCount = 0;
        }
    }
}
