using System;
using System.Collections;
using System.Collections.Generic;
using ASTeams.SingleLine.Core;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Sits between the input, the rules and the views.
    ///
    /// It owns the <see cref="PathSession"/> and is the only thing that calls into it.
    /// The views are told what to show and never read the model; the input reports cells
    /// and never decides whether a move is legal. Every rule stays in the tested core.
    /// </summary>
    public sealed class GameplayController : MonoBehaviour
    {
        [SerializeField] private ThemeSO theme;
        [SerializeField] private BoardView boardView;
        [SerializeField] private PathView pathView;
        [SerializeField] private BoardInput boardInput;
        [SerializeField] private BoardCameraFramer cameraFramer;
        [SerializeField] private BoardFeedback boardFeedback;

        [Tooltip("Seconds between cells while the path rewinds. Small enough to read as one motion.")]
        [SerializeField, Min(0f)] private float rewindStepDelay = 0.025f;

        [Tooltip("Search budget for a hint. Kept small so a hint never stalls a frame.")]
        [SerializeField, Min(1000)] private int hintNodeBudget = 200000;

        private PathSession session;
        private Coroutine rewind;
        private WarnsdorffSolver hintSolver;
        private int[] hintBuffer;

        // Reused so a move never allocates; a board holds at most ninety cells.
        private readonly List<int> scratchCells = new List<int>(96);

        /// <summary>Raised as (previous, current) so a UI layer can react without polling.</summary>
        public event Action<PathState, PathState> OnStateChanged;

        /// <summary>Raised whenever the drawn path changes length, for progress readouts.</summary>
        public event Action OnPathChanged;

        public LevelData Level => session?.Level;

        public PathState State => session == null ? PathState.Ready : session.State;

        public int Progress => session == null ? 0 : session.Length;

        public int Target => session == null ? 0 : session.Level.ActiveCellCount;

        /// <summary>True while the path is unwinding itself back to the start.</summary>
        public bool IsRewinding => rewind != null;

        /// <summary>
        /// Turns board input on or off without touching the path, so a screen on top can
        /// hold the board still while it is open.
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            boardInput.AcceptsInput = enabled && rewind == null && State != PathState.Won;
        }

        /// <summary>
        /// Points at the next cell of a way to finish from where the player is now.
        ///
        /// It solves the continuation rather than reading the level's stored solution,
        /// because once the player has taken their own route that stored path is usually
        /// no longer reachable, and pointing at a cell they cannot get to would be worse
        /// than saying nothing.
        /// </summary>
        public bool ShowHint()
        {
            if (session == null || rewind != null || session.State == PathState.Won || session.Length == 0)
            {
                return false;
            }

            if (hintSolver == null)
            {
                hintSolver = new WarnsdorffSolver(hintNodeBudget);
            }

            LevelData level = session.Level;

            if (hintBuffer == null || hintBuffer.Length < level.ActiveCellCount)
            {
                hintBuffer = new int[level.ActiveCellCount];
            }

            CollectPath();

            if (!hintSolver.TryContinue(level, scratchCells, hintBuffer, out int _))
            {
                return false;
            }

            boardFeedback.PlayHint(hintBuffer[session.Length]);
            return true;
        }

        private void OnEnable()
        {
            boardInput.OnCellEntered += HandleCellEntered;
        }

        private void OnDisable()
        {
            boardInput.OnCellEntered -= HandleCellEntered;
        }

        public void Load(LevelData level)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            PathState previous = State;

            CancelRewind();
            session = new PathSession(level);
            session.OnStateChanged += HandleSessionStateChanged;

            boardFeedback.Stop();
            boardView.Build(level);
            cameraFramer.Frame(boardView.WorldSize);
            pathView.Clear();

            // Hold the starting cell from the outset, the same way a reset leaves it. A
            // board with nothing drawn has no head to move from, so a hint would have
            // nothing to answer and the first touch would be a special case.
            if (level.HasFixedStart)
            {
                session.Move(level.FixedStart);
            }

            Refresh();

            boardInput.AcceptsInput = true;
            OnStateChanged?.Invoke(previous, State);
        }

        /// <summary>Steps back exactly one cell, as GDD 3.3 specifies.</summary>
        public bool Undo()
        {
            if (session == null || rewind != null || !session.Undo())
            {
                return false;
            }

            Refresh();
            return true;
        }

        /// <summary>
        /// Unwinds the whole path back to the start, one cell at a time.
        ///
        /// Restart rewinds rather than blanking the board because watching the route come
        /// apart tells the player more about the wrong turn they took than an empty board
        /// appearing would.
        /// </summary>
        public void Restart()
        {
            if (session == null || rewind != null || session.State == PathState.Won || session.Length <= 1)
            {
                return;
            }

            rewind = StartCoroutine(RewindRoutine());
        }

        private IEnumerator RewindRoutine()
        {
            boardInput.AcceptsInput = false;
            boardFeedback.Stop();

            var wait = new WaitForSecondsRealtime(rewindStepDelay);

            while (session.Length > 1 && session.Undo())
            {
                Refresh();

                if (rewindStepDelay > 0f)
                {
                    yield return wait;
                }
            }

            rewind = null;
            boardInput.AcceptsInput = true;
            Refresh();
        }

        private void CancelRewind()
        {
            if (rewind == null)
            {
                return;
            }

            StopCoroutine(rewind);
            rewind = null;
            boardInput.AcceptsInput = true;
        }

        /// <summary>
        /// Clears the path back to the starting cell at once, with no animation.
        ///
        /// This is what touching the start cell does. The finger is already there and the
        /// gesture is still live, so a rewind animation would fight the drag rather than
        /// read as feedback. It also leaves the start drawn rather than emptying the
        /// board: every level ships a fixed start, so an empty board is a dead state the
        /// player would have to tap their way out of.
        /// </summary>
        public void ResetToStart()
        {
            if (session == null)
            {
                return;
            }

            CancelRewind();
            boardFeedback.Stop();
            session.Restart();

            if (session.Level.HasFixedStart)
            {
                session.Move(session.Level.FixedStart);
            }

            boardInput.AcceptsInput = true;
            Refresh();
        }

        private void HandleCellEntered(int cell)
        {
            if (session == null || rewind != null)
            {
                return;
            }

            // Touching the cell the path began from clears it. Retracing all the way back
            // is the player saying they want to start over, and making them hold the line
            // through every cell to do it would be busywork.
            if (session.Length > 1 && session.Level.HasFixedStart && cell == session.Level.FixedStart)
            {
                ResetToStart();
                return;
            }

            // Rejection is normal and must change nothing, which is what MOV-06 asks for.
            // It still has to be felt: the nudge lands on the head rather than the cell
            // that was refused, because that cell is often a hole with nothing drawn.
            MoveResult result = session.Move(cell);

            if (result == MoveResult.Rejected)
            {
                boardFeedback.PlayInvalid(session.Head);
                return;
            }

            Refresh();

            // Only a step forward is a new connection. Backtracking recolours cells too,
            // and a square swelling as the player deletes it would read as the opposite of
            // what just happened.
            if (result != MoveResult.Backtracked)
            {
                boardView.PopCell(session.Head);
            }
        }

        private void HandleSessionStateChanged(PathState previous, PathState current)
        {
            if (current == PathState.Won)
            {
                boardInput.AcceptsInput = false;
                boardFeedback.PlayWin(CollectPath());
            }
            else if (current == PathState.Stuck)
            {
                // Being stuck is not a loss: nothing is reset, the board just points at
                // what is still uncovered so the player can undo their way out.
                boardFeedback.PlayStuck(CollectUncovered());
            }
            else if (previous == PathState.Stuck || previous == PathState.Won)
            {
                boardFeedback.Stop();
            }

            OnStateChanged?.Invoke(previous, current);
        }

        private List<int> CollectUncovered()
        {
            scratchCells.Clear();
            LevelData level = session.Level;

            for (int index = 0; index < level.Grid.CellCount; index++)
            {
                if (level.IsActive(index) && !session.IsVisited(index))
                {
                    scratchCells.Add(index);
                }
            }

            return scratchCells;
        }

        private List<int> CollectPath()
        {
            scratchCells.Clear();

            for (int step = 0; step < session.Length; step++)
            {
                scratchCells.Add(session.GetCell(step));
            }

            return scratchCells;
        }

        /// <summary>
        /// Repaints every active cell and redraws the line.
        ///
        /// This runs only on an accepted move, not every frame, and a board holds at most
        /// ninety cells, so a full pass costs less than tracking which cells changed and
        /// cannot drift out of step with the model after an undo or a restart.
        /// </summary>
        private void Refresh()
        {
            if (session == null)
            {
                return;
            }

            LevelData level = session.Level;
            int head = session.Head;
            int cellCount = level.Grid.CellCount;

            for (int index = 0; index < cellCount; index++)
            {
                if (level.IsActive(index))
                {
                    boardView.SetCellVisited(index, session.IsVisited(index), index == head);
                }
            }

            pathView.Rebuild(session);
            pathView.SetColor(ColorForState(session.State));
            OnPathChanged?.Invoke();
        }

        private Color ColorForState(PathState state)
        {
            switch (state)
            {
                case PathState.Won:
                    return theme.Won;

                case PathState.Stuck:
                    return theme.Stuck;

                default:
                    return theme.Path;
            }
        }
    }
}
