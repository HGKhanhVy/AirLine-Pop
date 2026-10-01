using System;
using System.Collections.Generic;

namespace ASTeams.SingleLine.Core
{
    /// <summary>
    /// Looks player-facing text up by key in the language asked for. A line not yet
    /// translated falls back to English, and an unknown key comes back as itself, so a
    /// missing translation shows up on screen instead of a blank.
    /// </summary>
    public sealed class BilingualTextCatalog
    {
        private readonly Dictionary<string, BilingualTextEntry> entries;

        public BilingualTextCatalog(IReadOnlyList<BilingualTextEntry> source)
        {
            entries = new Dictionary<string, BilingualTextEntry>(source.Count, StringComparer.Ordinal);

            for (int i = 0; i < source.Count; i++)
            {
                entries[source[i].Key] = source[i];
            }
        }

        public string Get(string key, Language language)
        {
            if (key == null || !entries.TryGetValue(key, out BilingualTextEntry entry))
            {
                return key ?? string.Empty;
            }

            if (language == Language.Vietnamese && !string.IsNullOrEmpty(entry.Vietnamese))
            {
                return entry.Vietnamese;
            }

            return string.IsNullOrEmpty(entry.English) ? key : entry.English;
        }
    }
}
