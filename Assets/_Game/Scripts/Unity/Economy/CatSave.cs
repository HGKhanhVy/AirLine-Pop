using System;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Saved state of one owned cat (GDD 12: catId, name, bondXP, interactionDay,
    /// dailyCareCounters). Plain fields so the profile's JSON writer can store it.
    /// </summary>
    [Serializable]
    public sealed class CatSave
    {
        public string catId;
        public string name;
        public int bondXP;

        /// <summary>Day number the daily counters below belong to; a new day resets them.</summary>
        public int interactionDay;
        public bool hasPettedToday;
        public bool hasPlayedToday;
        public int mealsToday;

        /// <summary>Day number this cat last handed over its daily gift.</summary>
        public int giftDay;

        /// <summary>The accessory it wears, by id; empty for none.</summary>
        public string accessoryId;
    }
}
