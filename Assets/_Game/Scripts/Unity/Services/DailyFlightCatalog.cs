using System.Collections.Generic;
using AirlinePop.SingleLine.Core;
using AirlinePop.SingleLine.Data;

namespace AirlinePop.SingleLine.Unity
{
    /// <summary>The daily flights shipped in their own file, apart from the campaign's chapters.</summary>
    public static class DailyFlightCatalog
    {
        public const string Folder = "Daily";
        public const string ChapterId = "daily";
        public const string ResourcePath = Folder + "/" + ChapterId;

        /// <summary>What the flight is billed as beside its number, as picture boards are by their picture.</summary>
        public const string FlightName = "daily";

        /// <summary>Every daily flight, or an empty list when the file is missing.</summary>
        public static List<LevelData> Load()
        {
            var source = new ResourcesChapterSource(Folder);
            return source.TryReadChapter(ChapterId, out string json)
                ? LevelJsonSerializer.DeserializeChapter(json)
                : new List<LevelData>();
        }
    }
}
