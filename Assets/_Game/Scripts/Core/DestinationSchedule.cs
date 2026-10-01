using System;

namespace ASTeams.SingleLine.Core
{
    /// <summary>
    /// Which destination each flight goes to, and how far along each postcard is.
    ///
    /// Flights are numbered from 1 and flown in order, and each destination takes a fixed
    /// run of consecutive flights. Every flight stamps the passport once; the last stamp of
    /// a destination earns its postcard. Nothing here is saved: with a linear campaign the
    /// next flight to play is enough to know every stamp and postcard, so the collection
    /// can never drift out of step with progress.
    /// </summary>
    public sealed class DestinationSchedule
    {
        public DestinationSchedule(int destinationCount, int flightsPerDestination)
        {
            if (destinationCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(destinationCount));
            }

            if (flightsPerDestination < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(flightsPerDestination));
            }

            DestinationCount = destinationCount;
            FlightsPerDestination = flightsPerDestination;
        }

        public int DestinationCount { get; }

        public int FlightsPerDestination { get; }

        /// <summary>
        /// The destination a flight goes to. Flights past the last destination keep going
        /// to it, so a campaign longer than the catalog still has somewhere to fly.
        /// </summary>
        public int DestinationOf(int flightNumber)
        {
            int index = (Math.Max(1, flightNumber) - 1) / FlightsPerDestination;
            return Math.Min(index, DestinationCount - 1);
        }

        /// <summary>Which stamp, 1 to <see cref="FlightsPerDestination"/>, this flight earns.</summary>
        public int StampOf(int flightNumber)
        {
            int flown = Math.Max(1, flightNumber) - FirstFlight(DestinationOf(flightNumber));
            return Math.Min(flown + 1, FlightsPerDestination);
        }

        /// <summary>True when this flight earns its destination's last stamp, and so the postcard.</summary>
        public bool CompletesPostcard(int flightNumber)
        {
            return flightNumber == LastFlight(DestinationOf(flightNumber));
        }

        public int FirstFlight(int destination)
        {
            return destination * FlightsPerDestination + 1;
        }

        public int LastFlight(int destination)
        {
            return (destination + 1) * FlightsPerDestination;
        }

        /// <summary>Stamps collected for a destination, given the next flight the player will fly.</summary>
        public int StampsCollected(int destination, int nextFlight)
        {
            int flown = nextFlight - FirstFlight(destination);
            return Math.Max(0, Math.Min(FlightsPerDestination, flown));
        }

        public bool HasPostcard(int destination, int nextFlight)
        {
            return nextFlight > LastFlight(destination);
        }
    }
}
