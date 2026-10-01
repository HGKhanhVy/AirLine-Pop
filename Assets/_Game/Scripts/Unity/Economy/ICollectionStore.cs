namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Holds the collection save and writes it together with a coin change, so a
    /// purchase can never charge without delivering or deliver without charging.
    /// </summary>
    public interface ICollectionStore
    {
        CollectionSave State { get; }

        long Coins { get; }

        /// <summary>
        /// Applies <paramref name="coinDelta"/> and writes the balance and the current
        /// <see cref="State"/> in one save. Callers validate the balance first.
        /// </summary>
        void Commit(long coinDelta);
    }
}
