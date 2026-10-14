using AirlinePop.SingleLine.Core;
using DG.Tweening;
using UnityEngine;

namespace AirlinePop.SingleLine.Unity
{
    /// <summary>
    /// Draws the clouds, beacons and overpasses of a level on its squares. A mark fades once
    /// the path has used the square up and comes back if the move is undone; an overpass
    /// crossed once dims half way, since it still owes its second crossing. The markers are
    /// made once in the prefab and reused level after level.
    /// </summary>
    public sealed class CellMarkMarkers : MonoBehaviour
    {
        [SerializeField] private BoardView board;
        [SerializeField] private SpriteRenderer[] markers = new SpriteRenderer[0];
        [SerializeField] private Sprite cloudSprite;
        [SerializeField] private Sprite beaconSprite;
        [SerializeField] private Sprite bridgeSprite;

        [Tooltip("Marker size as a share of a square.")]
        [SerializeField, Range(0.3f, 1.2f)] private float size = 0.86f;

        [Tooltip("How visible an overpass stays once crossed one way, before its second crossing.")]
        [SerializeField, Range(0f, 1f)] private float halfCrossedAlpha = 0.55f;

        [SerializeField, Min(0.02f)] private float fadeSeconds = 0.15f;

        private int[] markerCells;
        private int[] capacities;
        private float[] shownAlpha;
        private int markerCount;

        private void Awake()
        {
            markerCells = new int[markers.Length];
            capacities = new int[markers.Length];
            shownAlpha = new float[markers.Length];
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
            HideAll();
        }

        private void HandleLevelLoaded(int levelNumber, string levelId, int difficulty, int totalCells)
        {
            Show(board.Level);
        }

        private void HandleProgressChanged(int visitedCells, int totalCells)
        {
            for (int i = 0; i < markerCount; i++)
            {
                Follow(i);
            }
        }

        private void Follow(int index)
        {
            CellView view = board.GetCellView(markerCells[index]);
            int passes = view == null ? 0 : view.PassCount;
            int left = capacities[index] - passes;
            float wanted = left <= 0 ? 0f : left < capacities[index] ? halfCrossedAlpha : 1f;

            if (Mathf.Approximately(wanted, shownAlpha[index]))
            {
                return;
            }

            shownAlpha[index] = wanted;
            markers[index].DOKill();
            markers[index].DOFade(wanted, fadeSeconds);
        }

        private void Show(LevelData level)
        {
            HideAll();

            if (level == null)
            {
                return;
            }

            float scale = board.CellSize * size;

            // At night the marks stay lit over the dark, like the runway and the wind.
            int order = level.IsNight ? BoardSortingOrder.NightLit : BoardSortingOrder.RuleMarker;
            markerCount = Mathf.Min(markers.Length, level.Marks.Count);

            for (int i = 0; i < markerCount; i++)
            {
                CellMark mark = level.Marks[i];
                SpriteRenderer marker = markers[i];
                marker.sprite = SpriteFor(mark.Kind);
                marker.sortingOrder = order;
                marker.color = Color.white;

                Transform markTransform = marker.transform;
                markTransform.position = board.GetCellWorldPosition(mark.Cell);
                markTransform.localScale = Vector3.one * scale;
                marker.gameObject.SetActive(true);

                markerCells[i] = mark.Cell;
                capacities[i] = level.CapacityOf(mark.Cell);
                shownAlpha[i] = 1f;
            }
        }

        private Sprite SpriteFor(CellKind kind)
        {
            switch (kind)
            {
                case CellKind.Turn:
                    return beaconSprite;
                case CellKind.Bridge:
                    return bridgeSprite;
                default:
                    return cloudSprite;
            }
        }

        private void HideAll()
        {
            for (int i = 0; i < markers.Length; i++)
            {
                markers[i].DOKill();
                markers[i].gameObject.SetActive(false);
            }

            markerCount = 0;
        }

#if UNITY_EDITOR
        public void EditorLink(BoardView linkedBoard, SpriteRenderer[] linkedMarkers, Sprite cloud, Sprite beacon, Sprite bridge)
        {
            board = linkedBoard;
            markers = linkedMarkers;
            cloudSprite = cloud;
            beaconSprite = beacon;
            bridgeSprite = bridge;
        }
#endif
    }
}
