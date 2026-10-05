using ASTeams.SingleLine.Core;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Every destination in flying order. Each takes <see cref="FlightsPerDestination"/>
    /// consecutive flights; adding destinations here extends the route map. Consecutive
    /// destinations in one country share a passport page.
    /// </summary>
    [CreateAssetMenu(menuName = "AirLine Pop/Destination Catalog", fileName = "DestinationCatalog")]
    public sealed class DestinationCatalogSO : ScriptableObject
    {
        [SerializeField] private Destination[] destinations = new Destination[0];
        [SerializeField, Min(1)] private int flightsPerDestination = 10;

        [Tooltip("One page per country, in the order the countries are flown.")]
        [SerializeField] private PassportPage[] pages = new PassportPage[0];

        private DestinationSchedule schedule;
        private RouteRegions regions;

        public int Count => destinations.Length;

        public int FlightsPerDestination => flightsPerDestination;

        public DestinationSchedule Schedule =>
            schedule ??= new DestinationSchedule(Mathf.Max(1, destinations.Length), flightsPerDestination);

        public RouteRegions Regions => regions ??= BuildRegions();

        public Destination Get(int index)
        {
            return destinations.Length == 0 ? null : destinations[Mathf.Clamp(index, 0, destinations.Length - 1)];
        }

        public Destination ForFlight(int flightNumber)
        {
            return Get(Schedule.DestinationOf(flightNumber));
        }

        /// <summary>The passport page for a page index of <see cref="Regions"/>, or null if none was installed.</summary>
        public PassportPage GetPage(int page)
        {
            Destination first = Get(Regions.FirstDestination(page));

            for (int i = 0; i < pages.Length; i++)
            {
                if (pages[i].RegionId == first.RegionId)
                {
                    return pages[i];
                }
            }

            return null;
        }

        private RouteRegions BuildRegions()
        {
            var ids = new string[destinations.Length];

            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = destinations[i].RegionId;
            }

            return new RouteRegions(ids);
        }

        private void OnValidate()
        {
            schedule = null;
            regions = null;
        }

#if UNITY_EDITOR
        public void EditorSetDestinations(Destination[] newDestinations, int flights, PassportPage[] newPages)
        {
            destinations = newDestinations;
            flightsPerDestination = flights;
            pages = newPages;
            schedule = null;
            regions = null;
        }
#endif
    }
}
