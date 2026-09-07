using System.Collections.Generic;
using ASTeams.SingleLine.Core;
using UnityEngine;

// UnityEngine ships its own Grid, so the board geometry type has to be named explicitly.
using Grid = ASTeams.SingleLine.Core.Grid;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Draws a board and answers where things are on it.
    ///
    /// Pointer positions are turned into cell indices by arithmetic rather than by a
    /// physics raycast: a board is a regular lattice, so the maths is exact, costs
    /// nothing per frame and needs no colliders on ninety squares.
    /// </summary>
    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField] private ThemeSO theme;
        [SerializeField] private Transform cellRoot;

        [Tooltip("How long a square takes to take its new colour. GDD 10 asks for 80 to 120 ms.")]
        [SerializeField, Min(0f)] private float fillDuration = 0.1f;

        private CellViewPool pool;

        // Only the handful of squares mid-fill are ticked, so a ninety cell board costs
        // nothing while the player is not drawing.
        private readonly List<CellView> filling = new List<CellView>(8);
        private LevelData level;
        private CellView[] cellsByIndex;
        private Vector3 originLocal;

        public LevelData Level => level;

        /// <summary>Board size in world units, used to frame the camera.</summary>
        public Vector2 WorldSize { get; private set; }

        /// <summary>Distance between neighbouring cell centres, in world units.</summary>
        public float CellPitch => theme.CellPitch;

        /// <summary>Squares currently drawn, in no particular order.</summary>
        public IReadOnlyList<CellView> ActiveCells => pool.Live;

        private void Awake()
        {
            if (cellRoot == null)
            {
                cellRoot = transform;
            }

            pool = new CellViewPool(cellRoot);
            pool.Prewarm(64);
        }

        private void Update()
        {
            for (int i = filling.Count - 1; i >= 0; i--)
            {
                if (!filling[i].AdvanceFill(Time.deltaTime))
                {
                    filling.RemoveAt(i);
                }
            }
        }

        public void Build(LevelData newLevel)
        {
            level = newLevel;
            pool.ReleaseAll();
            filling.Clear();

            if (level == null)
            {
                return;
            }

            Grid grid = level.Grid;

            WorldSize = new Vector2(
                grid.Width * theme.CellSize + (grid.Width - 1) * theme.CellSpacing,
                grid.Height * theme.CellSize + (grid.Height - 1) * theme.CellSpacing);

            // Row 0 is the top row in level data, so y runs downward from the top edge.
            originLocal = new Vector3(
                -WorldSize.x * 0.5f + theme.CellSize * 0.5f,
                WorldSize.y * 0.5f - theme.CellSize * 0.5f,
                0f);

            if (cellsByIndex == null || cellsByIndex.Length < grid.CellCount)
            {
                cellsByIndex = new CellView[grid.CellCount];
            }

            for (int i = 0; i < cellsByIndex.Length; i++)
            {
                cellsByIndex[i] = null;
            }

            Sprite sprite = theme.CellSprite != null ? theme.CellSprite : PlaceholderSprite.RoundedSquare;

            for (int index = 0; index < grid.CellCount; index++)
            {
                if (!level.IsActive(index))
                {
                    continue;
                }

                CellView view = pool.Acquire();
                view.Place(index, GetCellLocalPosition(index), theme.CellSize, sprite, ColorFor(index, visited: false, head: false));
                cellsByIndex[index] = view;
            }
        }

        public void SetCellVisited(int index, bool visited, bool isHead)
        {
            CellView view = GetView(index);

            if (view == null)
            {
                return;
            }

            view.FillTo(ColorFor(index, visited, isHead), fillDuration);

            if (!filling.Contains(view))
            {
                filling.Add(view);
            }
        }

        public void SetCellColor(int index, Color color)
        {
            CellView view = GetView(index);

            if (view != null)
            {
                view.SetColor(color);
            }
        }

        /// <summary>The square standing for a cell, or null when the cell is a hole.</summary>
        public CellView GetCellView(int index)
        {
            return GetView(index);
        }

        public Vector3 GetCellWorldPosition(int index)
        {
            return cellRoot.TransformPoint(GetCellLocalPosition(index));
        }

        /// <summary>
        /// Nearest cell to a world point, or false when the point is off the board or over
        /// a hole. Rounding rather than a hit test means a finger slightly off centre still
        /// counts, which is what makes dragging feel forgiving.
        /// </summary>
        public bool TryGetCellAt(Vector3 worldPosition, out int index)
        {
            index = LevelData.NoCell;

            if (level == null)
            {
                return false;
            }

            Vector3 local = cellRoot.InverseTransformPoint(worldPosition);
            float pitch = theme.CellPitch;

            int column = Mathf.RoundToInt((local.x - originLocal.x) / pitch);
            int row = Mathf.RoundToInt((originLocal.y - local.y) / pitch);

            Grid grid = level.Grid;

            if (column < 0 || column >= grid.Width || row < 0 || row >= grid.Height)
            {
                return false;
            }

            int candidate = row * grid.Width + column;

            if (!level.IsActive(candidate))
            {
                return false;
            }

            index = candidate;
            return true;
        }

        private CellView GetView(int index)
        {
            return cellsByIndex != null && index >= 0 && index < cellsByIndex.Length ? cellsByIndex[index] : null;
        }

        private Vector3 GetCellLocalPosition(int index)
        {
            Grid grid = level.Grid;
            int row = index / grid.Width;
            int column = index % grid.Width;
            float pitch = theme.CellPitch;

            return new Vector3(
                originLocal.x + column * pitch,
                originLocal.y - row * pitch,
                0f);
        }

        private Color ColorFor(int index, bool visited, bool head)
        {
            if (head)
            {
                return theme.Head;
            }

            if (visited)
            {
                return theme.Visited;
            }

            return level.HasFixedStart && level.FixedStart == index ? theme.Start : theme.Cell;
        }
    }
}
