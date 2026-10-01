using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>Everything the win card says about the flight that just took off.</summary>
    public readonly struct WinFlightSummary
    {
        public WinFlightSummary(int flightNumber, int coins, int passengers, string destination, int stamp,
            int stampsNeeded, Sprite earnedPostcard)
        {
            FlightNumber = flightNumber;
            Coins = coins;
            Passengers = passengers;
            Destination = destination;
            Stamp = stamp;
            StampsNeeded = stampsNeeded;
            EarnedPostcard = earnedPostcard;
        }

        public int FlightNumber { get; }

        public int Coins { get; }

        public int Passengers { get; }

        /// <summary>City name, or null when no route map is set up.</summary>
        public string Destination { get; }

        /// <summary>Which stamp this flight earned for its destination, 1 to <see cref="StampsNeeded"/>.</summary>
        public int Stamp { get; }

        public int StampsNeeded { get; }

        /// <summary>The destination's postcard when this flight completed it; otherwise null.</summary>
        public Sprite EarnedPostcard { get; }
    }
}
