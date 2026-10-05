using ASTeams.Base.Data;
using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>Keeps the rules already introduced in the saved profile, beside the player's other progress.</summary>
    public sealed class ProfileRuleIntroStore : IRuleIntroStore
    {
        private const string KeyPrefix = "rule_seen_";

        private readonly UserProfileController profile;

        public ProfileRuleIntroStore(UserProfileController profile)
        {
            this.profile = profile;
        }

        public bool HasSeen(LevelRule rule)
        {
            return profile != null && profile.GetParam<bool>(KeyPrefix + rule);
        }

        public void MarkSeen(LevelRule rule)
        {
            profile?.SetParam(KeyPrefix + rule, true);
        }
    }
}
