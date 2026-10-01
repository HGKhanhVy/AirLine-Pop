using System.Text;
using TMPro;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The departures board on the lounge wall: the next few flights the player will fly,
    /// with where they go. The next one is boarding; the rest are on time.
    /// </summary>
    public sealed class DeparturesBoardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text rows;
        [SerializeField, Range(1, 5)] private int flightsShown = 3;

        [Tooltip("{0} flight number, {1} city (upper case), {2} status. Column stops use TMP <pos> tags.")]
        [SerializeField] private string rowFormat = "AP {0:000}<pos=30%>{1}<pos=64%>{2}";

        [SerializeField] private string boardingColour = "#F6B8BC";
        [SerializeField] private string onTimeColour = "#C4E6D6";

        private DestinationCatalogSO catalog;
        private int firstFlight;

        private void OnEnable()
        {
            Localization.Service.OnLanguageChanged += Render;
        }

        private void OnDisable()
        {
            Localization.Service.OnLanguageChanged -= Render;
        }

        public void Show(DestinationCatalogSO destinations, int nextFlight)
        {
            catalog = destinations;
            firstFlight = nextFlight;
            Render();
        }

        private void Render()
        {
            DestinationCatalogSO destinations = catalog;
            int nextFlight = firstFlight;
            string boarding = "<color=" + boardingColour + ">" + Localization.Get("departures.boarding") + "</color>";
            string onTime = "<color=" + onTimeColour + ">" + Localization.Get("departures.onTime") + "</color>";

            if (destinations == null)
            {
                rows.text = string.Empty;
                return;
            }

            var text = new StringBuilder(160);

            for (int i = 0; i < flightsShown; i++)
            {
                int flight = nextFlight + i;
                Destination destination = destinations.ForFlight(flight);
                string city = destination != null ? destination.DisplayName.ToUpperInvariant() : string.Empty;

                if (i > 0)
                {
                    text.Append('\n');
                }

                text.AppendFormat(rowFormat, flight, city, i == 0 ? boarding : onTime);
            }

            rows.text = text.ToString();
        }

#if UNITY_EDITOR
        public void EditorLink(TMP_Text linkedRows)
        {
            rows = linkedRows;
        }
#endif
    }
}
