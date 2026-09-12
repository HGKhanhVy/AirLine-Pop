using System;
using System.Collections.Generic;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// Ships the reference packs in their own order: pack by pack as
    /// <see cref="LevelPackOrder"/> lists them, and inside a pack by the number on the
    /// file, then cut into chapters of the size the layout asks for.
    ///
    /// The campaign the players see is the one the reference data already tuned by hand,
    /// which is what the team asked for after comparing the build against the resource
    /// folders. Difficulty is still scored and carried through, because the UI and the
    /// level config read it, but it no longer decides where a level lands.
    ///
    /// Levels the preparer could not solve never reach this point, so a slot is filled by
    /// the next source level rather than left empty. That shifts the numbering after a
    /// dropped file, which is why the import report lists exactly which ones went.
    /// </summary>
    public sealed class SourceOrderAssembler : ICampaignAssembler
    {
        private readonly ChapterLayout layout;
        private readonly LevelPackOrder packOrder;

        public SourceOrderAssembler()
            : this(ChapterLayout.Default, LevelPackOrder.Default)
        {
        }

        public SourceOrderAssembler(ChapterLayout layout)
            : this(layout, LevelPackOrder.Default)
        {
        }

        public SourceOrderAssembler(ChapterLayout layout, LevelPackOrder packOrder)
        {
            this.layout = layout;
            this.packOrder = packOrder ?? LevelPackOrder.Default;
        }

        public Campaign Assemble(IReadOnlyList<ScoredLevel> pool)
        {
            if (pool == null)
            {
                throw new ArgumentNullException(nameof(pool));
            }

            if (pool.Count < layout.TotalLevels)
            {
                throw new ArgumentException(
                    "Need " + layout.TotalLevels + " levels but the pool holds only " + pool.Count + ".",
                    nameof(pool));
            }

            List<ScoredLevel> ordered = SortBySourceOrder(pool);
            var chapters = new List<Chapter>(layout.ChapterCount);
            int taken = 0;

            for (int chapterIndex = 0; chapterIndex < layout.ChapterCount; chapterIndex++)
            {
                string chapterId = "ch" + (chapterIndex + 1).ToString("00");
                var placements = new List<PlacedLevel>(layout.LevelsPerChapter);

                for (int slot = 0; slot < layout.LevelsPerChapter; slot++)
                {
                    ScoredLevel chosen = ordered[taken++];
                    string levelId = chapterId + "_" + (slot + 1).ToString("000");

                    // Target mirrors the score: source order makes no claim about a curve,
                    // so reporting a miss against one would invent a number.
                    placements.Add(new PlacedLevel(
                        chosen.Level.WithIdentity(levelId, chosen.Difficulty),
                        chosen.Score,
                        chosen.Score));
                }

                chapters.Add(new Chapter(chapterIndex + 1, chapterId, placements));
            }

            return new Campaign(chapters, taken, 0);
        }

        private List<ScoredLevel> SortBySourceOrder(IReadOnlyList<ScoredLevel> pool)
        {
            var ordered = new List<ScoredLevel>(pool);
            var addresses = new Dictionary<string, SourceLevelAddress>(pool.Count, StringComparer.Ordinal);

            for (int i = 0; i < pool.Count; i++)
            {
                string id = pool[i].Level.Id;

                if (!addresses.ContainsKey(id))
                {
                    addresses.Add(id, SourceLevelAddress.Parse(id));
                }
            }

            ordered.Sort((a, b) => Compare(addresses[a.Level.Id], addresses[b.Level.Id], a.Level.Id, b.Level.Id));
            return ordered;
        }

        private int Compare(SourceLevelAddress a, SourceLevelAddress b, string idA, string idB)
        {
            int byPack = packOrder.RankOf(a.Pack).CompareTo(packOrder.RankOf(b.Pack));

            if (byPack != 0)
            {
                return byPack;
            }

            int byName = string.CompareOrdinal(a.Pack, b.Pack);

            if (byName != 0)
            {
                return byName;
            }

            int byNumber = a.Number.CompareTo(b.Number);

            // Ids are unique, so the final comparison makes the whole sort total and the
            // import reproducible whatever order the files arrived in.
            return byNumber != 0 ? byNumber : string.CompareOrdinal(idA, idB);
        }
    }
}
