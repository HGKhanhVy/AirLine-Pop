namespace ASTeams.SingleLine.Unity
{
    /// <summary>What a shop card's button does now.</summary>
    public enum ShopCardState
    {
        /// <summary>Not owned yet, or a snack pack: pay to get it.</summary>
        Buy = 0,

        /// <summary>Owned but not worn: put it on.</summary>
        Use = 1,

        /// <summary>Owned and worn: nothing to do.</summary>
        InUse = 2,
    }
}
