using System;
using System.Collections.Generic;
using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// Collapses a corpus down to one level per board shape. Across the 1059 ripped
    /// files this removes 225 duplicates, several of which are nine copies of the same
    /// board scattered over two packs.
    ///
    /// The result is deterministic: running the importer twice must produce the same
    /// representatives, or level ids would move between builds and break save data.
    /// </summary>
    public sealed class LevelDeduplicator
    {
        private readonly BoardCanonicalizer canonicalizer;

        public LevelDeduplicator(BoardCanonicalizer canonicalizer)
        {
            this.canonicalizer = canonicalizer ?? throw new ArgumentNullException(nameof(canonicalizer));
        }

        /// <summary>
        /// Groups levels by board shape, preserving the order in which shapes were first
        /// seen so the caller gets a stable listing.
        /// </summary>
        public List<LevelGroup> Group(IReadOnlyList<LevelData> levels)
        {
            if (levels == null)
            {
                throw new ArgumentNullException(nameof(levels));
            }

            var byKey = new Dictionary<BoardKey, List<LevelData>>();
            var order = new List<BoardKey>();

            for (int i = 0; i < levels.Count; i++)
            {
                LevelData level = levels[i];

                if (level == null)
                {
                    throw new ArgumentException("Level list must not contain nulls.", nameof(levels));
                }

                BoardKey key = canonicalizer.GetKey(level);

                if (!byKey.TryGetValue(key, out List<LevelData> members))
                {
                    members = new List<LevelData>(1);
                    byKey.Add(key, members);
                    order.Add(key);
                }

                members.Add(level);
            }

            var groups = new List<LevelGroup>(order.Count);

            for (int i = 0; i < order.Count; i++)
            {
                BoardKey key = order[i];
                List<LevelData> members = byKey[key];
                groups.Add(new LevelGroup(key, ChooseRepresentative(members), members));
            }

            return groups;
        }

        /// <summary>
        /// Prefers a level that ships its own solution, because that solution was authored
        /// and play tested rather than produced by the solver. Ties break on the id so the
        /// choice never depends on file enumeration order.
        /// </summary>
        private static LevelData ChooseRepresentative(List<LevelData> members)
        {
            LevelData best = members[0];

            for (int i = 1; i < members.Count; i++)
            {
                LevelData candidate = members[i];

                if (candidate.HasSolution != best.HasSolution)
                {
                    if (candidate.HasSolution)
                    {
                        best = candidate;
                    }

                    continue;
                }

                if (string.CompareOrdinal(candidate.Id, best.Id) < 0)
                {
                    best = candidate;
                }
            }

            return best;
        }
    }
}
