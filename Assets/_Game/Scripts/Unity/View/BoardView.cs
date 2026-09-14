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
        [SerializeField] private CellView cellPrefab;
        [SerializeField, Min(1)] private int poolCapacity = 100;

        [Tooltip("Plays the board's particles. Leave empty to run the board without them.")]
        [SerializeField] private PooledEffectPlayer effectPlayer;

        [Tooltip("Quiet time before the square the player stopped on nudges itself. The " +
                 "start square waits the same three seconds before its own idle.")]
        [SerializeField, Min(0f)] private float headCueDelay = 3f;

        [Tooltip("Gap between two nudges. One idle clip is four seconds long.")]
        [SerializeField, Min(0.5f)] private float headIdleInterval = 4f;

        [Tooltip("How long a square takes to take its new colour. GDD 10 asks for 80 to 120 ms.")]
        [SerializeField, Min(0f)] private float fillDuration = 0.1f;

        private CellViewPool pool;
        private IEffectPlayer effects;
        private BoardPalette palette;
        private int headCell = LevelData.NoCell;
        private float headCueAt;

        // Only the handful of squares mid-fill are ticked, so a ninety cell board costs
        // nothing while the player is not drawing.
        private readonly List<CellView> filling = new List<CellView>(100);
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

            effects = effectPlayer;
            palette = theme.GetPalette(1);
            pool = new CellViewPool(new PrefabCellViewFactory(cellPrefab, cellRoot));
            pool.Prewarm(poolCapacity);
        }

        private void Update()
        {
            for (int i = filling.Count - 1; i >= 0; i--)
            {
                if (!filling[i].Advance(Time.deltaTime))
                {
                    filling.RemoveAt(i);
                }
            }

            AdvanceHeadCue();
        }

        /// <summary>
        /// Nudges the square the player stopped on, now and again rather than on a beat.
        /// One float compare per frame when there is nothing to do.
        /// </summary>
        private void AdvanceHeadCue()
        {
            if (headCell == LevelData.NoCell || Time.time < headCueAt)
            {
                return;
            }

            CellView view = GetView(headCell);
            headCueAt = Time.time + Mathf.Max(headIdleInterval, CellView.IdleNudgeLength);

            if (view != null)
            {
                view.PlayIdleNudge();
            }
        }

        /// <summary>
        /// Puts the star on the square that finished the level. Called once, on the win,
        /// so the mark reads as the reward for a complete path rather than a hint that was
        /// there all along.
        /// </summary>
        public void RevealGoal(int cellIndex)
        {
            CellView view = GetView(cellIndex);

            if (view != null)
            {
                view.PlayGoalReveal();
            }
        }

        /// <summary>
        /// Names the square the path stops on. The wait before the first nudge means a
        /// player still drawing never sees it, only one who has actually stopped.
        /// </summary>
        public void SetHeadCue(int cellIndex, bool isEnabled)
        {
            int wanted = isEnabled ? cellIndex : LevelData.NoCell;

            if (headCell == wanted)
            {
                return;
            }

            CellView previous = GetView(headCell);

            if (previous != null)
            {
                previous.StopHeadCue();
            }

            headCell = wanted;
            headCueAt = Time.time + headCueDelay;
        }

        /// <summary>
        /// The colours this level is drawn with. Set before <see cref="Build"/>, since
        /// every square takes its colour as it is placed.
        /// </summary>
        public void SetPalette(BoardPalette newPalette)
        {
            palette = newPalette;
        }

        /// <summary>The colours in use, so the path and the feedback match the squares.</summary>
        public BoardPalette Palette => palette;

        public void Build(LevelData newLevel)
        {
            level = newLevel;
            pool.ReleaseAll();
            filling.Clear();
            effects?.StopAll();
            headCell = LevelData.NoCell;

            if (level == null)
            {
                return;
            }

            Grid grid = level.Grid;

            if (!TryMeasureActiveCells(out int minRow, out int maxRow, out int minColumn, out int maxColumn))
            {
                WorldSize = Vector2.zero;
                return;
            }

            // GDD 5 measures the board by the cells that exist, not by the grid they sit
            // in. A level whose holes bunch into one corner would otherwise be pushed off
            // centre by empty space nobody can see.
            int usedWidth = maxColumn - minColumn + 1;
            int usedHeight = maxRow - minRow + 1;

            WorldSize = new Vector2(
                usedWidth * theme.CellSize + (usedWidth - 1) * theme.CellSpacing,
                usedHeight * theme.CellSize + (usedHeight - 1) * theme.CellSpacing);

            // Placing the middle of that span on the origin. Row 0 is the top row in level
            // data, so y runs downward and the sign flips.
            float pitch = theme.CellPitch;
            originLocal = new Vector3(
                -(minColumn + maxColumn) * 0.5f * pitch,
                (minRow + maxRow) * 0.5f * pitch,
                0f);

            if (cellsByIndex == null || cellsByIndex.Length < grid.CellCount)
            {
                cellsByIndex = new CellView[grid.CellCount];
            }

            for (int i = 0; i < cellsByIndex.Length; i++)
            {
                cellsByIndex[i] = null;
            }

            Sprite sprite = theme.CellSprite;

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

            Color target = ColorFor(index, visited, isHead);

            // The controller refreshes every square on every move, and all but two of them
            // keep the colour they already had. Ticking those for nothing would put a
            // ninety cell board through the fill list on each step.
            if (view.Color == target)
            {
                return;
            }

            view.FillTo(target, fillDuration);
            Track(view);
        }

        /// <summary>
        /// Presses a square down and lets it bounce back, for the moment the path reaches
        /// it. Driven from the controller rather than from the colour change, because
        /// stepping back onto a cell also recolours it and an undo should not read as a
        /// new connection. The feel itself is tuned on the cell prefab.
        /// </summary>
        public void PopCell(int index)
        {
            CellView view = GetView(index);

            if (view == null)
            {
                return;
            }

            view.PlayConnect();
            effects?.Play(GameplayEffect.CellConnected, GetCellWorldPosition(index), view.Color);
            Track(view);
        }
        public void SetStartCue(bool isVisible)
        {
            if (level == null || !level.HasFixedStart)
            {
                return;
            }

            CellView view = GetView(level.FixedStart);

            if (view == null)
            {
                return;
            }

            if (isVisible)
            {
                view.ShowStartCue(theme.StartDot, theme.StartHalo);
                effects?.Play(GameplayEffect.StartCue, GetCellWorldPosition(level.FixedStart), palette.Start);
                Track(view);
            }
            else
            {
                view.HideStartCue();
            }
        }

        private void Track(CellView view)
        {
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

        /// <summary>The rows and columns actually occupied, or false for an empty board.</summary>
        private bool TryMeasureActiveCells(out int minRow, out int maxRow, out int minColumn, out int maxColumn)
        {
            Grid grid = level.Grid;
            minRow = int.MaxValue;
            maxRow = int.MinValue;
            minColumn = int.MaxValue;
            maxColumn = int.MinValue;

            for (int index = 0; index < grid.CellCount; index++)
            {
                if (!level.IsActive(index))
                {
                    continue;
                }

                int row = index / grid.Width;
                int column = index % grid.Width;

                minRow = Mathf.Min(minRow, row);
                maxRow = Mathf.Max(maxRow, row);
                minColumn = Mathf.Min(minColumn, column);
                maxColumn = Mathf.Max(maxColumn, column);
            }

            return maxRow >= minRow;
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

        /// <summary>
        /// The start square is asked about first, and keeps its own colour for the whole
        /// level. The reference game marks it that way, and the board opens with the head
        /// already sitting on it: letting the head win would paint the opening square in
        /// the pale head tint instead of the saturated one the start is supposed to have.
        /// </summary>
        private Color ColorFor(int index, bool visited, bool head)
        {
            if (level.HasFixedStart && level.FixedStart == index)
            {
                return palette.Start;
            }

            if (head)
            {
                return palette.Head;
            }

            return visited ? palette.Visited : palette.Cell;
        }
    }
}
