using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>Remembers which level rules the player has already been shown.</summary>
    public interface IRuleIntroStore
    {
        bool HasSeen(LevelRule rule);

        void MarkSeen(LevelRule rule);
    }
}
