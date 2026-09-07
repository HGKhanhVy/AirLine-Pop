using System.Collections.Generic;
using ASTeams.Base;
using ASTeams.SingleLine.Core;
using ASTeams.SingleLine.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Composition root for the gameplay scene: builds the repository, hands the
    /// controller a level, and moves on when one is won.
    ///
    /// This is the only place that knows a concrete <see cref="IChapterSource"/> exists.
    /// Everything else takes <see cref="ILevelRepository"/>, which is what keeps the
    /// placeholder level data replaceable without touching gameplay.
    /// </summary>
    public sealed class LevelBootstrap : MonoBehaviour
    {
        [SerializeField] private GameplayController controller;
        [SerializeField] private HomeScreen homeScreen;

        [Tooltip("From the SDK's MANAGERS prefab. Leave empty to play without haptics.")]
        [SerializeField] private VibrationController vibration;

        [Tooltip("Level to open on start. Ids are chapter, underscore, slot: ch01_001.")]
        [SerializeField] private string startLevelId = "ch01_001";

        [Tooltip("Seconds to admire a finished board before the next one opens.")]
        [SerializeField, Min(0f)] private float delayAfterWin = 0.9f;

        private ILevelRepository repository;
        private string currentLevelId;
        private float advanceAt;
        private bool waitingToAdvance;

        public string CurrentLevelId => currentLevelId;

        private void Start()
        {
            repository = new ChapterLevelRepository(new ResourcesChapterSource());
            controller.SetHaptics(new SdkHapticService(vibration));
            controller.OnStateChanged += HandleStateChanged;

            if (!TryLoad(startLevelId))
            {
                Debug.LogError(
                    "No level " + startLevelId + " under Resources. Run Tools/Single Line/Level Importer first.",
                    this);
                return;
            }

            if (homeScreen == null)
            {
                return;
            }

            homeScreen.OnPlayRequested += HandlePlayRequested;
            homeScreen.SetSubtitle(currentLevelId);

            // Arriving from the home scene means Play has already been pressed; showing a
            // second entry panel here would ask for it twice.
            if (GameplayEntry.ConsumeImmediateStart())
            {
                homeScreen.HideImmediately();
                controller.SetInputEnabled(true);
                return;
            }

            // The board is built and waiting behind the home panel, so Play costs nothing.
            homeScreen.Show();
            controller.SetInputEnabled(false);
        }

        private void OnDestroy()
        {
            if (controller != null)
            {
                controller.OnStateChanged -= HandleStateChanged;
            }

            if (homeScreen != null)
            {
                homeScreen.OnPlayRequested -= HandlePlayRequested;
            }
        }

        private void Update()
        {
            if (waitingToAdvance && Time.unscaledTime >= advanceAt)
            {
                waitingToAdvance = false;
                LoadNext();
            }

            ReadPrototypeKeys();
        }

        public bool TryLoad(string levelId)
        {
            if (repository == null || !repository.TryGet(levelId, out LevelData level))
            {
                return false;
            }

            currentLevelId = levelId;
            controller.Load(level);

            if (homeScreen != null)
            {
                homeScreen.SetSubtitle(currentLevelId);

                // Coming back to a level while the home panel is up must not hand control
                // back to the board behind it.
                controller.SetInputEnabled(!homeScreen.IsShown);
            }

            return true;
        }

        /// <summary>Rewinds the path back to the start, one cell at a time.</summary>
        public void Restart()
        {
            controller.Restart();
        }

        /// <summary>Steps back one cell.</summary>
        public void Undo()
        {
            controller.Undo();
        }

        /// <summary>Points at a cell that leads to a finish from where the player is.</summary>
        public bool Hint()
        {
            return controller.ShowHint();
        }

        private void HandlePlayRequested()
        {
            controller.SetInputEnabled(true);
        }

        public bool LoadNext()
        {
            string next = FindNextLevelId(currentLevelId);
            return next != null && TryLoad(next);
        }

        private void HandleStateChanged(PathState previous, PathState current)
        {
            if (current != PathState.Won)
            {
                return;
            }

            waitingToAdvance = true;
            advanceAt = Time.unscaledTime + delayAfterWin;
        }

        /// <summary>
        /// The next slot in the chapter, or the first slot of the next chapter. Returns
        /// null at the end of the campaign.
        ///
        /// Walking the repository rather than assuming thirty levels a chapter means a
        /// re-import with a different layout still navigates correctly. Progression proper
        /// belongs to a service later; this is enough to play through.
        /// </summary>
        private string FindNextLevelId(string levelId)
        {
            string chapterId = ChapterLevelRepository.GetChapterId(levelId);

            if (chapterId == null)
            {
                return null;
            }

            IReadOnlyList<string> ids = repository.GetLevelIds(chapterId);

            for (int i = 0; i < ids.Count; i++)
            {
                if (ids[i] != levelId)
                {
                    continue;
                }

                if (i + 1 < ids.Count)
                {
                    return ids[i + 1];
                }

                string nextChapter = NextChapterId(chapterId);
                IReadOnlyList<string> nextIds = nextChapter == null
                    ? null
                    : repository.GetLevelIds(nextChapter);

                return nextIds == null || nextIds.Count == 0 ? null : nextIds[0];
            }

            return null;
        }

        private static string NextChapterId(string chapterId)
        {
            if (!chapterId.StartsWith("ch", System.StringComparison.Ordinal) ||
                !int.TryParse(chapterId.Substring(2), out int number))
            {
                return null;
            }

            return "ch" + (number + 1).ToString("00");
        }

        /// <summary>
        /// Keyboard shortcuts so the prototype is playable before any UI exists. These go
        /// away once the real buttons land.
        /// </summary>
        private void ReadPrototypeKeys()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
            {
                return;
            }

            if (keyboard.zKey.wasPressedThisFrame)
            {
                Undo();
            }

            if (keyboard.rKey.wasPressedThisFrame)
            {
                Restart();
            }

            if (keyboard.nKey.wasPressedThisFrame)
            {
                LoadNext();
            }

            if (keyboard.hKey.wasPressedThisFrame)
            {
                Hint();
            }
        }
    }
}
