using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// One extra condition laid onto a finished level, built from the level's own solution
    /// so the level stays solvable. A new kind of condition is a new class, not an edit.
    /// </summary>
    public interface ILevelVariant
    {
        /// <summary>The condition this variant adds.</summary>
        LevelRule Rule { get; }

        /// <summary>The level with the condition added; unchanged when it cannot take it.</summary>
        LevelData Apply(LevelData level);
    }
}
