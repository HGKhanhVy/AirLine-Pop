using System;
using System.Collections.Generic;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>The accessories regulars can wear, which of them a cat has unlocked and what it has on.</summary>
    public interface ICatWardrobeService
    {
        /// <summary>Raised when a cat puts something on or takes it off: the cat's id.</summary>
        event Action<string> OnWornChanged;

        IReadOnlyList<AccessorySO> Accessories { get; }

        bool IsUnlocked(string catId, AccessorySO accessory);

        /// <summary>What the cat wears now; null for nothing.</summary>
        AccessorySO GetWorn(string catId);

        /// <summary>Puts an unlocked accessory on the cat, or takes it off with null. False when it is still locked.</summary>
        bool Wear(string catId, AccessorySO accessory);
    }
}
