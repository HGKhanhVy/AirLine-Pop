using System;
using ASTeams.Base.Data;
using ASTeams.Base.Level;
using UnityEngine;

namespace ASTeams.Base.Gameplay
{
    /// <summary>
    /// Service version of LevelController (drag-drop, no Singleton).
    /// Keeps the same logic: UserProfileController.Instance.LEVEL is 1-based.
    /// </summary>
    public sealed class LevelService : GameplayServiceBehaviour
    {
        [SerializeField] private LevelConfigSO levelConfig;

        public event Action<int, LevelConfig> OnLevelLoaded; 

        [SerializeField] private int _currentLevelNumber = 1;

        [SerializeField] private LevelConfig _currentLevel;

        /// <summary>1-based level index (Lv.1, Lv.2...)</summary>
        public int CurrentLevelNumber => _currentLevelNumber;

        /// <summary>Current LevelConfig (can be null if config missing/out of range)</summary>
        public LevelConfig CurrentLevel => _currentLevel;

        public int LevelCount => levelConfig != null ? levelConfig.LevelCount : 0;

        public override void OnStart()
        {
            var profileLevel = UserProfileController.Instance.LEVEL;
            LoadLevel(Mathf.Max(1, profileLevel));
        }

        public void LoadLevel(int levelNumber1Based)
        {
            if (levelConfig == null)
            {
                Debug.LogWarning("[LevelService] LevelConfigSO is not assigned.");
                return;
            }

            _currentLevelNumber = Mathf.Max(1, levelNumber1Based);
            _currentLevel = levelConfig.GetLevelByIndex(_currentLevelNumber - 1);
            UserProfileController.Instance.LEVEL = _currentLevelNumber;
            OnLevelLoaded?.Invoke(_currentLevelNumber, _currentLevel);
        }

        /// <summary>Advance to next level.</summary>
        public void NextLevel()
        {
            LoadLevel(_currentLevelNumber + 1);
        }
    }
}