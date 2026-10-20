using System;
using AirlinePop.SingleLine.Data;

namespace AirlinePop.SingleLine.Unity
{
    public sealed class UserProfileLevelProgressStore : ILevelProgressStore
    {
        private readonly PlayerProfile profile;

        public UserProfileLevelProgressStore(PlayerProfile profile)
        {
            this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
        }

        public int CurrentLevelNumber => profile.LevelNumber;

        public void SaveCurrentLevel(int levelNumber)
        {
            profile.LevelNumber = levelNumber;
        }
    }
}
