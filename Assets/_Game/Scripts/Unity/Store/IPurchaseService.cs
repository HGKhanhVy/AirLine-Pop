using System;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Buying and restoring store products. Every callback fires exactly once.
    /// </summary>
    public interface IPurchaseService
    {
        bool IsOwned(StoreProduct product);

        /// <summary>The store's localized price, or a fallback while the store is not ready.</summary>
        string GetPrice(StoreProduct product);

        void Buy(StoreProduct product, Action<bool> onFinished);

        /// <summary>Reports true when something owned was found and applied.</summary>
        void Restore(Action<bool> onFinished);
    }
}
