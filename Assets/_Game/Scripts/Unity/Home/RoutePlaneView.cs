using System;
using DG.Tweening;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The airline's plane on a map of the route. It sits where the player has got to and,
    /// after a win, flies along the route to the next hop, turned to face where it goes.
    /// A flight takes time in proportion to its length, within a floor and a ceiling, so a
    /// hop between levels is quick and an international leg feels long.
    /// </summary>
    public sealed class RoutePlaneView : MonoBehaviour
    {
        [SerializeField] private RectTransform rect;

        [Tooltip("Which way the plane's picture points, in degrees: 0 for nose right, 90 for nose up.")]
        [SerializeField] private float artHeading;
        [SerializeField, Min(1f)] private float speed = 700f;
        [SerializeField, Min(0.05f)] private float shortestFlight = 0.45f;
        [SerializeField, Min(0.05f)] private float longestFlight = 3.2f;

        private Sequence flight;

        public bool IsFlying => flight != null && flight.IsActive() && flight.IsPlaying();

        public void Hide()
        {
            Stop();
            gameObject.SetActive(false);
        }

        public void Place(Vector2 position, Vector2 heading)
        {
            Stop();
            gameObject.SetActive(true);
            rect.anchoredPosition = position;
            Face(heading);
        }

        /// <summary>
        /// Waits, flies through the first <paramref name="count"/> points in order, then calls
        /// back. Returns how long the flight itself lasts, without the wait.
        /// </summary>
        public float Fly(Vector2[] path, int count, float delay, Action onLanded)
        {
            Stop();
            gameObject.SetActive(true);
            rect.anchoredPosition = path[0];
            Face(path[1] - path[0]);

            float length = 0f;

            for (int i = 1; i < count; i++)
            {
                length += Vector2.Distance(path[i - 1], path[i]);
            }

            float duration = Mathf.Clamp(length / speed, shortestFlight, longestFlight);
            flight = DOTween.Sequence().SetUpdate(true).AppendInterval(delay);

            for (int i = 1; i < count; i++)
            {
                Vector2 from = path[i - 1];
                Vector2 to = path[i];
                float hop = length > 0f ? duration * Vector2.Distance(from, to) / length : duration / (count - 1);
                flight.AppendCallback(() => Face(to - from));
                flight.Append(rect.DOAnchorPos(to, hop).SetEase(Ease.Linear));
            }

            flight.OnComplete(() => onLanded?.Invoke());
            return duration;
        }

        private void Face(Vector2 heading)
        {
            if (heading.sqrMagnitude > 0.01f)
            {
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg - artHeading);
            }
        }

        private void Stop()
        {
            flight?.Kill();
            flight = null;
        }

        private void OnDisable()
        {
            Stop();
        }

#if UNITY_EDITOR
        public void EditorLink(RectTransform linkedRect, float linkedArtHeading = 0f)
        {
            rect = linkedRect;
            artHeading = linkedArtHeading;
        }
#endif
    }
}
