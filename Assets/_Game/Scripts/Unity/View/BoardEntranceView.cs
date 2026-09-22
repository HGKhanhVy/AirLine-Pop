using System.Collections.Generic;
using ASTeams.SingleLine.Core;
using DG.Tweening;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Opens a new level by growing its squares out of nothing in a ripple that starts at
    /// the start square, so the eye lands on the square the player has to touch first.
    ///
    /// Squares grow in place rather than travel: a fly-in crossed the screen too fast to
    /// read and looked jerky. Only the scale moves and every square ends at exactly one,
    /// the size the rest of the board assumes. The moment the player starts a path the
    /// ripple is finished at once, because a connected square animates its own scale.
    /// </summary>
    public sealed class BoardEntranceView : MonoBehaviour
    {
        [SerializeField] private BoardView boardView;

        [SerializeField, Min(0.05f)] private float popDuration = 0.32f;

        [Tooltip("Wait before the farthest square starts. Kept short so a large board does not take longer to open than a small one.")]
        [SerializeField, Min(0f)] private float maxStagger = 0.35f;

        [Tooltip("How far past full size a square swells before settling.")]
        [SerializeField, Min(0f)] private float overshoot = 1.4f;

        private readonly List<CellView> cells = new List<CellView>();
        private readonly List<float> distances = new List<float>();
        private Sequence ripple;
        private string lastLevelId;

        private void OnEnable()
        {
            GameplayEvents.OnLevelLoaded += HandleLevelLoaded;
            GameplayEvents.OnProgressChanged += HandleProgressChanged;
        }

        private void OnDisable()
        {
            GameplayEvents.OnLevelLoaded -= HandleLevelLoaded;
            GameplayEvents.OnProgressChanged -= HandleProgressChanged;
            Finish();
        }

        private void HandleLevelLoaded(int levelNumber, string levelId, int difficulty, int totalCells)
        {
            // A screen asking the board to repeat itself re-announces the same level; that
            // is not a new arrival.
            if (levelId == lastLevelId)
            {
                return;
            }

            lastLevelId = levelId;
            Play();
        }

        private void HandleProgressChanged(int visited, int total)
        {
            if (visited > 1)
            {
                Finish();
            }
        }

        private void Play()
        {
            Finish();

            LevelData level = boardView.Level;
            IReadOnlyList<CellView> active = boardView.ActiveCells;

            if (level == null || active == null || active.Count == 0)
            {
                return;
            }

            Vector3 origin = level.HasFixedStart
                ? boardView.GetCellWorldPosition(level.FixedStart)
                : boardView.transform.position;

            CollectByDistanceFrom(origin, active);

            float farthest = distances[distances.Count - 1];
            ripple = DOTween.Sequence();

            for (int i = 0; i < cells.Count; i++)
            {
                Transform square = cells[i].transform;
                square.localScale = Vector3.zero;

                float wait = farthest <= 0f ? 0f : maxStagger * distances[i] / farthest;
                ripple.Insert(wait, square.DOScale(Vector3.one, popDuration).SetEase(Ease.OutBack, overshoot));
            }
        }

        /// <summary>Orders the squares nearest-first from a point, into the reused lists.</summary>
        private void CollectByDistanceFrom(Vector3 origin, IReadOnlyList<CellView> active)
        {
            cells.Clear();
            distances.Clear();

            for (int i = 0; i < active.Count; i++)
            {
                CellView cell = active[i];
                float distance = Vector3.Distance(origin, cell.transform.parent.TransformPoint(cell.BaseLocalPosition));
                int at = distances.Count;

                while (at > 0 && distances[at - 1] > distance)
                {
                    at--;
                }

                distances.Insert(at, distance);
                cells.Insert(at, cell);
            }
        }

        /// <summary>Ends any ripple in progress with every square at full size.</summary>
        private void Finish()
        {
            if (ripple == null)
            {
                return;
            }

            ripple.Kill();
            ripple = null;

            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i] != null)
                {
                    cells[i].transform.localScale = Vector3.one;
                }
            }
        }
    }
}
