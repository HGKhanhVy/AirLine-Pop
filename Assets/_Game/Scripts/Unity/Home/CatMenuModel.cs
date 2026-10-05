namespace ASTeams.SingleLine.Unity
{
    /// <summary>What the cat menu shows about one regular right now.</summary>
    public readonly struct CatMenuModel
    {
        public CatMenuModel(string name, string tier, float tierProgress, string feedLabel, int foodCount, string status)
        {
            Name = name;
            Tier = tier;
            TierProgress = tierProgress;
            FeedLabel = feedLabel;
            FoodCount = foodCount;
            Status = status;
        }

        public string Name { get; }

        /// <summary>Loyalty card, such as "Silver".</summary>
        public string Tier { get; }

        /// <summary>0 to 1 towards the next loyalty card.</summary>
        public float TierProgress { get; }

        /// <summary>The snack button's short label: "Snack", or what buying one costs when none are left.</summary>
        public string FeedLabel { get; }

        /// <summary>Snacks left, shown as a badge on the snack button; none hides the badge.</summary>
        public int FoodCount { get; }

        /// <summary>A short line answering the last thing the player did; empty for none.</summary>
        public string Status { get; }
    }
}
