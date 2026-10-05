using System;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// One country's page in the passport: its map, and where the plane comes in from
    /// before the first city on it.
    /// </summary>
    [Serializable]
    public sealed class PassportPage
    {
        [SerializeField] private string region;
        [SerializeField] private Sprite map;

        [Tooltip("Where the route enters the page, 0..1 from the top left.")]
        [SerializeField] private Vector2 entry;

        public PassportPage(string region, Sprite map, Vector2 entry)
        {
            this.region = region;
            this.map = map;
            this.entry = entry;
        }

        public string RegionId => region;

        public string DisplayName => Localization.Get(Destination.RegionKey(region));

        public Sprite Map => map;

        public Vector2 Entry => entry;
    }
}
