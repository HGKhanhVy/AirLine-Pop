using System;
using ASTeams.SingleLine.Core;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// One city the airline flies to, and the postcard it gives. Its name, its country and
    /// its postcard's caption follow the chosen language.
    /// </summary>
    [Serializable]
    public sealed class Destination
    {
        [SerializeField] private string id;
        [SerializeField] private string region;
        [SerializeField] private Sprite postcard;
        [SerializeField] private Sprite postcardVietnamese;

        public Destination(string id, string region, Sprite postcard, Sprite postcardVietnamese)
        {
            this.id = id;
            this.region = region;
            this.postcard = postcard;
            this.postcardVietnamese = postcardVietnamese;
        }

        public string Id => id;

        public string DisplayName => Localization.Get(NameKey(id));

        public string Region => Localization.Get(RegionKey(region));

        public Sprite Postcard => Localization.Current == Language.Vietnamese && postcardVietnamese != null ? postcardVietnamese : postcard;

        public static string NameKey(string destinationId)
        {
            return "destination." + destinationId;
        }

        public static string RegionKey(string regionId)
        {
            return "region." + regionId;
        }
    }
}
