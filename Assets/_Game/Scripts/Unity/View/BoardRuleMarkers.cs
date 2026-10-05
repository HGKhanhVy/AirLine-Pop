using ASTeams.SingleLine.Core;
using DG.Tweening;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Draws a level's extra conditions on its squares. The runway is a tile of its own laid
    /// over the landing square, riding on that square so it matches it exactly; the path
    /// lands on it like any other square. Each square with wind gets an arrow turned the
    /// way it blows, which fades once the path covers it and comes back if the move is
    /// undone. The markers are made once in the prefab and reused.
    /// </summary>
    public sealed class BoardRuleMarkers : MonoBehaviour
    {
        [SerializeField] private BoardView board;
        [SerializeField] private SpriteRenderer runway;
        [SerializeField] private SpriteRenderer[] gusts = new SpriteRenderer[0];

        [Tooltip("Marker size as a share of a square.")]
        [SerializeField, Range(0.3f, 1.2f)] private float size = 0.9f;

        [Tooltip("How much a wind arrow swells as it breathes.")]
        [SerializeField, Range(1f, 1.3f)] private float gustPulse = 1.1f;
        [SerializeField, Min(0.2f)] private float gustSeconds = 0.7f;
        [SerializeField, Min(0.02f)] private float fadeSeconds = 0.15f;

        private int[] gustCells;
        private bool[] isGustShown;

        private void Awake()
        {
            gustCells = new int[gusts.Length];
            isGustShown = new bool[gusts.Length];
        }

        private void OnEnable()
        {
            GameplayEvents.OnLevelLoaded += HandleLevelLoaded;
            GameplayEvents.OnProgressChanged += HandleProgressChanged;
            Show(board.Level);
        }

        private void OnDisable()
        {
            GameplayEvents.OnLevelLoaded -= HandleLevelLoaded;
            GameplayEvents.OnProgressChanged -= HandleProgressChanged;

            // No reparenting here: this also runs while the scene is torn down.
            HideAll(false);
        }

        private void HandleLevelLoaded(int levelNumber, string levelId, int difficulty, int totalCells)
        {
            Show(board.Level);
        }

        private void HandleProgressChanged(int visitedCells, int totalCells)
        {
            for (int i = 0; i < gusts.Length; i++)
            {
                if (gustCells[i] != LevelData.NoCell)
                {
                    isGustShown[i] = Follow(gusts[i], gustCells[i], isGustShown[i]);
                }
            }
        }

        /// <summary>Fades a mark out when the path covers its square and back in when it is uncovered.</summary>
        private bool Follow(SpriteRenderer marker, int cell, bool isShown)
        {
            CellView view = board.GetCellView(cell);
            bool wanted = view == null || !view.IsVisited;

            if (wanted == isShown)
            {
                return isShown;
            }

            marker.DOKill();
            marker.DOFade(wanted ? 1f : 0f, fadeSeconds);
            return wanted;
        }

        private void Show(LevelData level)
        {
            HideAll(true);

            if (level == null)
            {
                return;
            }

            float scale = board.CellSize * size;

            // At night the runway lights and the wind socks stay lit over the dark.
            int order = level.IsNight ? BoardSortingOrder.NightLit : BoardSortingOrder.RuleMarker;
            runway.sortingOrder = order;

            for (int i = 0; i < gusts.Length; i++)
            {
                gusts[i].sortingOrder = order;
            }

            if (level.HasFixedEnd)
            {
                ShowRunway(level.FixedEnd);
            }

            int count = Mathf.Min(gusts.Length, level.Wind.Count);

            for (int i = 0; i < count; i++)
            {
                WindCell gust = level.Wind[i];
                gustCells[i] = gust.Cell;
                isGustShown[i] = true;
                Place(gusts[i], gust.Cell, -90f * (int)gust.Direction, scale);

                // Out of step with each other, so a row of gusts does not breathe in unison.
                gusts[i].transform.DOScale(scale * gustPulse, gustSeconds).SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo).Goto(gustSeconds * 0.37f * i, true);
            }
        }

        private void ShowRunway(int cell)
        {
            CellView view = board.GetCellView(cell);

            if (view == null)
            {
                return;
            }

            Transform tile = runway.transform;
            tile.SetParent(view.FaceRoot, false);
            tile.localPosition = Vector3.zero;
            tile.localRotation = Quaternion.identity;
            tile.localScale = Vector3.one;
            runway.gameObject.SetActive(true);
        }

        private void Place(SpriteRenderer marker, int cell, float degrees, float scale)
        {
            Transform mark = marker.transform;
            mark.position = board.GetCellWorldPosition(cell);
            mark.rotation = Quaternion.Euler(0f, 0f, degrees);
            mark.localScale = Vector3.one * scale;
            marker.color = Color.white;
            marker.gameObject.SetActive(true);
        }

        private void HideAll(bool bringRunwayHome)
        {
            // Back home from whichever square it rode on, so it never shows on a pooled square reused elsewhere.
            if (bringRunwayHome && runway.transform.parent != transform)
            {
                runway.transform.SetParent(transform, false);
            }

            runway.gameObject.SetActive(false);

            for (int i = 0; i < gusts.Length; i++)
            {
                gustCells[i] = LevelData.NoCell;
                gusts[i].DOKill();
                gusts[i].transform.DOKill();
                gusts[i].gameObject.SetActive(false);
            }
        }

#if UNITY_EDITOR
        public void EditorLink(BoardView linkedBoard, SpriteRenderer linkedRunway, SpriteRenderer[] linkedGusts)
        {
            board = linkedBoard;
            runway = linkedRunway;
            gusts = linkedGusts;
        }
#endif
    }
}
