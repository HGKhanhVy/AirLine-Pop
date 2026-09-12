using System.Collections.Generic;
using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// Decides which parsed levels are allowed into the pool the campaign is built from.
    ///
    /// The two assemblers disagree about duplicates. A curve needs every board to be a
    /// distinct puzzle, so rotations and mirrors collapse. Source order must ship the
    /// packs as they are, and the reference packs genuinely repeat a few boards across
    /// pack boundaries; dropping those would shift every later level by one and break
    /// the very thing source order exists to preserve.
    /// </summary>
    public interface IBoardSelector
    {
        IReadOnlyList<LevelData> Select(IReadOnlyList<LevelData> parsed, ImportReport report);
    }
}
