using ASTeams.SingleLine.Core;
using UnityEditor;
using UnityEngine;

// UnityEngine ships its own Grid, so the board geometry type has to be named explicitly.
using Grid = ASTeams.SingleLine.Core.Grid;

namespace ASTeams.SingleLine.Editor
{
    /// <summary>
    /// Draws a level the way the player will see it, with the solution laid over it.
    ///
    /// Seeing the board is the whole point of a preview: a level that scores oddly or
    /// sits in a strange chapter is usually obvious at a glance and invisible in a table.
    /// </summary>
    public static class LevelBoardPreview
    {
        private const float MaxCellSize = 26f;
        private const float CellGap = 2f;

        private static readonly Color HoleColor = new Color(0f, 0f, 0f, 0f);

        public static void Draw(Rect area, LevelData level, bool showSolutionOrder)
        {
            if (level == null || level.ActiveCellCount == 0)
            {
                return;
            }

            Grid grid = level.Grid;
            float cell = Mathf.Min(
                MaxCellSize,
                Mathf.Min(
                    (area.width - (grid.Width - 1) * CellGap) / grid.Width,
                    (area.height - (grid.Height - 1) * CellGap) / grid.Height));

            if (cell <= 1f)
            {
                return;
            }

            float boardWidth = grid.Width * cell + (grid.Width - 1) * CellGap;
            float boardHeight = grid.Height * cell + (grid.Height - 1) * CellGap;
            float originX = area.x + (area.width - boardWidth) * 0.5f;
            float originY = area.y;

            var order = BuildOrderLookup(level);
            GUIStyle labelStyle = BuildLabelStyle(cell);

            for (int row = 0; row < grid.Height; row++)
            {
                for (int column = 0; column < grid.Width; column++)
                {
                    int index = row * grid.Width + column;
                    var rect = new Rect(
                        originX + column * (cell + CellGap),
                        originY + row * (cell + CellGap),
                        cell,
                        cell);

                    if (!level.IsActive(index))
                    {
                        EditorGUI.DrawRect(rect, HoleColor);
                        continue;
                    }

                    bool isStart = level.HasFixedStart && level.FixedStart == index;
                    EditorGUI.DrawRect(rect, isStart ? StartColor() : CellColor());

                    if (showSolutionOrder && order != null && order.TryGetValue(index, out int step))
                    {
                        GUI.Label(rect, step.ToString(), labelStyle);
                    }
                }
            }
        }

        /// <summary>Height the board will take at the given width, so callers can lay out around it.</summary>
        public static float MeasureHeight(LevelData level, float availableWidth)
        {
            if (level == null || level.ActiveCellCount == 0)
            {
                return 0f;
            }

            Grid grid = level.Grid;
            float cell = Mathf.Min(MaxCellSize, (availableWidth - (grid.Width - 1) * CellGap) / grid.Width);
            return grid.Height * cell + (grid.Height - 1) * CellGap;
        }

        private static System.Collections.Generic.Dictionary<int, int> BuildOrderLookup(LevelData level)
        {
            if (!level.HasSolution)
            {
                return null;
            }

            var order = new System.Collections.Generic.Dictionary<int, int>(level.Solution.Count);

            for (int i = 0; i < level.Solution.Count; i++)
            {
                order[level.Solution[i]] = i + 1;
            }

            return order;
        }

        private static GUIStyle BuildLabelStyle(float cell)
        {
            return new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Max(7, Mathf.RoundToInt(cell * 0.42f)),
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : new Color(0.1f, 0.12f, 0.15f) }
            };
        }

        private static Color CellColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.29f, 0.33f, 0.38f)
                : new Color(0.74f, 0.78f, 0.83f);
        }

        private static Color StartColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.79f, 0.30f, 0.33f)
                : new Color(0.76f, 0.21f, 0.24f);
        }
    }
}
