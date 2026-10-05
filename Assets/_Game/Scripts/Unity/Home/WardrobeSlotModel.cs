using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>What one slot of the wardrobe picker shows.</summary>
    public readonly struct WardrobeSlotModel
    {
        public WardrobeSlotModel(Sprite icon, string label, bool isUnlocked, bool isWorn)
        {
            Icon = icon;
            Label = label;
            IsUnlocked = isUnlocked;
            IsWorn = isWorn;
        }

        /// <summary>The accessory's picture; null for the "nothing" slot.</summary>
        public Sprite Icon { get; }

        /// <summary>Its name when unlocked, or the loyalty card that unlocks it.</summary>
        public string Label { get; }

        public bool IsUnlocked { get; }

        public bool IsWorn { get; }
    }
}
