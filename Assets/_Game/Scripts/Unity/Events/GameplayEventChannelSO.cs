using System;
using ASTeams.SingleLine.Core;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// What the board tells the rest of the game, and what the rest of the game may ask
    /// the board for.
    ///
    /// Both directions run through this one asset so a screen can be built against it
    /// without holding a reference to any gameplay object. That is the point: two people
    /// wiring the same scene is how references get reassigned and lost in a merge.
    ///
    /// The Raise methods belong to gameplay and the Request methods to whoever is driving
    /// it; nothing here decides what a popup or a label does with either.
    /// </summary>
    [CreateAssetMenu(menuName = "Single Line/Events/Gameplay Event Channel", fileName = "GameplayEvents")]
    public sealed class GameplayEventChannelSO : ScriptableObject
    {
        public event Action<int, string, int, int> OnLevelLoaded;
        public event Action<int, int> OnProgressChanged;
        public event Action<PathState, PathState> OnStateChanged;
        public event Action<int> OnInvalidMove;

        /// <summary>Raised when a hint starts being searched for, so a button can show it is busy.</summary>
        public event Action OnHintStarted;

        public event Action<HintResult> OnHintResolved;
        public event Action<int, string> OnLevelWon;

        /// <summary>Asks the board to take back one step.</summary>
        public event Action OnUndoRequested;

        /// <summary>Asks the board to rewind to the start square.</summary>
        public event Action OnRestartRequested;

        /// <summary>Asks the board for a hint.</summary>
        public event Action OnHintRequested;

        public void RaiseLevelLoaded(int levelNumber, string levelId, int difficulty, int totalCells)
        {
            OnLevelLoaded?.Invoke(levelNumber, levelId, difficulty, totalCells);
        }

        public void RaiseProgressChanged(int visitedCells, int totalCells)
        {
            OnProgressChanged?.Invoke(visitedCells, totalCells);
        }

        public void RaiseStateChanged(PathState previous, PathState current)
        {
            OnStateChanged?.Invoke(previous, current);
        }

        public void RaiseInvalidMove(int cellIndex)
        {
            OnInvalidMove?.Invoke(cellIndex);
        }

        public void RaiseHintStarted()
        {
            OnHintStarted?.Invoke();
        }

        public void RaiseHintResolved(HintResult result)
        {
            OnHintResolved?.Invoke(result);
        }

        public void RaiseLevelWon(int levelNumber, string levelId)
        {
            OnLevelWon?.Invoke(levelNumber, levelId);
        }

        public void RequestUndo()
        {
            OnUndoRequested?.Invoke();
        }

        public void RequestRestart()
        {
            OnRestartRequested?.Invoke();
        }

        public void RequestHint()
        {
            OnHintRequested?.Invoke();
        }
    }
}
