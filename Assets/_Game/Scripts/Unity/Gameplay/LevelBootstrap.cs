using System.Collections;
using System.Threading.Tasks;
using ASTeams.Base;
using ASTeams.Base.Data;
using ASTeams.Base.Gameplay;
using ASTeams.Base.Level;
using ASTeams.SingleLine.Core;
using ASTeams.SingleLine.Data;
using ASTeams.Template;
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
        [SerializeField] private GameStateService gameStateService;

        [Tooltip("The one config that lists every level. Leave empty to fall back on the " +
                 "ch<chapter>_<slot> numbering rule, which an isolated scene needs.")]
        [SerializeField] private LevelConfigSO levelConfig;

        [Tooltip("From the Gameplay scene services. Keeps the template flow on the same " +
                 "level number gameplay is on.")]
        [SerializeField] private LevelService levelService;

        [Tooltip("From the Gameplay scene services. Only needed so the reward is not paid " +
                 "twice when the template's own win flow is switched on.")]
        [SerializeField] private GameResultHandleService resultHandler;

        [Tooltip("From the persistent MANAGERS prefab. Leave empty to play without haptics.")]
        [SerializeField] private VibrationController vibration;

        [Tooltip("Used only when UserProfileController is unavailable in an isolated Editor run.")]
        [SerializeField, Range(1, CampaignLevelAddress.MaxLevelNumber)]
        private int fallbackLevelNumber = 1;

        [Tooltip("Open the next level on its own after a win. Turn this off when a win " +
                 "screen owns the moment; it then raises RequestNextLevel on the channel.")]
        [SerializeField] private bool advancesAutomatically = true;

        [Tooltip("Seconds to admire a finished board before the next one opens.")]
        [SerializeField, Min(0f)] private float delayAfterWin = 0.9f;

        [Tooltip("Paid on top of the level reward the first time a level is finished. " +
                 "GDD 8.1 puts it at 20 coins; the base reward lives in the team's GameConfig.")]
        [SerializeField, Min(0)] private int firstClearBonus = 20;

        private ILevelRepository repository;
        private ILevelProgressStore progressStore;
        private ILevelCatalog levelCatalog;
        private ILevelRewardService rewardService;
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

            Subscribe();
            levelCatalog = CreateCatalog();
            progressStore = CreateProgressStore();
            rewardService = CreateRewardService();

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

            Unsubscribe();
        }

        /// <summary>
        /// Listening for the three things a screen can ask the board to do. A button
        /// anywhere in the project can raise these on the channel asset, so the UI needs
        /// no reference into gameplay and neither scene has to be rewired when it lands.
        /// </summary>
        private void Subscribe()
        {
            GameplayEvents.OnUndoRequested += Undo;
            GameplayEvents.OnRestartRequested += Restart;
            GameplayEvents.OnHintRequested += HandleHintRequested;
            GameplayEvents.OnNextLevelRequested += HandleNextLevelRequested;
            GameplayEvents.OnSnapshotRequested += PublishSnapshot;
        }

        private void Unsubscribe()
        {
            GameplayEvents.OnUndoRequested -= Undo;
            GameplayEvents.OnRestartRequested -= Restart;
            GameplayEvents.OnHintRequested -= HandleHintRequested;
            GameplayEvents.OnNextLevelRequested -= HandleNextLevelRequested;
            GameplayEvents.OnSnapshotRequested -= PublishSnapshot;
        }

        /// <summary>
        /// The hint runs as a task the caller may await. A channel request has nobody to
        /// await it, so the result is reported through the channel instead and the task
        /// is deliberately left unobserved.
        /// </summary>
        private void HandleHintRequested()
        {
            _ = HintAsync();
        }

        /// <summary>
        /// A win screen's Continue button. Cancels any pending automatic advance first, so
        /// pressing Continue during the delay opens the next level once, not twice.
        /// </summary>
        private void HandleNextLevelRequested()
        {
            CancelAdvance();
            LoadNext();
        }

        /// <summary>
        /// Nothing is paid when no profile is loaded, which is the case for a scene opened
        /// on its own; the board still plays, it just has no economy behind it.
        /// </summary>
        private ILevelRewardService CreateRewardService()
        {
            UserProfileController profile = UserProfileController.Instance;
            return profile == null ? null : new ProfileLevelRewardService(profile, resultHandler, firstClearBonus);
        }

        private void AwardLevelReward(bool isFirstClear)
        {
            if (rewardService == null)
            {
                return;
            }

            int granted = rewardService.AwardLevelReward(isFirstClear);

            if (granted > 0)
            {
                GameplayEvents.RaiseCoinsAwarded(granted, rewardService.Balance);
                PublishBalance();
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
            GameplayEvents.RaiseLevelLoaded(currentLevelNumber, currentLevelId, level.Difficulty,
                level.ActiveCellCount);

            // A fresh board already stands on its start square, so the count starts at one.
            // Without this a label would read zero until the player's first move.
            GameplayEvents.RaiseProgressChanged(controller.Progress, controller.Target);
            PublishBalance();
            return true;
        }

        /// <summary>
        /// Says again everything a screen would have heard had it been listening when the
        /// level opened. Nothing is stored to answer this; the board reads its own state.
        /// </summary>
        private void PublishSnapshot()
        {
            LevelData level = controller.Level;

            if (level != null)
            {
                GameplayEvents.RaiseLevelLoaded(currentLevelNumber, currentLevelId, level.Difficulty,
                    level.ActiveCellCount);
                GameplayEvents.RaiseProgressChanged(controller.Progress, controller.Target);
                GameplayEvents.RaiseStateChanged(controller.State, controller.State);
            }

            GameplayEvents.RaiseRewindChanged(controller.IsRewinding);
            PublishBalance();
        }

        private void PublishBalance()
        {
            if (rewardService != null)
            {
                GameplayEvents.RaiseCoinBalanceChanged(rewardService.Balance);
            }
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

            // A finished level is a checkpoint, so both halves of it are written now:
            // the coins first, then the position. Paying after the state change rather
            // than after an animation means a player who kills the app on the win screen
            // still keeps what they earned.
            // Finishing the furthest level the save has reached is a first clear. Replaying
            // an earlier one moves the save nowhere, which is the same test.
            bool isFirstClear = progressStore == null || progressStore.CurrentLevelNumber < savedNextLevel;

            AwardLevelReward(isFirstClear);

            if (progressStore != null && isFirstClear)
            {
                progressStore.SaveCurrentLevel(savedNextLevel);
            }

            GameplayEvents.RaiseLevelWon(completedLevel, completedLevelId);
            gameStateService?.Win();

            CancelAdvance();

            if (advancesAutomatically && completedLevel < LastLevelNumber)
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
