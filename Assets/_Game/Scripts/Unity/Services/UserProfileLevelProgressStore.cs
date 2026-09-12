using System;
using ASTeams.Base.Data;
using ASTeams.SingleLine.Data;

namespace ASTeams.SingleLine.Unity
{
    public sealed class UserProfileLevelProgressStore : ILevelProgressStore
    {
        private readonly UserProfileController profile;

        public UserProfileLevelProgressStore(UserProfileController profile)
        {
            this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
        }

        public int CurrentLevelNumber => profile.LEVEL;

        public void SaveCurrentLevel(int levelNumber)
        {
            profile.LEVEL = levelNumber;
        }
    }
}
