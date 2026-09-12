using System;
using ASTeams.SingleLine.Core;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    [CreateAssetMenu(menuName = "Single Line/Events/Gameplay Event Channel", fileName = "GameplayEvents")]
    public sealed class GameplayEventChannelSO : ScriptableObject
    {
        public event Action<int, string, int, int> OnLevelLoaded;
        public event Action<int, int> OnProgressChanged;
        public event Action<PathState, PathState> OnStateChanged;
        public event Action<int> OnInvalidMove;
        public event Action<HintResult> OnHintResolved;
        public event Action<int, string> OnLevelWon;

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

        public void RaiseHintResolved(HintResult result)
        {
            OnHintResolved?.Invoke(result);
        }

        public void RaiseLevelWon(int levelNumber, string levelId)
        {
            OnLevelWon?.Invoke(levelNumber, levelId);
        }
    }
}
