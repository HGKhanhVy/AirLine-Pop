using System.Collections;
using System.Threading.Tasks;
using ASTeams.Base;
using ASTeams.Base.Data;
using ASTeams.Base.Gameplay;
using ASTeams.Base.Level;
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

        [Tooltip("The one config that lists every level. Leave empty to fall back on the " +
                 "ch<chapter>_<slot> numbering rule, which an isolated scene needs.")]
        [SerializeField] private LevelConfigSO levelConfig;

        [Tooltip("From the Gameplay scene services. Keeps the template flow on the same " +
                 "level number gameplay is on.")]
        [SerializeField] private LevelService levelService;

        [Tooltip("From the persistent MANAGERS prefab. Leave empty to play without haptics.")]
        [SerializeField] private VibrationController vibration;

        [Tooltip("Used only when UserProfileController is unavailable in an isolated Editor run.")]
        [SerializeField, Range(1, CampaignLevelAddress.MaxLevelNumber)]
        private int fallbackLevelNumber = 1;

        [Tooltip("Seconds to admire a finished board before the next one opens.")]
        [SerializeField, Min(0f)] private float delayAfterWin = 0.9f;

        private ILevelRepository repository;
        private ILevelProgressStore progressStore;
        private ILevelCatalog levelCatalog;
        private string currentLevelId;
        private int currentLevelNumber;
        private Coroutine advanceRoutine;
        private WaitForSeconds advanceDelay;

        public string CurrentLevelId => currentLevelId;

        public int CurrentLevelNumber => currentLevelNumber;

        private void Start()
        {
            repository = new ChapterLevelRepository(new ResourcesChapterSource());
            advanceDelay = new WaitForSeconds(delayAfterWin);

            VibrationController hapticController = vibration != null
                ? vibration
                : VibrationController.Instance;
            controller.SetHaptics(hapticController == null ? null : new SdkHapticService(hapticController));
            controller.SetHintService(new ContinuationHintService(new WarnsdorffSolverFactory()));
            controller.OnStateChanged += HandleStateChanged;

            levelCatalog = CreateCatalog();
            progressStore = CreateProgressStore();

            int savedLevel = Mathf.Clamp(progressStore.CurrentLevelNumber, 1, LastLevelNumber);

            if (savedLevel != progressStore.CurrentLevelNumber)
            {
                progressStore.SaveCurrentLevel(savedLevel);
            }

            if (!TryLoadLevelNumber(savedLevel))
            {
                Debug.LogError(
                    "No level " + savedLevel + " to load. Check the level config and run " +
                    "Tools/Single Line/Level Importer if the chapter files are missing.",
                    this);
            }
        }

        /// <summary>
        /// The config decides the order when a designer has filled one in. An empty or
        /// missing config would otherwise leave the scene with nothing to play, so the
        /// numbering rule stands in for it.
        /// </summary>
        private ILevelCatalog CreateCatalog()
        {
            var fromConfig = new LevelConfigCatalog(levelConfig);

            if (fromConfig.LevelCount > 0 && fromConfig.TryGetLevelId(1, out _))
            {
                return fromConfig;
            }

            if (levelConfig != null)
            {
                Debug.LogWarning(
                    "Level config holds no Single Line levels, falling back on the numbering rule.",
                    this);
            }

            return new CampaignAddressCatalog();
        }

        /// <summary>
        /// One owner for the campaign position, in this order: the template's level
        /// service when the scene has one, the profile alone when it does not, and an
        /// in-memory value when neither exists so a bare scene still runs.
        /// </summary>
        private ILevelProgressStore CreateProgressStore()
        {
            UserProfileController profile = UserProfileController.Instance;

            if (profile == null)
            {
                return new SessionLevelProgressStore(fallbackLevelNumber);
            }

            return levelService != null
                ? (ILevelProgressStore)new LevelServiceProgressStore(levelService, profile)
                : new UserProfileLevelProgressStore(profile);
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
            int clamped = Mathf.Clamp(levelNumber, 1, LastLevelNumber);
            return levelCatalog.TryGetLevelId(clamped, out string levelId) && TryLoad(levelId);
        }

        /// <summary>Highest number the catalog can address, so a short config cannot run off its end.</summary>
        private int LastLevelNumber =>
            levelCatalog == null || levelCatalog.LevelCount <= 0
                ? CampaignLevelAddress.MaxLevelNumber
                : levelCatalog.LevelCount;

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

        /// <summary>
        /// Steps one place along the catalog rather than along the chapter files, so a
        /// config that reorders or shortens the campaign is obeyed here too.
        /// </summary>
        public bool LoadNext()
        {
            return currentLevelNumber < LastLevelNumber && TryLoadLevelNumber(currentLevelNumber + 1);
        }

        private void HandleStateChanged(PathState previous, PathState current)
        {
            if (current != PathState.Won)
            {
                return;
            }

            int completedLevel = currentLevelNumber;
            string completedLevelId = currentLevelId;
            int savedNextLevel = Mathf.Min(LastLevelNumber, completedLevel + 1);

            if (progressStore != null && progressStore.CurrentLevelNumber < savedNextLevel)
            {
                progressStore.SaveCurrentLevel(savedNextLevel);
            }

            eventChannel?.RaiseLevelWon(completedLevel, completedLevelId);
            gameStateService?.Win();

            CancelAdvance();

            if (completedLevel < LastLevelNumber)
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
