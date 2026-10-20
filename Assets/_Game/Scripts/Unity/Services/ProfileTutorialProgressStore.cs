
namespace AirlinePop.SingleLine.Unity
{
    /// <summary>
    /// Tutorial steps kept in the free-form data slot of the saved profile, beside the
    /// settings, so they travel with the rest of the save.
    /// </summary>
    public sealed class ProfileTutorialProgressStore : ITutorialProgressStore
    {
        private const string KeyPrefix = "tutorial_";

        private readonly PlayerProfile profile;

        public ProfileTutorialProgressStore(PlayerProfile profile)
        {
            this.profile = profile;
        }

        public bool IsDone(string stepId)
        {
            return profile != null && profile.Get<bool>(KeyPrefix + stepId);
        }

        public void MarkDone(string stepId)
        {
            if (profile != null)
            {
                profile.Set(KeyPrefix + stepId, true);
            }
        }
    }
}
