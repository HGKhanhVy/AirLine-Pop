using System.Collections.Generic;

namespace ASTeams.SingleLine.Import
{
    /// <summary>Builds the tag arrays a level is copied with.</summary>
    public static class TagList
    {
        /// <summary>The tags with one more added at the end.</summary>
        public static string[] With(IReadOnlyList<string> tags, string tag)
        {
            var result = new string[tags.Count + 1];

            for (int i = 0; i < tags.Count; i++)
            {
                result[i] = tags[i];
            }

            result[tags.Count] = tag;
            return result;
        }
    }
}
