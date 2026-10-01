namespace ASTeams.SingleLine.Unity
{
    /// <summary>What a care action did (GDD 5).</summary>
    public enum CareOutcome
    {
        /// <summary>Done, and it counted towards today's bond.</summary>
        BondGained,

        /// <summary>Done, but today's bond for this action is already used up.</summary>
        NoBond,

        /// <summary>Not done: no food left.</summary>
        NoFood,

        /// <summary>Not done: the cat already had today's rewarded meals.</summary>
        AlreadyFed,

        Invalid,
    }
}
