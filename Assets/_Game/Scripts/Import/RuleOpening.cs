using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Import
{
    /// <summary>The flight on which a condition is first met, flown on its own there.</summary>
    public readonly struct RuleOpening
    {
        public LevelRule Rule { get; }

        public int LevelNumber { get; }

        public RuleOpening(LevelRule rule, int levelNumber)
        {
            Rule = rule;
            LevelNumber = levelNumber;
        }
    }
}
