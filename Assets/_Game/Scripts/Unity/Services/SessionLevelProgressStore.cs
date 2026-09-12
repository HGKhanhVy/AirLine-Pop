using ASTeams.SingleLine.Data;

namespace ASTeams.SingleLine.Unity
{
    public sealed class SessionLevelProgressStore : ILevelProgressStore
    {
        public SessionLevelProgressStore(int initialLevelNumber)
        {
            CurrentLevelNumber = initialLevelNumber;
        }

        public int CurrentLevelNumber { get; private set; }

        public void SaveCurrentLevel(int levelNumber)
        {
            CurrentLevelNumber = levelNumber;
        }
    }
}
