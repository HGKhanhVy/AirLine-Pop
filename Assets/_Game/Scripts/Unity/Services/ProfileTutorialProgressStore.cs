using ASTeams.Base.Data;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Tutorial steps kept in the free-form data slot of the saved profile, beside the
    /// settings, so they travel with the rest of the save.
    /// </summary>
    public sealed class ProfileTutorialProgressStore : ITutorialProgressStore
    {
        private const string KeyPrefix = "tutorial_";

        private readonly UserProfileController profile;

        public ProfileTutorialProgressStore(UserProfileController profile)
        {
            this.profile = profile;
        }

        public bool IsDone(string stepId)
        {
            return profile != null && profile.GetParam<bool>(KeyPrefix + stepId);
        }

        public void MarkDone(string stepId)
        {
            if (profile != null)
            {
                profile.SetParam(KeyPrefix + stepId, true);
            }
        }
    }
}
