using System.Collections.Generic;
using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// Keeps every parsed level, duplicates included. Source order ships the reference
    /// packs as their authors numbered them, so a board repeated later in a pack is a
    /// deliberate part of that run and stays in place.
    /// </summary>
    public sealed class AllBoardsSelector : IBoardSelector
    {
        public IReadOnlyList<LevelData> Select(IReadOnlyList<LevelData> parsed, ImportReport report)
        {
            return parsed;
        }
    }
}
