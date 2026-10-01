namespace ASTeams.SingleLine.Unity
{
    /// <summary>What the cat card shows about one regular right now.</summary>
    public readonly struct CatMenuModel
    {
        public CatMenuModel(string name, string tier, float tierProgress, string feedLabel, string status)
        {
            Name = name;
            Tier = tier;
            TierProgress = tierProgress;
            FeedLabel = feedLabel;
            Status = status;
        }

        public string Name { get; }

        /// <summary>Loyalty card, such as "Silver".</summary>
        public string Tier { get; }

        /// <summary>0 to 1 towards the next loyalty card.</summary>
        public float TierProgress { get; }

        public string FeedLabel { get; }

        /// <summary>A short line answering the last thing the player did; empty for none.</summary>
        public string Status { get; }
    }
}
