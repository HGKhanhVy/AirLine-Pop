namespace ASTeams.SingleLine.Core
{
    /// <summary>
    /// When a cat becomes a regular of the airline. Regulars are not bought: a cat joins
    /// once the player has flown past its arrival flight. Derived from flight progress, so
    /// replaying old flights or reinstalling can never lose or duplicate a regular.
    /// </summary>
    public static class RegularArrivals
    {
        /// <param name="arrivesAfterFlight">Flights that must be flown first; 0 means from the start.</param>
        /// <param name="nextFlight">The next flight the player will fly, 1-based.</param>
        public static bool IsDue(int arrivesAfterFlight, int nextFlight)
        {
            return nextFlight > arrivesAfterFlight;
        }
    }
}
