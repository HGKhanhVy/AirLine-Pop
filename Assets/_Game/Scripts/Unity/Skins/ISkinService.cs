using System;
using System.Collections.Generic;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Which board looks the player owns and which one they wear (GDD 14: unlockedThemes,
    /// equippedTheme). Every change is saved as it happens.
    /// </summary>
    public interface ISkinService
    {
        IReadOnlyList<SkinSO> Skins { get; }

        SkinSO Equipped { get; }

        /// <summary>Raised when something is bought, granted or equipped.</summary>
        event Action Changed;

        bool IsOwned(SkinSO skin);

        /// <summary>Pays for a skin and hands it over. False when the coins are not there.</summary>
        bool TryBuy(SkinSO skin);

        /// <summary>Hands over a skin without payment, for a reward or a bundle.</summary>
        void Grant(SkinSO skin);

        void Equip(SkinSO skin);
    }
}
