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

        [Tooltip("The wingman's mirrored line, drawn only on formation flights.")]
        [SerializeField] private PathView wingmanPath;

        [Tooltip("The dark of night flights.")]
        [SerializeField] private NightFlightView nightView;

        [Tooltip("Seconds between cells while the path rewinds. Small enough to read as one motion.")]
        [SerializeField, Min(0f)] private float rewindStepDelay = 0.025f;

        [Tooltip("Seconds a gust holds the plane on its square before blowing it on to the next.")]
        [SerializeField, Min(0f)] private float gustDelay = 0.12f;

        [Tooltip("Lets the player step back by dragging along the line onto the square behind " +
                 "the head. On: there is no Undo button, drawing back over the route erases it.")]
        [SerializeField] private bool allowsDragBacktrack = true;

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
        private Coroutine gust;
        private WaitForSeconds gustWait;

        // The windy square the plane was just blown off; the finger is usually still over it.
        private int blownFrom = LevelData.NoCell;
        private IHintService hintService;
        private CancellationTokenSource hintRequest;
        private bool isNightHeldForIntro;

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

        /// <summary>Squares covered so far, the wingman's included on a formation flight.</summary>
        public int Progress => session == null ? 0 : session.CoveredCount;

        public int Target => session == null ? 0 : session.Level.CoverageTarget;

        /// <summary>The square the path ends on, where the airplane stands.</summary>
        public int Head => session == null ? LevelData.NoCell : session.Head;

        /// <summary>Where a formation flight's wingman stands, or <see cref="LevelData.NoCell"/>.</summary>
        public int WingHead => session == null ? LevelData.NoCell : session.WingHead;

        public bool IsVisited(int cell)
        {
            return session != null && session.IsVisited(cell);
        }

        /// <summary>True while the path is unwinding itself back to the start.</summary>
        public bool IsRewinding => rewind != null;

        /// <summary>True while a night flight is still showing its route, before the player may fly.</summary>
        private bool IsNightPreviewing => nightView != null && nightView.IsPreviewing;

        private void Awake()
        {
            rewindWait = new WaitForSeconds(rewindStepDelay);
            gustWait = new WaitForSeconds(gustDelay);
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

            boardInput.AcceptsInput = enabled && rewind == null && gust == null && State != PathState.Won && !IsNightPreviewing;
        }

        public void SetHintService(IHintService service)
        {
            CancelHint();
            hintService = service;
        }

        public async Task<bool> ShowHintAsync()
        {
            if (session == null || IsHintPending || hintService == null || rewind != null || gust != null || IsNightPreviewing ||
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

                // The hint always starts where the plane stands: onward when the route can
                // still be finished from here, back along the route when it cannot.
                result = AnchorToHead(result);
                boardFeedback.PlayHint(result.Steps, session.Head);
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

        /// <summary>
        /// A hint that needs undos first is turned into the way back: the squares from the head
        /// back to where the route can still be finished, which the guide flies in reverse. A
        /// windy square is never a place to stop, since stepping back onto it takes its gust
        /// back too, so the way back runs on past it.
        /// </summary>
        private HintResult AnchorToHead(HintResult result)
        {
            if (result.NeedsRestart || result.BacktrackCount <= 0)
            {
                return result;
            }

            int keep = Math.Max(1, session.Length - result.BacktrackCount);

            while (keep > 1 && IsWindy(session.GetCell(keep - 1)))
            {
                keep--;
            }

            var wayBack = new int[session.Length - keep];

            for (int i = 0; i < wayBack.Length; i++)
            {
                wayBack[i] = session.GetCell(session.Length - 2 - i);
            }

            return new HintResult(wayBack.Length, wayBack);
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
            GameplayEvents.OnRuleIntroChanged += HandleRuleIntroChanged;
        }

        private void OnDisable()
        {
            boardInput.OnCellEntered -= HandleCellEntered;
            GameplayEvents.OnRuleIntroChanged -= HandleRuleIntroChanged;
            CancelHint();
            CancelRewind();
            CancelGust();
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
            CancelGust();
            isNightHeldForIntro = false;
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
            pathView.Prepare(level.PathLength);
            wingmanPath?.Prepare(level.PathLength);
            nightView?.Prepare();

            // Hold the starting cell from the outset, the same way a reset leaves it. A
            // board with nothing drawn has no head to move from, so a hint would have
            // nothing to answer and the first touch would be a special case.
            if (level.HasFixedStart)
            {
                session.Move(level.FixedStart);
            }

            Refresh();

            boardInput.AcceptsInput = true;
            ShowNightRoute();
            OnStateChanged?.Invoke(previous, State);
        }

        /// <summary>
        /// On a night flight, shows the route once before the dark falls, with the board held
        /// still until then. Played again after a restart, so a forgotten turn can be relearnt.
        /// </summary>
        private void ShowNightRoute()
        {
            if (nightView == null || !session.Level.IsNight)
            {
                return;
            }

            if (isNightHeldForIntro)
            {
                return;
            }

            boardInput.AcceptsInput = false;
            CampaignLevelAddress.TryGetLevelNumber(session.Level.Id, out int levelNumber);
            nightView.PlayPreview(session.Level.Solution, levelNumber, HandleNightFallen);
        }

        /// <summary>
        /// A card explaining a rule opens just after the level loads, over a night route that
        /// has started showing; the route waits and starts again from its first square once
        /// the card is closed, so none of it is missed.
        /// </summary>
        private void HandleRuleIntroChanged(bool isShown)
        {
            if (session == null || nightView == null || !session.Level.IsNight)
            {
                return;
            }

            if (isShown && session.Length <= 1)
            {
                isNightHeldForIntro = true;
                boardInput.AcceptsInput = false;
                nightView.Prepare();
            }
            else if (!isShown && isNightHeldForIntro)
            {
                isNightHeldForIntro = false;
                ShowNightRoute();
            }
        }

        private void HandleNightFallen()
        {
            boardInput.AcceptsInput = rewind == null && State != PathState.Won;
        }

        /// <summary>Steps back exactly one cell, as GDD 3.3 specifies.</summary>
        public bool Undo()
        {
            CancelHint();
            if (session == null || rewind != null || gust != null || !session.Undo())
            {
                return false;
            }

            UnwindGusts();
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
            CancelGust();
            if (rewindStepDelay <= 0f)
            {
                boardFeedback.Stop();
                while (session.Length > 1)
                {
                    session.Undo();
                }

                Refresh();
                ShowNightRoute();
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
            ShowNightRoute();
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
            if (session == null || rewind != null || gust != null)
            {
                return;
            }

            // Tapping the start shares the Restart button's rewind. It has to be a tap: a
            // drag that crosses the start would otherwise wipe the whole route. Dragging
            // back onto it from the second square is an ordinary one-step backtrack.
            bool isTap = boardInput.IsReportingPress;

            // In formation only the player's own half is drawn on; the wingman follows by itself.
            LevelData level = session.Level;

            if (level.IsOnWingSide(cell))
            {
                return;
            }

            // After a gust the finger is still over the windy square, and moving on reaches the
            // square the plane was blown to; neither is a move of its own.
            if (blownFrom != LevelData.NoCell)
            {
                bool isLeftover = cell == blownFrom || cell == session.Head;

                if (cell != blownFrom)
                {
                    blownFrom = LevelData.NoCell;
                }

                if (isLeftover)
                {
                    return;
                }
            }

            if (isTap && session.Length > 1 && cell == session.GetCell(0))
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

            MoveResult result = IsRefusedBacktrack(cell) ? MoveResult.Rejected : session.Move(cell);

            if (result == MoveResult.Rejected)
            {
                boardFeedback.PlayInvalid(session.Head);
                GameplayEvents.RaiseInvalidMove(session.Head);
                return;
            }

            if (result == MoveResult.Backtracked)
            {
                UnwindGusts();
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
                StartGust(cell);
            }
        }

        /// <summary>
        /// A plane that flies onto a windy square is blown on to the next square the way the
        /// wind points, and on again if that square is windy too. The core rule already allows
        /// only that one way out, so the gust simply takes the step for the player. A gust that
        /// meets a square it cannot enter leaves the plane where it is, stuck.
        /// </summary>
        private void StartGust(int enteredCell)
        {
            if (session.State != PathState.Drawing || !IsWindy(session.Head))
            {
                return;
            }

            gust = StartCoroutine(GustRoutine(enteredCell));
        }

        private IEnumerator GustRoutine(int enteredCell)
        {
            boardInput.AcceptsInput = false;

            while (session.State == PathState.Drawing && session.Level.TryGetWind(session.Head, out Direction wind) &&
                   session.Level.Grid.TryGetNeighbor(session.Head, wind, out int downwind))
            {
                if (gustDelay > 0f)
                {
                    yield return gustWait;
                }

                if (session.Move(downwind) == MoveResult.Rejected)
                {
                    break;
                }

                blownFrom = enteredCell;
                Refresh();
                boardView.PopCell(session.Head);
                haptics?.Play(HapticStrength.Light);
            }

            gust = null;
            boardInput.AcceptsInput = rewind == null && session.State != PathState.Won && !IsNightPreviewing;
        }

        private void CancelGust()
        {
            if (gust != null)
            {
                StopCoroutine(gust);
                gust = null;
                boardInput.AcceptsInput = true;
            }

            blownFrom = LevelData.NoCell;
        }

        /// <summary>
        /// Stepping back off a square the wind blew the plane onto takes the gust back too: the
        /// plane never rests on a windy square, so it returns to where it flew in from.
        /// </summary>
        private void UnwindGusts()
        {
            while (session.Length > 1 && IsWindy(session.Head))
            {
                session.Undo();
            }

            blownFrom = LevelData.NoCell;
        }

        private bool IsWindy(int cell)
        {
            return session.Level.TryGetWind(cell, out _);
        }

        private void HandleSessionStateChanged(PathState previous, PathState current)
        {
            if (current == PathState.Won)
            {
                boardInput.AcceptsInput = false;
                boardView.RevealGoal(session.Head);
                boardFeedback.PlayWin(CollectPath());
                // The line leaves each square as that square is at its fullest.
                pathView.RetractForWin(
                    boardFeedback.WinStepSeconds, boardFeedback.WinPulseSeconds * 0.5f);
                wingmanPath?.RetractForWin(boardFeedback.WinStepSeconds, boardFeedback.WinPulseSeconds * 0.5f);
                nightView?.Dawn();
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

        /// <summary>
        /// A drag onto the square just behind the head when stepping back by dragging is
        /// switched off: the session would step back for it, so it is turned away first.
        /// </summary>
        private bool IsRefusedBacktrack(int cell)
        {
            return !allowsDragBacktrack
                && session.Length >= 2
                && cell == session.GetCell(session.Length - 2);
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

            RepaintVisuals();
            OnPathChanged?.Invoke();
            GameplayEvents.RaiseProgressChanged(session.CoveredCount, session.Level.CoverageTarget);
        }

        /// <summary>
        /// Puts the current model back on screen without announcing a move. A skin change
        /// mid level calls this: the board changes colour, the path the player drew stays
        /// exactly where it is (GDD EC-10).
        /// </summary>
        public void RepaintVisuals()
        {
            if (session == null)
            {
                return;
            }

            LevelData level = session.Level;
            int head = session.Head;
            int wingHead = session.WingHead;
            int cellCount = level.Grid.CellCount;

            for (int index = 0; index < cellCount; index++)
            {
                if (level.IsActive(index))
                {
                    boardView.SetCellVisited(index, session.IsVisited(index), index == head || index == wingHead);
                }
            }

            for (int i = 0; i < level.Marks.Count; i++)
            {
                int marked = level.Marks[i].Cell;
                boardView.SetCellPasses(marked, session.VisitCount(marked));
            }

            Color lineColor = ColorForState(session.State);
            pathView.Rebuild(session);
            pathView.SetColor(lineColor);

            if (wingmanPath != null)
            {
                wingmanPath.Rebuild(session);
                wingmanPath.SetColor(lineColor);
            }
            boardView.SetStartCue(session.Length <= 1 && session.State != PathState.Won);

            // The start square already has a cue of its own, so the head only takes over
            // once the player has drawn at least one step and the board is still theirs.
            boardView.SetHeadCue(head, session.Length > 1 && session.State == PathState.Drawing);
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
