using System.Collections.Generic;
using UnityEngine;

namespace ASTeams.Base.Level
{
    public enum LevleType
    {
        Easy,
        Hard,
        SuperHard
    }

    [System.Serializable]
    public class LevelConfig
    {
        public int second;
        public ScriptableObject levelDefinition;
        public LevleType type;
    }

    [CreateAssetMenu(fileName = "LevelConfig", menuName = "ASTeams/Level Config", order = 1)]
    public class LevelConfigSO : ScriptableObject
    {
        public List<LevelConfig> levels = new List<LevelConfig>();
        public int levelStartLoop = 20;

        public int LevelCount => levels != null ? levels.Count : 0;

        public LevelConfig GetLevelByIndex(int index)
        {
            if (levels == null || levels.Count == 0)
            {
                return null;
            }

            if (index >= 0 && index < levels.Count)
            {
                return CloneLevelConfig(levels[index]);
            }

            var startLoopingIndex = levelStartLoop - 1;
            var loopCount = levels.Count - startLoopingIndex;
            if (loopCount <= 0)
            {
                return CloneLevelConfig(levels[levels.Count - 1]);
            }

            var relativeIndex = (index - startLoopingIndex) % loopCount;
            var mappedIndex = startLoopingIndex + relativeIndex;
            return CloneLevelConfig(levels[mappedIndex]);
        }

        public int GetMaxBranchCount()
        {
            return 0;
        }

        private static LevelConfig CloneLevelConfig(LevelConfig source)
        {
            if (source == null)
            {
                return null;
            }

            return new LevelConfig
            {
                second = source.second,
                levelDefinition = source.levelDefinition,
                type = source.type,
            };
        }
    }
}
