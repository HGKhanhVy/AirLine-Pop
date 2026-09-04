using System.Collections.Generic;
using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// One chapter of the campaign: a run of levels already renamed and re-rated, in the
    /// order the player will meet them.
    /// </summary>
    public sealed class Chapter
    {
        private readonly List<PlacedLevel> placements;
        private readonly List<LevelData> levels;

        /// <summary>One based, so chapter 1 is the first one a player sees.</summary>
        public int Number { get; }

        /// <summary>Stable identifier used for the exported file name, such as ch01.</summary>
        public string Id { get; }

        /// <summary>What gets exported.</summary>
        public IReadOnlyList<LevelData> Levels => levels;

        /// <summary>The same levels with the curve data the exporter does not need.</summary>
        public IReadOnlyList<PlacedLevel> Placements => placements;

        public int LevelCount => levels.Count;

        public Chapter(int number, string id, List<PlacedLevel> placements)
        {
            Number = number;
            Id = id;
            this.placements = placements;

            levels = new List<LevelData>(placements.Count);

            for (int i = 0; i < placements.Count; i++)
            {
                levels.Add(placements[i].Level);
            }
        }

        public override string ToString()
        {
            return Id + " (" + levels.Count + " levels)";
        }
    }
}
