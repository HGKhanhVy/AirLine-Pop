using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ASTeams.SingleLine.Core;
using ASTeams.SingleLine.Data;
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

        [Tooltip("Steps a single hint gives away. GDD 7.1 allows 1 to 3.")]
        [SerializeField, Range(1, 3)] private int hintSteps = 3;

        [Tooltip("Total worker search time before falling back to the stored solution.")]
        [SerializeField, Min(1)] private int hintTimeBudgetMilliseconds = 100;

        private PathSession session;
        private IHapticService haptics;
        private Coroutine rewind;
        private WaitForSeconds rewindWait;
        private IHintService hintService;
        private CancellationTokenSource hintRequest;

        // Reused so a move never allocates; a board holds at most ninety cells.
        private readonly List<int> scratchCells = new List<int>(96);

        public bool IsHintPending => hintRequest != null;
        public event Action<HintResult> OnHintReady;
        public event Action OnHintChanged;

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

        private void Awake()
        {
            rewindWait = new WaitForSeconds(rewindStepDelay);
        }

        /// <summary>
        /// Hands in the buzzer. Injected rather than looked up, and optional: with nothing
        /// supplied the game simply plays without haptics.
        /// </summary>
        public void SetHaptics(IHapticService service)
        {
            haptics = service;
        }

        /// <summary>
        /// Turns board input on or off without touching the path, so a screen on top can
        /// hold the board still while it is open.
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            if (!enabled)
            {
                CancelHint();
            }

            boardInput.AcceptsInput = enabled && rewind == null && State != PathState.Won;
        }

        public void SetHintService(IHintService service)
        {
            CancelHint();
            hintService = service;
        }

        public async Task<bool> ShowHintAsync()
        {
            if (session == null || IsHintPending || hintService == null || rewind != null ||
                session.State == PathState.Won || session.Length == 0 || !isActiveAndEnabled)
            {
                return false;
            }

            var request = new CancellationTokenSource();
            hintRequest = request;
            OnHintChanged?.Invoke();
            GameplayEvents.RaiseHintStarted();

            try
            {
                HintResult result = await hintService.FindHintAsync(session.Level, CollectPath(),
                    hintSteps, hintNodeBudget, hintTimeBudgetMilliseconds, request.Token);
                if (request.IsCancellationRequested || !ReferenceEquals(hintRequest, request))
                {
                    return false;
                }

                boardFeedback.PlayHint(result.Steps);
                OnHintReady?.Invoke(result);
                GameplayEvents.RaiseHintResolved(result);
                return !result.NeedsRestart && result.Steps.Count > 0;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception exception)
            {
                if (!request.IsCancellationRequested)
                {
                    Debug.LogException(exception, this);
                    OnHintReady?.Invoke(HintResult.RestartRequired);
                    GameplayEvents.RaiseHintResolved(HintResult.RestartRequired);
                }

                return false;
            }
            finally
            {
                if (ReferenceEquals(hintRequest, request))
                {
                    hintRequest = null;
                    OnHintChanged?.Invoke();
                }

                request.Dispose();
            }
        }

        private void CancelHint()
        {
            if (hintRequest != null)
            {
                hintRequest.Cancel();
                hintRequest = null;
            }

            boardFeedback.StopHint();
            OnHintReady?.Invoke(null);
            OnHintChanged?.Invoke();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                CancelHint();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                CancelHint();
            }
        }
        private void OnEnable()
        {
            boardInput.OnCellEntered += HandleCellEntered;
        }

        private void OnDisable()
        {
            boardInput.OnCellEntered -= HandleCellEntered;
            CancelHint();
            CancelRewind();
        }

        public void Load(LevelData level)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            CancelHint();
            if (session != null)
            {
                session.OnStateChanged -= HandleSessionStateChanged;
            }

            PathState previous = State;

            CancelRewind();
            session = new PathSession(level);
            session.OnStateChanged += HandleSessionStateChanged;

            // Each level has its own hue in the reference game, so the palette is settled
            // before a single square is placed. The number comes from the id rather than
            // from a second argument, so nothing calling Load has to know about colour.
            if (!CampaignLevelAddress.TryGetLevelNumber(level.Id, out int levelNumber))
            {
                levelNumber = 1;
            }

            boardView.SetPalette(theme.GetPalette(levelNumber));

            boardFeedback.Stop();
            boardView.Build(level);
            cameraFramer.Frame(boardView.WorldSize);
            pathView.Prepare(level.ActiveCellCount);

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
            CancelHint();
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

            CancelHint();
            if (rewindStepDelay <= 0f)
            {
                boardFeedback.Stop();
                while (session.Length > 1)
                {
                    session.Undo();
                }

                Refresh();
                return;
            }

            rewind = StartCoroutine(RewindRoutine());
            GameplayEvents.RaiseRewindChanged(true);
        }

        private IEnumerator RewindRoutine()
        {
            boardInput.AcceptsInput = false;
            boardFeedback.Stop();

            while (session.Length > 1 && session.Undo())
            {
                Refresh();

                if (rewindStepDelay > 0f)
                {
                    yield return rewindWait;
                }
            }

            rewind = null;
            boardInput.AcceptsInput = true;
            GameplayEvents.RaiseRewindChanged(false);
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
            GameplayEvents.RaiseRewindChanged(false);
        }

        private void HandleCellEntered(int cell)
        {
            if (session == null || rewind != null)
            {
                return;
            }

            // The start gesture shares the same rewind as the Restart button.
            if (session.Length > 1 && cell == session.GetCell(0))
            {
                Restart();
                return;
            }

            // Rejection is normal and must change nothing, which is what MOV-06 asks for.
            // It still has to be felt: the nudge lands on the head rather than the cell
            // that was refused, because that cell is often a hole with nothing drawn.
            // Any move answers the hint, so it stops pointing at a route the player has
            // already left.
            CancelHint();

            MoveResult result = session.Move(cell);

            if (result == MoveResult.Rejected)
            {
                boardFeedback.PlayInvalid(session.Head);
                GameplayEvents.RaiseInvalidMove(session.Head);
                return;
            }

            Refresh();

            // Only a step forward is a new connection. Backtracking recolours cells too,
            // and a square swelling as the player deletes it would read as the opposite of
            // what just happened.
            if (result != MoveResult.Backtracked)
            {
                boardView.PopCell(session.Head);

                // GDD 10 gives haptics to entering a cell and to winning, and to nothing
                // else. Backtracking and refused moves answer with sound and motion only.
                haptics?.Play(HapticStrength.Light);
            }
        }

        private void HandleSessionStateChanged(PathState previous, PathState current)
        {
            if (current == PathState.Won)
            {
                boardInput.AcceptsInput = false;
                boardView.RevealGoal(session.Head);
                boardFeedback.PlayWin(CollectPath());
                haptics?.Play(HapticStrength.Medium);
            }
            else if (current == PathState.Stuck)
            {
                // Being stuck is not a loss: nothing is reset, the view just says so and
                // the player undoes their way out.
                boardFeedback.PlayStuck();
            }
            else if (previous == PathState.Stuck || previous == PathState.Won)
            {
                boardFeedback.Stop();
            }

            OnStateChanged?.Invoke(previous, current);
            GameplayEvents.RaiseStateChanged(previous, current);
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
            boardView.SetStartCue(session.Length <= 1 && session.State != PathState.Won);

            // The start square already has a cue of its own, so the head only takes over
            // once the player has drawn at least one step and the board is still theirs.
            boardView.SetHeadCue(head, session.Length > 1 && session.State == PathState.Drawing);
            OnPathChanged?.Invoke();
            GameplayEvents.RaiseProgressChanged(session.Length, session.Level.ActiveCellCount);
        }

        private Color ColorForState(PathState state)
        {
            BoardPalette palette = boardView.Palette;

            switch (state)
            {
                case PathState.Won:
                    return palette.Won;

                case PathState.Stuck:
                    return palette.Stuck;

                default:
                    return palette.Path;
            }
        }
    }
}
