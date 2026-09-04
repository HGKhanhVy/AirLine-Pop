using System;
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

        private PathSession session;

        /// <summary>Raised as (previous, current) so a UI layer can react without polling.</summary>
        public event Action<PathState, PathState> OnStateChanged;

        public LevelData Level => session?.Level;

        public PathState State => session == null ? PathState.Ready : session.State;

        public int Progress => session == null ? 0 : session.Length;

        public int Target => session == null ? 0 : session.Level.ActiveCellCount;

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

            session = new PathSession(level);
            session.OnStateChanged += HandleSessionStateChanged;

            boardView.Build(level);
            cameraFramer.Frame(boardView.WorldSize);
            pathView.Clear();
            Refresh();

            boardInput.AcceptsInput = true;
            OnStateChanged?.Invoke(previous, State);
        }

        public bool Undo()
        {
            if (session == null || !session.Undo())
            {
                return false;
            }

            Refresh();
            return true;
        }

        public void Restart()
        {
            if (session == null)
            {
                return;
            }

            session.Restart();
            boardInput.AcceptsInput = true;
            Refresh();
        }

        private void HandleCellEntered(int cell)
        {
            if (session == null)
            {
                return;
            }

            // Rejection is normal and must change nothing, which is what MOV-06 asks for.
            if (session.Move(cell) == MoveResult.Rejected)
            {
                return;
            }

            Refresh();
        }

        private void HandleSessionStateChanged(PathState previous, PathState current)
        {
            if (current == PathState.Won)
            {
                boardInput.AcceptsInput = false;
            }

            OnStateChanged?.Invoke(previous, current);
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
