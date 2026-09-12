using System;
using System.Collections.Generic;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// The order the reference packs are played in.
    ///
    /// PackInfo.json in the source data names four packs, Beginner, Medium, Expert and
    /// Master, but only the first two have a folder that still carries levels; the rest
    /// of the folders are earlier revisions kept beside them. The default below is that
    /// manifest order first, then the leftovers, so the shipped run opens with the
    /// reference game's own beginner pack in its own numbering.
    ///
    /// A pack the order does not mention still ships, after every pack it does, in
    /// ordinal name order. Silently dropping levels because a folder was renamed would
    /// be worse than placing them late.
    /// </summary>
    public sealed class LevelPackOrder
    {
        private readonly Dictionary<string, int> ranks;

        public static readonly LevelPackOrder Default = new LevelPackOrder(
            "beginner",
            "medium",
            "hard",
            "mediumold",
            "expertold",
            "masterold",
            "beginner 1",
            "beginner222");

        public LevelPackOrder(params string[] packs)
        {
            ranks = new Dictionary<string, int>(
                packs == null ? 0 : packs.Length,
                StringComparer.OrdinalIgnoreCase);

            if (packs == null)
            {
                return;
            }

            for (int i = 0; i < packs.Length; i++)
            {
                if (!string.IsNullOrEmpty(packs[i]) && !ranks.ContainsKey(packs[i]))
                {
                    ranks.Add(packs[i], i);
                }
            }
        }

        public int PackCount => ranks.Count;

        /// <summary>
        /// Sort rank of a pack. Unknown packs share the rank just past the known ones,
        /// which leaves their relative order to the name comparison in the assembler.
        /// </summary>
        public int RankOf(string pack)
        {
            if (pack != null && ranks.TryGetValue(pack, out int rank))
            {
                return rank;
            }

            return ranks.Count;
        }

        public bool Knows(string pack)
        {
            return pack != null && ranks.ContainsKey(pack);
        }
    }
}
