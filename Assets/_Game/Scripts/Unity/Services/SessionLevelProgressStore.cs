using AirlinePop.SingleLine.Data;

namespace AirlinePop.SingleLine.Unity
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
