using ASTeams.Base;
using ASTeams.SingleLine.Core;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    public sealed class GameplayAudioPresenter : MonoBehaviour
    {
        [SerializeField] private GameplayEventChannelSO eventChannel;

        private AudioController audioController;
        private int lastProgress;

        private void OnEnable()
        {
            if (eventChannel == null)
            {
                return;
            }

            audioController = AudioController.Instance;
            eventChannel.OnLevelLoaded += HandleLevelLoaded;
            eventChannel.OnProgressChanged += HandleProgressChanged;
            eventChannel.OnInvalidMove += HandleInvalidMove;
            eventChannel.OnStateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            if (eventChannel == null)
            {
                return;
            }

            eventChannel.OnLevelLoaded -= HandleLevelLoaded;
            eventChannel.OnProgressChanged -= HandleProgressChanged;
            eventChannel.OnInvalidMove -= HandleInvalidMove;
            eventChannel.OnStateChanged -= HandleStateChanged;
        }

        private void HandleLevelLoaded(int levelNumber, string levelId, int difficulty, int totalCells)
        {
            lastProgress = 0;
            audioController?.PlaySound(SoundName.UI_LevelStart);
        }

        private void HandleProgressChanged(int visitedCells, int totalCells)
        {
            if (visitedCells > lastProgress && visitedCells > 1)
            {
                audioController?.PlaySound(SoundName.UI_Progress);
            }

            lastProgress = visitedCells;
        }

        private void HandleInvalidMove(int cellIndex)
        {
            audioController?.PlaySound(SoundName.UI_Invalid);
        }

        private void HandleStateChanged(PathState previous, PathState current)
        {
            if (current == PathState.Stuck)
            {
                audioController?.PlaySound(SoundName.UI_Warning);
            }
            else if (current == PathState.Won)
            {
                audioController?.PlaySound(SoundName.UI_LevelComplete);
            }
        }
    }
}