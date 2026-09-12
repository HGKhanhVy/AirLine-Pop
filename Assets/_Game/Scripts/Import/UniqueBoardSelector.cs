using System.Collections.Generic;
using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// Collapses rotations and mirrors so the pool holds each board shape once. This is
    /// what a difficulty curve needs: the same puzzle turned ninety degrees reads as a
    /// repeat to the player, however different its file looks.
    /// </summary>
    public sealed class UniqueBoardSelector : IBoardSelector
    {
        private readonly LevelDeduplicator deduplicator;

        public UniqueBoardSelector()
            : this(new LevelDeduplicator(new BoardCanonicalizer()))
        {
        }

        public UniqueBoardSelector(LevelDeduplicator deduplicator)
        {
            this.deduplicator = deduplicator;
        }

        public IReadOnlyList<LevelData> Select(IReadOnlyList<LevelData> parsed, ImportReport report)
        {
            IReadOnlyList<LevelGroup> groups = deduplicator.Group(parsed);

            if (report != null)
            {
                report.Groups = groups;
            }

            var representatives = new List<LevelData>(groups.Count);

            for (int i = 0; i < groups.Count; i++)
            {
                representatives.Add(groups[i].Representative);
            }

            return representatives;
        }
    }
}
