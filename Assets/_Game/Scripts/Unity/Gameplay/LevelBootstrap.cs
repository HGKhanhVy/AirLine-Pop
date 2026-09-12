using System.Collections;
using System.Threading.Tasks;
using ASTeams.Base;
using ASTeams.Base.Data;
using ASTeams.Base.Gameplay;
using ASTeams.SingleLine.Core;
using ASTeams.SingleLine.Data;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Composition root for Single Line gameplay. It owns repository and progression
    /// adapters, while rules remain inside the tested core classes.
    /// </summary>
    public sealed class LevelBootstrap : MonoBehaviour
    {
        [SerializeField] private GameplayController controller;
        [SerializeField] private GameplayEventChannelSO eventChannel;
        [SerializeField] private GameStateService gameStateService;

        [Tooltip("From the persistent MANAGERS prefab. Leave empty to play without haptics.")]
        [SerializeField] private VibrationController vibration;

        [Tooltip("Used only when UserProfileController is unavailable in an isolated Editor run.")]
        [SerializeField, Range(1, CampaignLevelAddress.MaxLevelNumber)]
        private int fallbackLevelNumber = 1;

        [Tooltip("Seconds to admire a finished board before the next one opens.")]
        [SerializeField, Min(0f)] private float delayAfterWin = 0.9f;

        private ILevelRepository repository;
        private ILevelSequence levelSequence;
        private ILevelProgressStore progressStore;
        private string currentLevelId;
        private int currentLevelNumber;
        private Coroutine advanceRoutine;
        private WaitForSeconds advanceDelay;

        public string CurrentLevelId => currentLevelId;

        public int CurrentLevelNumber => currentLevelNumber;

        private void Start()
        {
            repository = new ChapterLevelRepository(new ResourcesChapterSource());
            levelSequence = new ChapterLevelSequence(repository);
            advanceDelay = new WaitForSeconds(delayAfterWin);

            VibrationController hapticController = vibration != null
                ? vibration
                : VibrationController.Instance;
            controller.SetHaptics(hapticController == null ? null : new SdkHapticService(hapticController));
            controller.SetHintService(new ContinuationHintService(new WarnsdorffSolverFactory()));
            controller.OnStateChanged += HandleStateChanged;

            UserProfileController profile = UserProfileController.Instance;
            progressStore = profile == null
                ? new SessionLevelProgressStore(fallbackLevelNumber)
                : new UserProfileLevelProgressStore(profile);

            int savedLevel = Mathf.Clamp(progressStore.CurrentLevelNumber, 1,
                CampaignLevelAddress.MaxLevelNumber);

            if (savedLevel != progressStore.CurrentLevelNumber)
            {
                progressStore.SaveCurrentLevel(savedLevel);
            }

            if (!TryLoadLevelNumber(savedLevel))
            {
                Debug.LogError(
                    "No level " + CampaignLevelAddress.ToLevelId(savedLevel) +
                    " under Resources. Run Tools/Single Line/Level Importer first.",
                    this);
            }
        }

        private void OnDestroy()
        {
            if (controller != null)
            {
                controller.OnStateChanged -= HandleStateChanged;
            }
        }

        public bool TryLoad(string levelId)
        {
            if (repository == null || !repository.TryGet(levelId, out LevelData level))
            {
                return false;
            }

            CancelAdvance();
            currentLevelId = levelId;

            if (!CampaignLevelAddress.TryGetLevelNumber(levelId, out currentLevelNumber))
            {
                currentLevelNumber = 1;
            }

            controller.Load(level);
            gameStateService?.ResetToPlaying();
            eventChannel?.RaiseLevelLoaded(currentLevelNumber, currentLevelId, level.Difficulty,
                level.ActiveCellCount);
            return true;
        }

        public bool TryLoadLevelNumber(int levelNumber)
        {
            int clamped = Mathf.Clamp(levelNumber, 1, CampaignLevelAddress.MaxLevelNumber);
            return TryLoad(CampaignLevelAddress.ToLevelId(clamped));
        }

        public void Restart()
        {
            controller.Restart();
        }

        public void Undo()
        {
            controller.Undo();
        }

        public Task<bool> HintAsync()
        {
            return controller.ShowHintAsync();
        }

        public bool LoadNext()
        {
            string next = levelSequence.GetNext(currentLevelId);
            return next != null && TryLoad(next);
        }

        private void HandleStateChanged(PathState previous, PathState current)
        {
            if (current != PathState.Won)
            {
                return;
            }

            int completedLevel = currentLevelNumber;
            string completedLevelId = currentLevelId;
            int savedNextLevel = Mathf.Min(CampaignLevelAddress.MaxLevelNumber, completedLevel + 1);

            if (progressStore != null && progressStore.CurrentLevelNumber < savedNextLevel)
            {
                progressStore.SaveCurrentLevel(savedNextLevel);
            }

            eventChannel?.RaiseLevelWon(completedLevel, completedLevelId);
            gameStateService?.Win();

            CancelAdvance();

            if (completedLevel < CampaignLevelAddress.MaxLevelNumber)
            {
                advanceRoutine = StartCoroutine(AdvanceRoutine());
            }
        }

        private IEnumerator AdvanceRoutine()
        {
            yield return advanceDelay;
            advanceRoutine = null;
            LoadNext();
        }

        private void CancelAdvance()
        {
            if (advanceRoutine == null)
            {
                return;
            }

            StopCoroutine(advanceRoutine);
            advanceRoutine = null;
        }

        private void OnDisable()
        {
            CancelAdvance();
        }
    }
}
