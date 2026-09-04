using Newtonsoft.Json;

namespace ASTeams.SingleLine.Data
{
    /// <summary>
    /// On-disk shape of one level, matching the JSON example in GDD 6.3 field for field.
    ///
    /// Kept separate from <see cref="ASTeams.SingleLine.Core.LevelData"/> on purpose: the
    /// runtime model is free to change shape, gain lookup masks or drop fields, without
    /// breaking a build that shipped with data already on players' devices.
    /// </summary>
    public sealed class LevelDto
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("version")]
        public int Version { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("activeCells")]
        public int[] ActiveCells { get; set; }

        /// <summary>Null when the level lets the player start anywhere.</summary>
        [JsonProperty("fixedStart", NullValueHandling = NullValueHandling.Ignore)]
        public int? FixedStart { get; set; }

        [JsonProperty("fixedEnd", NullValueHandling = NullValueHandling.Ignore)]
        public int? FixedEnd { get; set; }

        [JsonProperty("solution", NullValueHandling = NullValueHandling.Ignore)]
        public int[] Solution { get; set; }

        [JsonProperty("difficulty")]
        public int Difficulty { get; set; }

        [JsonProperty("themeId", NullValueHandling = NullValueHandling.Ignore)]
        public string ThemeId { get; set; }

        [JsonProperty("tags", NullValueHandling = NullValueHandling.Ignore)]
        public string[] Tags { get; set; }
    }
}
