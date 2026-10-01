using ASTeams.SingleLine.Core;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Every destination in flying order. Each takes <see cref="FlightsPerDestination"/>
    /// consecutive flights; adding destinations here extends the route map.
    /// </summary>
    [CreateAssetMenu(menuName = "AirLine Pop/Destination Catalog", fileName = "DestinationCatalog")]
    public sealed class DestinationCatalogSO : ScriptableObject
    {
        [SerializeField] private Destination[] destinations = new Destination[0];
        [SerializeField, Min(1)] private int flightsPerDestination = 10;

        private DestinationSchedule schedule;

        public int Count => destinations.Length;

        public int FlightsPerDestination => flightsPerDestination;

        public DestinationSchedule Schedule =>
            schedule ??= new DestinationSchedule(Mathf.Max(1, destinations.Length), flightsPerDestination);

        public Destination Get(int index)
        {
            return destinations.Length == 0 ? null : destinations[Mathf.Clamp(index, 0, destinations.Length - 1)];
        }

        public Destination ForFlight(int flightNumber)
        {
            return Get(Schedule.DestinationOf(flightNumber));
        }

        private void OnValidate()
        {
            schedule = null;
        }

#if UNITY_EDITOR
        public void EditorSetDestinations(Destination[] newDestinations, int flights)
        {
            destinations = newDestinations;
            flightsPerDestination = flights;
            schedule = null;
        }
#endif
    }
}
