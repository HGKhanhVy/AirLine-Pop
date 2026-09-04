using System.Collections.Generic;
using ASTeams.SingleLine.Core;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// One board shape and every file in the corpus that turned out to be a rotation or
    /// mirror of it. The importer ships <see cref="Representative"/> and keeps the rest
    /// only so the editor tool can explain what was dropped.
    /// </summary>
    public sealed class LevelGroup
    {
        private readonly List<LevelData> members;

        public BoardKey Key { get; }

        /// <summary>The level chosen to ship for this shape.</summary>
        public LevelData Representative { get; }

        /// <summary>Every level sharing the shape, including the representative.</summary>
        public IReadOnlyList<LevelData> Members => members;

        public int DuplicateCount => members.Count - 1;

        public bool HasDuplicates => members.Count > 1;

        public LevelGroup(BoardKey key, LevelData representative, List<LevelData> members)
        {
            Key = key;
            Representative = representative;
            this.members = members;
        }
    }
}
