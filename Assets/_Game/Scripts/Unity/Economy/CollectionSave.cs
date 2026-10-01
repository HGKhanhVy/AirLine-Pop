using System;
using System.Collections.Generic;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Everything the cat-room economy saves besides the coin balance, which the SDK
    /// profile keeps (GDD 12: wallet and inventory, cat instances, equipment).
    /// </summary>
    [Serializable]
    public sealed class CollectionSave
    {
        public int saveVersion = 1;
        public bool isSeeded;
        public int foodCount;
        public List<CatSave> cats = new List<CatSave>();
        public List<string> visibleRoomCatIds = new List<string>();
        public string companionCatId;
        public List<string> ownedPlaneSkinIds = new List<string>();
        public List<string> ownedThemeIds = new List<string>();
        public string equippedPlaneSkinId;
        public string equippedThemeId;
    }
}
