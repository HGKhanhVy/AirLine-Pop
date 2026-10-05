using ASTeams.SingleLine.Core;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The shape of the route map: how far apart levels sit, how widely the route swings,
    /// how long an international leg runs. Distances are in canvas units on a 1080-wide map.
    /// </summary>
    [CreateAssetMenu(menuName = "AirLine Pop/Route Map Config", fileName = "RouteMapConfig")]
    public sealed class RouteMapConfigSO : ScriptableObject
    {
        [SerializeField, Min(100f)] private float width = 1080f;
        [SerializeField, Min(40f)] private float levelSpacing = 300f;
        [SerializeField, Min(40f)] private float cityApproach = 300f;
        [SerializeField, Min(40f)] private float citySpacing = 800f;

        [Tooltip("From a city's last level to its airport card; keep it about half the city spacing.")]
        [SerializeField, Min(0f)] private float hubOffset = 400f;
        [SerializeField, Min(40f)] private float rowHeight = 620f;
        [SerializeField, Min(0f)] private float swing = 300f;
        [SerializeField, Min(0f)] private float swingJitter = 40f;
        [SerializeField, Min(40f)] private float crossingRise = 280f;
        [SerializeField, Min(0f)] private float edgeMargin = 90f;

        [Tooltip("Empty sky kept below the first level and above the last.")]
        [SerializeField, Min(0f)] private float padding = 420f;

        public float Width => width;

        public float Padding => padding;

        public RouteLayoutSettings ToSettings()
        {
            return new RouteLayoutSettings(width, levelSpacing, cityApproach, citySpacing, hubOffset, rowHeight, swing, swingJitter,
                crossingRise, edgeMargin);
        }
    }
}
