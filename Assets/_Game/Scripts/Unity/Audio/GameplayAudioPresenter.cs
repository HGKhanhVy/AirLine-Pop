using ASTeams.Base;
using ASTeams.SingleLine.Core;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    public sealed class GameplayAudioPresenter : MonoBehaviour
    {
        [Tooltip("One note per step, in the order of the tune: arpeggios over a " +
                 "chord progression, so every step is a new note and all of them belong " +
                 "together. Empty falls back to the single step sound.")]
        [SerializeField] private AudioClip[] stepTones;

        [SerializeField, Range(0f, 1f)] private float stepVolume = 0.25f;

        [Tooltip("Each level starts the step tune this many notes further on, so different " +
                 "levels play different parts of it rather than always its opening.")]
        [SerializeField, Min(0)] private int stepShiftPerLevel = 16;

        [Tooltip("The voices the tune is played on, in turn. Each note fades out on its own " +
                 "instead of being cut by the next; with only a few voices the " +
                 "oldest note gives way, so a fast drag never piles up into a wash. The notes " +
                 "of one chord are what ring together, which is why that stays sweet.")]
        [SerializeField] private AudioSource[] stepVoices = System.Array.Empty<AudioSource>();

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

        [Header("Passengers")]
        [Tooltip("A cat passenger climbing aboard.")]
        [SerializeField] private AudioClip passengerClip;
        [SerializeField, Range(0f, 1f)] private float passengerVolume = 0.6f;

        private AudioController audioController;
        private int lastProgress;
        private int lastAboard;
        private int stepOffset;
        private int nextVoice;

        private void OnEnable()
        {
            audioController = AudioController.Instance;

            GameplayEvents.OnLevelLoaded += HandleLevelLoaded;
            GameplayEvents.OnProgressChanged += HandleProgressChanged;
            GameplayEvents.OnPassengersChanged += HandlePassengersChanged;
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
            GameplayEvents.OnPassengersChanged -= HandlePassengersChanged;
            GameplayEvents.OnInvalidMove -= HandleInvalidMove;
            GameplayEvents.OnStateChanged -= HandleStateChanged;
            GameplayEvents.OnUndoRequested -= HandleUndoRequested;
            GameplayEvents.OnRestartRequested -= HandleRestartRequested;
            GameplayEvents.OnHintRequested -= HandleHintRequested;
        }

        private void HandleLevelLoaded(int levelNumber, string levelId, int difficulty, int totalCells)
        {
            lastProgress = 0;
            lastAboard = 0;
            int tuneLength = stepTones != null ? stepTones.Length : 0;
            stepOffset = tuneLength > 0 ? (levelNumber - 1) * stepShiftPerLevel % tuneLength : 0;
            audioController?.PlaySound(SoundName.UI_LevelStart);
        }

        /// <summary>Only a passenger getting on is heard; one let off by an undo is not.</summary>
        private void HandlePassengersChanged(int aboard, int total)
        {
            if (aboard > lastAboard && passengerClip != null)
            {
                audioController?.PlaySound(passengerClip, passengerVolume);
            }

            lastAboard = aboard;
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
        /// The second square is the first step, so the tune starts there. The clips are the
        /// notes of one tune in order, so a drag plays the tune note by note; each
        /// level starts it at its own place, and past the last note it goes round again.
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

            int rung = (stepOffset + visitedCells - 2) % stepTones.Length;
            AudioClip tone = stepTones[rung];

            if (tone == null)
            {
                return;
            }

            if (stepVoices.Length == 0)
            {
                audioController.PlaySound(tone, stepVolume);
                return;
            }

            // The voices are ours rather than the controller's, so the mute setting has to
            // be honoured here too.
            AudioSource voice = stepVoices[nextVoice];
            nextVoice = (nextVoice + 1) % stepVoices.Length;
            voice.clip = tone;
            voice.volume = audioController.IsMuteSound ? 0f : stepVolume;
            voice.Play();
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