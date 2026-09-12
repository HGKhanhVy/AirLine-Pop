using System;
using ASTeams.Base.Data;
using ASTeams.Base.Gameplay;
using ASTeams.SingleLine.Data;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Keeps the campaign position in the template's <see cref="LevelService"/> so the
    /// rest of the flow, the win popup, the background and the booster service, reads
    /// the same number gameplay does.
    ///
    /// Reading goes to the profile rather than to the service because both take their
    /// value from there and the profile is ready first: services start from
    /// <c>GameController.Start</c>, which has no defined order against this scene's own
    /// Start. Writing goes through the service, which updates its own state and the
    /// profile together.
    /// </summary>
    public sealed class LevelServiceProgressStore : ILevelProgressStore
    {
        private readonly LevelService levelService;
        private readonly UserProfileController profile;

        public LevelServiceProgressStore(LevelService levelService, UserProfileController profile)
        {
            this.levelService = levelService ?? throw new ArgumentNullException(nameof(levelService));
            this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
        }

        public int CurrentLevelNumber => profile.LEVEL;

        public void SaveCurrentLevel(int levelNumber)
        {
            levelService.LoadLevel(levelNumber);
        }
    }
}
