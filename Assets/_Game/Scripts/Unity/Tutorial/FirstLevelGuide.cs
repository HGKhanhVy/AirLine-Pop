using System.Collections.Generic;
using ASTeams.Base.Data;
using ASTeams.SingleLine.Core;
using DG.Tweening;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// GDD 9.3 step 1: on the first level a hand drags the solution across the real
    /// board. The player's first touch sends it away. If they then sit on the start
    /// square without moving, it comes back after a pause. Clearing the level marks the
    /// step done, so it is taught once.
    /// </summary>
    public sealed class FirstLevelGuide : MonoBehaviour
    {
        private const string StepId = "level1_drag";

        [SerializeField] private BoardView boardView;
        [SerializeField] private BoardInput boardInput;
        [SerializeField] private HandGuideView hand;

        [SerializeField, Min(1)] private int levelNumber = 1;

        [Tooltip("Wait after the level opens, so the board is settled before the hand moves.")]
        [SerializeField, Min(0f)] private float startDelay = 0.6f;

        [Tooltip("How long the player may sit on the start square before the hand returns.")]
        [SerializeField, Min(0.5f)] private float idleDelay = 3f;

        private readonly List<Vector3> path = new List<Vector3>();

        private ITutorialProgressStore progress;
        private Tween showTimer;
        private bool isActive;
        private bool isPressed;
        private int visitedCells;

        private void Awake()
        {
            progress = new ProfileTutorialProgressStore(UserProfileController.Instance);
        }

        private void OnEnable()
        {
            GameplayEvents.OnLevelLoaded += HandleLevelLoaded;
            GameplayEvents.OnProgressChanged += HandleProgressChanged;
            GameplayEvents.OnLevelWon += HandleLevelWon;
            boardInput.OnCellEntered += HandleCellEntered;
            boardInput.OnPressReleased += HandlePressReleased;
        }

        private void OnDisable()
        {
            GameplayEvents.OnLevelLoaded -= HandleLevelLoaded;
            GameplayEvents.OnProgressChanged -= HandleProgressChanged;
            GameplayEvents.OnLevelWon -= HandleLevelWon;
            boardInput.OnCellEntered -= HandleCellEntered;
            boardInput.OnPressReleased -= HandlePressReleased;
            Deactivate();
        }

        private void HandleLevelLoaded(int loadedLevel, string levelId, int difficulty, int totalCells)
        {
            Deactivate();
            visitedCells = 1;

            LevelData level = boardView.Level;
            isActive = loadedLevel == levelNumber && level != null && level.HasSolution && !progress.IsDone(StepId);

            if (isActive)
            {
                ScheduleShow(startDelay);
            }
        }

        private void HandleProgressChanged(int visited, int total)
        {
            bool isBackAtStart = visited <= 1 && visitedCells > 1;
            visitedCells = visited;

            if (!isActive)
            {
                return;
            }

            // Undo or a restart that lands back on the start square counts as starting over.
            if (isBackAtStart && !isPressed)
            {
                ScheduleShow(idleDelay);
            }
        }

        private void HandleCellEntered(int cellIndex)
        {
            isPressed = true;

            if (isActive)
            {
                CancelShow();
                hand.Stop();
            }
        }

        private void HandlePressReleased()
        {
            isPressed = false;

            if (isActive && visitedCells <= 1)
            {
                ScheduleShow(idleDelay);
            }
        }

        private void HandleLevelWon(int wonLevel, string levelId)
        {
            if (wonLevel != levelNumber || !isActive)
            {
                return;
            }

            progress.MarkDone(StepId);
            Deactivate();
        }

        private void ScheduleShow(float delay)
        {
            CancelShow();
            showTimer = DOVirtual.DelayedCall(delay, Show, false);
        }

        private void CancelShow()
        {
            showTimer?.Kill();
            showTimer = null;
        }

        private void Show()
        {
            showTimer = null;

            if (!isActive || isPressed || visitedCells > 1)
            {
                return;
            }

            IReadOnlyList<int> solution = boardView.Level.Solution;
            path.Clear();

            for (int i = 0; i < solution.Count; i++)
            {
                path.Add(boardView.GetCellWorldPosition(solution[i]));
            }

            hand.Play(path, boardView.CellPitch);
        }

        private void Deactivate()
        {
            isActive = false;
            CancelShow();
            hand.Stop();
        }
    }
}
