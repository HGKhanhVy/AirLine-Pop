using ASTeams.Base;
using ASTeams.SingleLine.Core;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    public sealed class GameplayAudioPresenter : MonoBehaviour
    {
        [Tooltip("One tone per step, in rising order. GDD 11 asks the step sound to climb " +
                 "with the path so a long line builds; the reference game ships the ladder " +
                 "and this plays it rung by rung. Empty falls back to the single step sound.")]
        [SerializeField] private AudioClip[] stepTones;

        [SerializeField, Range(0f, 1f)] private float stepVolume = 0.7f;

        [Tooltip("The ladder plays through its own source so that a new note replaces the " +
                 "one before it. The shared one-shot sources let notes pile up: measured " +
                 "at an ordinary drag speed, three neighbouring semitones ring together " +
                 "and the melody turns into a wash. One square, one note.")]
        [SerializeField] private AudioSource stepSource;

        [Header("Board actions")]
        [Tooltip("Taking a step back. These four are wired as clips rather than through " +
                 "SoundName because the shared enum lives in the SDK and has no entry for " +
                 "undo, restart or a hint; naming them here keeps the SDK untouched.")]
        [SerializeField] private AudioClip undoClip;

        [SerializeField] private AudioClip restartClip;

        [SerializeField] private AudioClip hintClip;

        [Tooltip("Played on a win. It used to be a short burst because the win screen " +
                 "carried the fanfare; that screen is gone and a win now rolls straight " +
                 "into the next level, so this is the only thing marking the finish.")]
        [SerializeField] private AudioClip winCelebrationClip;

        [Tooltip("Played when the path walks into a dead end. A named SDK sound stood " +
                 "here before, which made the one moment the player has actually lost " +
                 "share a cue with every other warning in the game.")]
        [SerializeField] private AudioClip deadEndClip;

        [SerializeField, Range(0f, 1f)] private float actionVolume = 0.7f;

        [Header("Music")]
        [Tooltip("Starts the background loop when the board opens. The track and its " +
                 "volume come from the shared sound config, not from here.")]
        [SerializeField] private bool isMusicOn = true;

        private AudioController audioController;
        private int lastProgress;

        private void OnEnable()
        {
            audioController = AudioController.Instance;

            // Nothing in the game was starting the track. It is asked for here rather than
            // per level because the SDK restarts the music on every call, and a loop that
            // begins again at every win is worse than no loop at all.
            if (isMusicOn)
            {
                audioController?.PlayMusic(SoundName.Gameplay_Music);
            }

            GameplayEvents.OnLevelLoaded += HandleLevelLoaded;
            GameplayEvents.OnProgressChanged += HandleProgressChanged;
            GameplayEvents.OnInvalidMove += HandleInvalidMove;
            GameplayEvents.OnStateChanged += HandleStateChanged;
            GameplayEvents.OnUndoRequested += HandleUndoRequested;
            GameplayEvents.OnRestartRequested += HandleRestartRequested;
            GameplayEvents.OnHintRequested += HandleHintRequested;
        }

        private void OnDisable()
        {
            GameplayEvents.OnLevelLoaded -= HandleLevelLoaded;
            GameplayEvents.OnProgressChanged -= HandleProgressChanged;
            GameplayEvents.OnInvalidMove -= HandleInvalidMove;
            GameplayEvents.OnStateChanged -= HandleStateChanged;
            GameplayEvents.OnUndoRequested -= HandleUndoRequested;
            GameplayEvents.OnRestartRequested -= HandleRestartRequested;
            GameplayEvents.OnHintRequested -= HandleHintRequested;
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
        /// rung it wraps back to the bottom and climbs again, the way the reference game
        /// does: the clips are 24 notes of a scale, not 24 unrelated noises.
        ///
        /// It used to clamp at the top instead, on the theory that dropping back would
        /// read as the path starting over. Counting the shipped levels shows why that was
        /// the wrong call: 48 of 300 run longer than the ladder and the largest is 69
        /// squares, so on those the last 44 steps all fired the same top note. One note
        /// hammered forty times reads as a broken sound, not as a melody.
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

            int rung = (visitedCells - 2) % stepTones.Length;
            AudioClip tone = stepTones[rung];

            if (tone == null)
            {
                return;
            }

            if (stepSource == null)
            {
                // No source of our own: the notes overlap, but a drag is still heard.
                audioController.PlaySound(tone, stepVolume);
                return;
            }

            // Play, not PlayOneShot: this cuts whatever was still ringing, which is the
            // whole point. The source is ours rather than the controller's, so the mute
            // setting has to be honoured here too.
            stepSource.clip = tone;
            stepSource.volume = audioController.IsMuteSound ? 0f : stepVolume;
            stepSource.Play();
        }

        /// <summary>
        /// A refused move answers with motion, not with sound.
        ///
        /// It used to play the scrape on UI_Invalid, and measuring a drag showed why that
        /// was wrong: a finger crossing a board brushes diagonals and squares it has
        /// already covered constantly, so the scrape fired over and over and two or three
        /// copies of it rang under the melody the whole way along. GDD 4 asks for a
        /// refused move to be felt, and the square's own nudge does that without putting
        /// anything on top of the notes.
        /// </summary>
        private void HandleInvalidMove(int cellIndex)
        {
        }

        // The three the player reaches for when a path goes wrong. They answer the press
        // rather than the outcome, which is what a button is expected to do; the HUD only
        // offers them when they can actually be used.
        private void HandleUndoRequested()
        {
            PlayAction(undoClip);
        }

        private void HandleRestartRequested()
        {
            PlayAction(restartClip);
        }

        private void HandleHintRequested()
        {
            PlayAction(hintClip);
        }

        private void PlayAction(AudioClip clip)
        {
            if (clip != null)
            {
                audioController?.PlaySound(clip, actionVolume);
            }
        }

        private void HandleStateChanged(PathState previous, PathState current)
        {
            if (current == PathState.Stuck)
            {
                PlayAction(deadEndClip);
            }
            else if (current == PathState.Won)
            {
                PlayAction(winCelebrationClip);
            }
        }
    }
}