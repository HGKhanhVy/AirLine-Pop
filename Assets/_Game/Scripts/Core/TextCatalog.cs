using System;
using System.Collections.Generic;

namespace ASTeams.SingleLine.Core
{
    public sealed class TextCatalog : ITextCatalog
    {
        private readonly Dictionary<string, string> texts;

        public TextCatalog(IReadOnlyList<LocalizedTextEntry> entries)
        {
            texts = new Dictionary<string, string>(entries.Count, StringComparer.Ordinal);
            for (int i = 0; i < entries.Count; i++)
            {
                texts.Add(entries[i].Key, entries[i].Text);
            }
        }

        public string Get(string key)
        {
            return texts.TryGetValue(key, out string text) ? text : key;
        }
    }
}
