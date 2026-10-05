using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// A city's airport on the route map, where the plane lands after the city's last gate:
    /// its IATA code on the header, the city's landmark and name, and how many of the flights
    /// there are done. Once landed it carries the arrival stamp; before the route reaches it,
    /// it waits greyed out behind a padlock.
    /// </summary>
    public sealed class HubCardView : MonoBehaviour
    {
        [SerializeField] private RectTransform rect;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image pin;
        [SerializeField] private TMP_Text codeLabel;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text progressLabel;
        [SerializeField] private GameObject arrivalStamp;
        [SerializeField] private GameObject lockBadge;

        [SerializeField] private Color lockedTint = new Color(0.7f, 0.74f, 0.8f, 1f);
        [SerializeField, Range(0f, 1f)] private float lockedAlpha = 0.85f;

        private Destination destination;
        private int flown = -1;
        private RouteStopState state;

        public int City { get; private set; } = -1;

        private void OnDisable()
        {
            City = -1;
            arrivalStamp.transform.DOKill();
        }

        public void Show(int city, Destination airport, int flightsFlown, int flightsTotal, RouteStopState hubState, Vector2 position)
        {
            rect.anchoredPosition = position;

            if (city == City && airport == destination && flightsFlown == flown && hubState == state)
            {
                return;
            }

            City = city;
            destination = airport;
            flown = flightsFlown;
            state = hubState;

            bool isLocked = hubState == RouteStopState.Locked;
            pin.sprite = airport != null ? airport.Pin : null;
            pin.color = isLocked ? lockedTint : Color.white;
            group.alpha = isLocked ? lockedAlpha : 1f;
            lockBadge.SetActive(isLocked);
            arrivalStamp.SetActive(hubState == RouteStopState.Flown);

            string code = airport != null ? airport.AirportCode : string.Empty;
            codeLabel.gameObject.SetActive(!string.IsNullOrEmpty(code));
            codeLabel.text = code;
            nameLabel.text = airport != null ? airport.DisplayName : string.Empty;
            progressLabel.SetText(Localization.Get("map.hubProgress"), flightsFlown, flightsTotal);
        }

        /// <summary>The stamp lands on the card: played when the plane has just arrived here.</summary>
        public void PlayArrival()
        {
            arrivalStamp.SetActive(true);
            Transform stamp = arrivalStamp.transform;
            stamp.DOKill();
            stamp.localScale = Vector3.one * 2f;
            stamp.DOScale(1f, 0.35f).SetEase(Ease.OutBounce).SetUpdate(true);
        }

        /// <summary>Forgets what is shown, so the next <see cref="Show"/> redraws: used when the language changes.</summary>
        public void Invalidate()
        {
            City = -1;
        }

#if UNITY_EDITOR
        public void EditorLink(RectTransform linkedRect, CanvasGroup linkedGroup, Image linkedPin, TMP_Text linkedCode, TMP_Text linkedName,
            TMP_Text linkedProgress, GameObject linkedStamp, GameObject linkedLock)
        {
            rect = linkedRect;
            group = linkedGroup;
            pin = linkedPin;
            codeLabel = linkedCode;
            nameLabel = linkedName;
            progressLabel = linkedProgress;
            arrivalStamp = linkedStamp;
            lockBadge = linkedLock;
        }
#endif
    }
}
