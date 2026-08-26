using System;
using System.Collections.Generic;
using UnityEngine;
using ASTeams.Base;
using ASTeams.Base.Analytics;
using ASTeams.Base.Data;
using ASTeams.Base.UI;
using ASTeams.Base.Gameplay;
using ASTeams.Base.UI.Template;

namespace ASTeams.Template
{
    public class GameResultHandleService : GameplayServiceBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private ReviveActionRouter reviveActionRouter;
        [SerializeField] private FailTypeSO failTypeSO;

        private LevelService levelService;
        private GameStateService gameStateService;
        private FeatureUnlockService featureUnlockService;
        private ComboService comboService;

        private bool _busy;
        private bool _winLevelProgressApplied;
        private bool _winFinalizeStarted;
        private bool _winCoinGranted;

        // cache fail data để revive / lose dùng tiếp
        private bool _hasFailCache;
        private GameFailPopupData _lastFailData;

        public override void OnRegister(GameplayServices services)
        {
            services.TryGet(out levelService);
            services.TryGet(out gameStateService);
            services.TryGet(out featureUnlockService);
            services.TryGet(out comboService);
        }

        public override void OnStart()
        {
            _busy = false;
            _winLevelProgressApplied = false;
            _winFinalizeStarted = false;
            _winCoinGranted = false;
            _hasFailCache = false;
            _lastFailData = default;

            if (gameStateService != null)
                gameStateService.OnChangeState += OnGameStateChanged;
        }

        public override void OnStop()
        {
            if (gameStateService != null)
                gameStateService.OnChangeState -= OnGameStateChanged;
        }

        private void OnGameStateChanged(GameState prev, GameState next)
        {
            if (!IsEnabled) return;

            switch (next)
            {
                case GameState.Win:
                    HandleWinState();
                    break;

                case GameState.Lose:
                    HandleLoseState();
                    break;
            }
        }

        private void HandleWinState()
        {
            ShowWin(new WinPopupData
            {
                level = GetLevelFallback(),
                coinReward = GetLevelReward()
            });
        }

        private void HandleLoseState()
        {
            var failType = gameStateService != null
                ? gameStateService.CurrentFailType
                : FailType.TimeUp;

            var failData = BuildFailPopupData(failType);
            var reviveData = BuildRevivePopupData(failData);
            ShowFailThenRevive(failData, reviveData, null);
        }

        private GameFailPopupData BuildFailPopupData(FailType failType)
        {
            var cfg = GetFailConfig(failType);
            var fallbackLoseText = GetFallbackLoseText(failType);
            return new GameFailPopupData
            {
                level = GetLevelFallback(),
                failType = failType,
                loseSprite = cfg != null ? cfg.loseSprite : null,
                loseText = cfg != null ? cfg.loseText : fallbackLoseText,
                reviveSprite = cfg != null ? cfg.reviveSprite : null,
                reviveText = cfg != null ? cfg.reviveTitle : fallbackLoseText,
                reviveDscText = cfg != null ? cfg.reviveDescription : "Continue?"
            };
        }

        private static string GetFallbackLoseText(FailType failType)
        {
            return failType == FailType.NoAvailableMoves ? "No Avaliable Moves" : "FAILED";
        }

        private RevivePopupData BuildRevivePopupData(in GameFailPopupData failData)
        {
            return new RevivePopupData
            {
                level = failData.level,
                failType = failData.failType,
                reviveText = failData.reviveText,
                reviveDscText = failData.reviveDscText,
                coinPrice = GetReviveCoinPrice(),
                rewardedPlacement = "revive",
                reviveSprite = failData.reviveSprite,
            };
        }

        private FailTypeConfig GetFailConfig(FailType failType)
        {
            if (failTypeSO == null) return null;
            return failTypeSO.GetConfig(failType);
        }

        private int GetLevelFallback()
        {
            if (levelService != null) return levelService.CurrentLevelNumber;
            if (UserProfileController.Instance != null) return UserProfileController.Instance.LEVEL;
            return 1;
        }

        private int GetLevelReward()
        {
            if (ConfigController.Instance != null && ConfigController.Instance.GameConfig != null)
                return ConfigController.Instance.GameConfig.levelReward;
            return 0;
        }

        private long GetReviveCoinPrice()
        {
            if (ConfigController.Instance != null && ConfigController.Instance.GameConfig != null)
                return ConfigController.Instance.GameConfig.levelRevice;
            return 0;
        }

        // =========================================================
        // WIN
        // =========================================================

        private void ShowWin(WinPopupData? overrideData = null)
        {
            if (!IsEnabled)
            {
                FinalizeWinAndReload();
                return;
            }

            if (_busy)
            {
                Debug.LogWarning("[GameResultHandleService] Win flow already active. Finalizing current win.");
                FinalizeWinAndReload();
                return;
            }

            _busy = true;
            ResetWinSessionFlags();

            if (UIPopupController.Instance == null)
            {
                Debug.LogWarning("[GameResultHandleService] UIPopupController missing. Advancing level without win popup.");
                ReportGameFinished(true);
                FinalizeWinAndReload();
                return;
            }

            var popup = UIPopupController.Instance.GetActivePopup<UIGameWinPopup>();
            if (popup == null)
            {
                Debug.LogWarning("[GameResultHandleService] UIGameWinPopup prefab missing. Advancing level without win popup.");
                ReportGameFinished(true);
                FinalizeWinAndReload();
                return;
            }

            ReportGameFinished(true);

            popup.onShowed.RemoveAllListeners();
            popup.onShowed.AddListener(TryApplyWinLevelProgress);
            popup.OnNext = FinalizeWinAndReload;
            popup.Show();
        }

        private void ResetWinSessionFlags()
        {
            _winLevelProgressApplied = false;
            _winFinalizeStarted = false;
            _winCoinGranted = false;
        }

        /// <summary>
        /// Single entry point: advance profile level, grant reward, hide popup, reload scene. Safe to call multiple times.
        /// </summary>
        public void FinalizeWinAndReload()
        {
            if (_winFinalizeStarted)
            {
                return;
            }

            _winFinalizeStarted = true;

            TryApplyWinLevelProgress();
            GrantWinCoinRewardOnce();
            DismissWinPopup();
            _busy = false;

            if (UISceneController.Instance != null)
            {
                UISceneController.Instance.ReloadCurrentScene();
            }
        }

        /// <summary>
        /// Ensures profile level advances once per win (popup anim, auto-continue, or missing UI).
        /// </summary>
        public void ApplyWinLevelProgressIfNeeded()
        {
            TryApplyWinLevelProgress();
        }

        public void MarkWinCoinsGranted()
        {
            _winCoinGranted = true;
        }

        private void GrantWinCoinRewardOnce()
        {
            if (_winCoinGranted || UserProfileController.Instance == null)
            {
                return;
            }

            int reward = GetLevelReward();
            if (reward <= 0)
            {
                return;
            }

            _winCoinGranted = true;
            UserProfileController.Instance.AddCoin(reward);
        }

        private static void DismissWinPopup()
        {
            if (UIPopupController.Instance == null)
            {
                return;
            }

            UIPopupController.Instance.HidePopup<UIGameWinPopup>();
        }

        // =========================================================
        // FAIL -> REVIVE
        // =========================================================

        private void ShowFailThenRevive(
            GameFailPopupData failData,
            RevivePopupData? reviveOverrideData = null,
            Action onReviveSuccess = null)
        {
            if (!IsEnabled || _busy) return;
            _busy = true;

            if (UIPopupController.Instance != null)
                UIPopupController.Instance.HideAllPopups();

            if (failData.level <= 0)
                failData.level = GetLevelFallback();

            _hasFailCache = true;
            _lastFailData = failData;

            var popup = UIPopupController.Instance.GetActivePopup<UIGameFailPopup>();
            if (popup == null)
            {
                _busy = false;
                return;
            }

            popup.SetData(failData);
            popup.SetOnFinished(() =>
            {
                ShowRevive(reviveOverrideData, onReviveSuccess);
            });
            popup.Show();
        }

        private void ShowRevive(RevivePopupData? overrideData, Action onReviveSuccess)
        {
            if (UIPopupController.Instance != null)
                UIPopupController.Instance.HidePopup<UIGameFailPopup>();

            var level = GetLevelFallback();
            var failType = _hasFailCache ? _lastFailData.failType : FailType.TimeUp;

            var reviveTitle = _hasFailCache ? _lastFailData.reviveText : "REVIVE";
            var reviveDescription = _hasFailCache ? _lastFailData.reviveDscText : "Continue?";

            var data = overrideData ?? new RevivePopupData
            {
                level = level,
                failType = failType,
                reviveText = reviveTitle,
                reviveDscText = reviveDescription,
                coinPrice = GetReviveCoinPrice(),
                rewardedPlacement = "revive",
                reviveSprite = _lastFailData.reviveSprite
            };

            if (data.level <= 0) data.level = level;
            data.failType = failType;

            var popup = UIPopupController.Instance.GetActivePopup<UIGameRevivePopup>();
            if (popup == null)
            {
                _busy = false;
                return;
            }

            popup.SetData(data);

            popup.OnGiveUp = () =>
            {
                _busy = false;
                ShowLose(new LosePopupData
                {
                    level = level,
                    failType = failType
                });
            };

            popup.OnRevive = () =>
            {
                _busy = false;

                if (reviveActionRouter != null)
                    reviveActionRouter.TryExecute(failType);

                onReviveSuccess?.Invoke();

                if (gameStateService != null)
                    gameStateService.ResetToPlaying();
            };

            popup.Show();
        }

        // =========================================================
        // LOSE
        // =========================================================

        private void ShowLose(LosePopupData? overrideData = null)
        {
            if (!IsEnabled || _busy) return;
            _busy = true;

            var failTypeFallback = _hasFailCache ? _lastFailData.failType : FailType.TimeUp;

            var data = overrideData ?? new LosePopupData
            {
                level = GetLevelFallback(),
                failType = failTypeFallback
            };

            var popup = UIPopupController.Instance.GetActivePopup<UIGameLosePopup>();
            if (popup == null)
            {
                ReportGameFinished(false);
                _busy = false;
                return;
            }

            ReportGameFinished(false);

            popup.OnRetry = () =>
            {
                _busy = false;
                UISceneController.Instance.ReloadCurrentScene();
            };

            popup.Show();
        }

        private void ReportGameFinished(bool isWin)
        {
            AnalyticsController analytics = AnalyticsController.Instance;
            if (analytics == null)
            {
                return;
            }

            int level = levelService != null && levelService.CurrentLevelNumber > 0
                ? levelService.CurrentLevelNumber
                : GetLevelFallback();
            int boosterCount = comboService != null ? Mathf.Max(0, comboService.Combo) : 0;
            string result = isWin ? "win" : "lose";
            string loseBy = isWin
                ? string.Empty
                : (gameStateService != null ? gameStateService.CurrentFailType.ToString() : FailType.TimeUp.ToString());

            analytics.LogLevelEnd(level, 1, 0, 0, 0f, boosterCount, 0, loseBy, result, 0f);
        }

        private void TryApplyWinLevelProgress()
        {
            if (_winLevelProgressApplied)
            {
                return;
            }

            if (UserProfileController.Instance == null)
            {
                return;
            }

            _winLevelProgressApplied = true;

            int completedLevel = UserProfileController.Instance.LEVEL;
            UserProfileController.Instance.LEVEL += 1;

            int syncLevel;
            if (featureUnlockService != null
                && featureUnlockService.TryGetFeatureAtLevel(completedLevel, out _))
            {
                syncLevel = UserProfileController.Instance.LEVEL;
            }
            else
            {
                syncLevel = completedLevel;
            }

            featureUnlockService?.HandleLevelLoaded(syncLevel);
        }

        // =========================================================
        // OPTIONAL PUBLIC API
        // =========================================================

        public void ForceShowWin()
        {
            HandleWinState();
        }

        public void ForceShowLose(FailType failType)
        {
            var failData = BuildFailPopupData(failType);
            ShowFailThenRevive(failData, BuildRevivePopupData(failData), null);
        }

        public void ClearFailCache()
        {
            _hasFailCache = false;
            _lastFailData = default;
        }
    }
}