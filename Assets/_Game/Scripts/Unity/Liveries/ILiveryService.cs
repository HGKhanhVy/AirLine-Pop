using System;
using System.Collections.Generic;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>Which liveries the player owns and which one the plane wears. Saved as it changes.</summary>
    public interface ILiveryService
    {
        IReadOnlyList<LiverySO> Liveries { get; }

        LiverySO Equipped { get; }

        event Action Changed;

        bool IsOwned(LiverySO livery);

        /// <summary>Pays for a livery and hands it over. False when the coins are not there.</summary>
        bool TryBuy(LiverySO livery);

        void Equip(LiverySO livery);
    }
}
