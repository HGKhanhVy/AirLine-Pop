using System;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Shows the daily flight on the airport once the campaign has gone far enough, and turns
    /// a press into a request the gameplay scene picks up.
    /// </summary>
    public sealed class DailyFlightPresenter : MonoBehaviour
    {
        [SerializeField] private DailyFlightButtonView view;

        private DailyFlightService service;

        /// <summary>Raised once the daily flight is asked for; the caller opens the gameplay scene.</summary>
        public event Action OnPlayRequested;

        public void Initialize(DailyFlightService dailyFlights, bool isUnlocked)
        {
            service = dailyFlights ?? throw new ArgumentNullException(nameof(dailyFlights));
            view.SetVisible(isUnlocked);

            if (!isUnlocked)
            {
                return;
            }

            if (service.IsFlownToday)
            {
                view.ShowDone(service.DayStreak);
            }
            else
            {
                view.ShowAvailable(service.RewardForToday, service.DayStreak);
            }
        }

        private void OnEnable()
        {
            view.OnPressed += HandlePressed;
        }

        private void OnDisable()
        {
            view.OnPressed -= HandlePressed;
        }

        private void HandlePressed()
        {
            if (service == null || service.IsFlownToday)
            {
                return;
            }

            DailyFlightRequest.Request();
            OnPlayRequested?.Invoke();
        }

#if UNITY_EDITOR
        public void EditorLink(DailyFlightButtonView linkedView)
        {
            view = linkedView;
        }
#endif
    }
}
