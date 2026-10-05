using System;
using ASTeams.SingleLine.Core;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// One city the airline flies to, and the postcard it gives. Its name, its country and
    /// its postcard's caption follow the chosen language. On the passport it is a round
    /// landmark pin at <see cref="MapPosition"/> on its country's page.
    /// </summary>
    [Serializable]
    public sealed class Destination
    {
        [SerializeField] private string id;
        [SerializeField] private string region;
        [SerializeField] private Sprite postcard;
        [SerializeField] private Sprite postcardVietnamese;
        [SerializeField] private Sprite pin;
        [SerializeField] private Sprite stamp;

        [Tooltip("The IATA code of the city's airport, such as DAD; empty where the city has none of its own.")]
        [SerializeField] private string airportCode;

        [Tooltip("Where the pin sits on the country's passport page, 0..1 from the top left.")]
        [SerializeField] private Vector2 mapPosition;

        public Destination(string id, string region, Sprite postcard, Sprite postcardVietnamese, Sprite pin, Vector2 mapPosition,
            string airportCode, Sprite stamp)
        {
            this.stamp = stamp;
            this.airportCode = airportCode;
            this.id = id;
            this.region = region;
            this.postcard = postcard;
            this.postcardVietnamese = postcardVietnamese;
            this.pin = pin;
            this.mapPosition = mapPosition;
        }

        public string Id => id;

        public string DisplayName => Localization.Get(NameKey(id));

        public string Region => Localization.Get(RegionKey(region));

        public string RegionId => region;

        public Sprite Pin => pin;

        /// <summary>The city's postage stamp in the stamp collection.</summary>
        public Sprite Stamp => stamp;

        public string AirportCode => airportCode;

        public Vector2 MapPosition => mapPosition;

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
