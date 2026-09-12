using System;

namespace ASTeams.SingleLine.Import
{
    /// <summary>
    /// Where a level sat in the reference data: which pack folder it came from and the
    /// number its file carried. Source order is defined by this pair, so parsing it
    /// lives in one place rather than being re-derived wherever an id is sorted.
    /// </summary>
    public readonly struct SourceLevelAddress
    {
        private const string LevelPrefix = "Level_";

        public string Pack { get; }

        /// <summary>Number from the file name, or <see cref="int.MaxValue"/> when absent.</summary>
        public int Number { get; }

        public SourceLevelAddress(string pack, int number)
        {
            Pack = pack;
            Number = number;
        }

        /// <summary>
        /// Reads "pack/Level_12". Anything that does not match still yields an address,
        /// with the whole id as the pack and no number, so an unexpected id sorts last
        /// instead of sinking the import.
        /// </summary>
        public static SourceLevelAddress Parse(string sourceId)
        {
            if (string.IsNullOrEmpty(sourceId))
            {
                return new SourceLevelAddress(string.Empty, int.MaxValue);
            }

            int slash = sourceId.LastIndexOf('/');

            if (slash < 0)
            {
                return new SourceLevelAddress(sourceId, int.MaxValue);
            }

            string pack = sourceId.Substring(0, slash);
            string file = sourceId.Substring(slash + 1);

            if (!file.StartsWith(LevelPrefix, StringComparison.Ordinal) ||
                !int.TryParse(file.Substring(LevelPrefix.Length), out int number))
            {
                return new SourceLevelAddress(pack, int.MaxValue);
            }

            return new SourceLevelAddress(pack, number);
        }

        public override string ToString()
        {
            return Pack + "/" + LevelPrefix + Number;
        }
    }
}
