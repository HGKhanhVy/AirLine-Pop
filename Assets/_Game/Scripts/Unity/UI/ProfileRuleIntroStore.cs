using AirlinePop.SingleLine.Core;

namespace AirlinePop.SingleLine.Unity
{
    /// <summary>Keeps the rules already introduced in the saved profile, beside the player's other progress.</summary>
    public sealed class ProfileRuleIntroStore : IRuleIntroStore
    {
        private const string KeyPrefix = "rule_seen_";

        private readonly PlayerProfile profile;

        public ProfileRuleIntroStore(PlayerProfile profile)
        {
            this.profile = profile;
        }

        public bool HasSeen(LevelRule rule)
        {
            return profile != null && profile.Get<bool>(KeyPrefix + rule);
        }

        public void MarkSeen(LevelRule rule)
        {
            profile?.Set(KeyPrefix + rule, true);
        }
    }
}
