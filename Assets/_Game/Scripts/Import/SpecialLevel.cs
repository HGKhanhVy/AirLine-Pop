using System;
using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// A hand-made board pinned to one place in the campaign, such as a picture board on
    /// the flight that lands in a new country. The assembler places it as it is instead
    /// of drawing a level from the pool for that slot.
    /// </summary>
    public readonly struct SpecialLevel
    {
        /// <summary>One based, the number the player sees on the route map.</summary>
        public int LevelNumber { get; }

        public LevelData Level { get; }

        public SpecialLevel(int levelNumber, LevelData level)
        {
            LevelNumber = levelNumber;
            Level = level ?? throw new ArgumentNullException(nameof(level));
        }

        public override string ToString()
        {
            return "#" + LevelNumber + " " + Level.Id;
        }
    }
}
