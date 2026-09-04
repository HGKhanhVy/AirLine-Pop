using System.Collections.Generic;
using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Data
{
    /// <summary>
    /// Reads levels by id, following GDD 15.3. Everything above this line, from the level
    /// select screen to the gameplay controller, depends on the interface and never on
    /// where the data physically lives, which is what makes the ripped placeholder
    /// content replaceable without touching gameplay.
    /// </summary>
    public interface ILevelRepository
    {
        /// <summary>
        /// The level with this id, loading its chapter first if needed. Throws when no
        /// such level exists, so a wrong id fails loudly at the call site.
        /// </summary>
        LevelData Get(string levelId);

        /// <summary>
        /// Non-throwing form, for the places that legitimately ask about a level that may
        /// not be there, such as looking up the next level past the end of a chapter.
        /// </summary>
        bool TryGet(string levelId, out LevelData level);

        /// <summary>
        /// Level ids of one chapter in play order, or an empty list when the chapter does
        /// not exist. Loads the chapter if it is not loaded yet.
        /// </summary>
        IReadOnlyList<string> GetLevelIds(string chapterId);

        /// <summary>
        /// Reads a chapter ahead of time so the first board of it does not pay the cost.
        /// Returns false when there is no such chapter.
        /// </summary>
        bool TryPreloadChapter(string chapterId);
    }
}
