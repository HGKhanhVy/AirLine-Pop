using System;

namespace ASTeams.SingleLine.Core
{
    /// <summary>One piece of player-facing text, written in each language the game supports.</summary>
    [Serializable]
    public sealed class BilingualTextEntry
    {
        public string Key;
        public string English;
        public string Vietnamese;
    }
}
