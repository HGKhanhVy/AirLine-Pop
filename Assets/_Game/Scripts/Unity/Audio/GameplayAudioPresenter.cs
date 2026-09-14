using ASTeams.Base;
using ASTeams.SingleLine.Core;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    public sealed class GameplayAudioPresenter : MonoBehaviour
    {
        [SerializeField] private GameplayEventChannelSO eventChannel;

        [Tooltip("One tone per step, in rising order. GDD 11 asks the step sound to climb " +
                 "with the path so a long line builds; the reference game ships the ladder " +
                 "and this plays it rung by rung. Empty falls back to the single step sound.")]
        [SerializeField] private AudioClip[] stepTones;

        [SerializeField, Range(0f, 1f)] private float stepVolume = 0.7f;

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
                PlayStep(visitedCells);
            }

            lastProgress = visitedCells;
        }

        /// <summary>
        /// The second square is the first step, so the ladder starts there. Past the top
        /// rung the highest tone repeats rather than falling back to the bottom, which
        /// would read as the path starting over.
        ///
        /// The clip overload is used on purpose: the named one is rate limited, and a fast
        /// drag would drop most of its own steps.
        /// </summary>
        private void PlayStep(int visitedCells)
        {
            if (audioController == null)
            {
                return;
            }

            if (stepTones == null || stepTones.Length == 0)
            {
                audioController.PlaySound(SoundName.UI_Progress);
                return;
            }

            int rung = Mathf.Clamp(visitedCells - 2, 0, stepTones.Length - 1);
            AudioClip tone = stepTones[rung];

            if (tone != null)
            {
                audioController.PlaySound(tone, stepVolume);
            }
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