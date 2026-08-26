using System;
using ASTeams.Base.Analytics;
using ASTeams.Template;
using UnityEngine;

namespace ASTeams.Base.Gameplay
{
    public enum GameState
    {
        Pause,
        Playing,
        Win,
        Lose
    }

    public sealed class GameStateService : GameplayServiceBehaviour
    {
        public event Action<GameState, GameState> OnChangeState; // (prev, next)

        public GameState State { get; private set; } = GameState.Playing;
        public FailType CurrentFailType { get; private set; } = FailType.TimeUp;

        private GameplayServices _services;

        private GameResultHandleService _resultHandleService;
        private ComboService _comboService;
        private LevelService _levelService;

        private bool _gameStartedReported;

        public override void OnRegister(GameplayServices services)
        {
            _services = services;
            services.TryGet(out _resultHandleService);
            services.TryGet(out _comboService);
            services.TryGet(out _levelService);
        }

        public override void OnStart()
        {
            _gameStartedReported = false;
            SetState(GameState.Pause);
            OnChangeState += OnGameStateChanged;

            if (State == GameState.Playing && !_gameStartedReported)
            {
                ReportGameStarted();
                _gameStartedReported = true;
            }
        }

        public override void OnStop()
        {
            OnChangeState -= OnGameStateChanged;
            OnChangeState = null;
        }

        private void OnGameStateChanged(GameState prev, GameState next)
        {
            if (next == GameState.Playing && prev == GameState.Pause && !_gameStartedReported)
            {
                ReportGameStarted();
                _gameStartedReported = true;
            }

            bool resultHandlerCanRun = _resultHandleService != null && _resultHandleService.IsEnabled;

            if (next == GameState.Win && !resultHandlerCanRun)
            {
                ReportGameFinished(true);
            }
            else if (next == GameState.Lose && !resultHandlerCanRun)
            {
                ReportGameFinished(false);
            }
        }

        private void ReportGameStarted()
        {
            AnalyticsController analytics = AnalyticsController.Instance;
            if (analytics == null)
            {
                return;
            }

            int level = _levelService != null && _levelService.CurrentLevelNumber > 0
                ? _levelService.CurrentLevelNumber
                : 1;

            analytics.LogLevelStart(level, 0f, "normal", 1, 0);
        }

        private void ReportGameFinished(bool isWin)
        {
            AnalyticsController analytics = AnalyticsController.Instance;
            if (analytics == null)
            {
                return;
            }

            int level = _levelService != null && _levelService.CurrentLevelNumber > 0
                ? _levelService.CurrentLevelNumber
                : 1;
            int boosterCount = _comboService != null ? Mathf.Max(0, _comboService.Combo) : 0;
            string result = isWin ? "win" : "lose";
            string loseBy = isWin ? string.Empty : CurrentFailType.ToString();

            analytics.LogLevelEnd(level, 1, 0, 0, 0f, boosterCount, 0, loseBy, result, 0f);
        }

        public bool Is(GameState s) => State == s;

        public bool SetState(GameState next)
        {
            if (State == next) return false;

            var prev = State;
            State = next;
            OnChangeState?.Invoke(prev, next);
            return true;
        }

        public bool Play() => SetState(GameState.Playing);

        public bool Pause()
        {
            if (State != GameState.Playing) return false;
            return SetState(GameState.Pause);
        }

        public bool Resume()
        {
            if (State != GameState.Pause) return false;
            return SetState(GameState.Playing);
        }

        public bool Win()
        {
            if (State != GameState.Playing && State != GameState.Pause) return false;
            return SetState(GameState.Win);
        }

        public bool Lose(FailType failType)
        {
            Debug.Log($"Lose {failType}");
            if (State != GameState.Playing && State != GameState.Pause) return false;

            CurrentFailType = failType;
            return SetState(GameState.Lose);
        }

        public void ResetToPlaying()
        {
            SetState(GameState.Playing);
        }
    }
}