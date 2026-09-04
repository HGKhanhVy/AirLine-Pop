using System.Collections.Generic;
using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// The levels that survived validation, every one of them carrying a solution that
    /// has been checked against the rules.
    /// </summary>
    public sealed class PreparedPool
    {
        private readonly List<LevelData> levels;
        private readonly List<string> droppedIds;

        /// <summary>Usable levels, in the order they were given.</summary>
        public IReadOnlyList<LevelData> Levels => levels;

        /// <summary>Ids of levels no solution could be found for, so they cannot ship.</summary>
        public IReadOnlyList<string> DroppedIds => droppedIds;

        /// <summary>How many levels had no solution stored and were given one by the solver.</summary>
        public int SolvedCount { get; }

        /// <summary>How many shipped a solution that failed the rules and was replaced.</summary>
        public int RepairedCount { get; }

        public PreparedPool(List<LevelData> levels, List<string> droppedIds, int solvedCount, int repairedCount)
        {
            this.levels = levels;
            this.droppedIds = droppedIds;
            SolvedCount = solvedCount;
            RepairedCount = repairedCount;
        }
    }
}
