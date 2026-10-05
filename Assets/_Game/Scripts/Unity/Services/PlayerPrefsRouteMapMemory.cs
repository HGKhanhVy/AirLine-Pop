using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Keeps the route map's last shown level on this device only. It only decides whether
    /// the plane animates, so losing it costs nothing but one replayed flight.
    /// </summary>
    public sealed class PlayerPrefsRouteMapMemory : IRouteMapMemory
    {
        private const string Key = "routeMapShownLevel";

        public int LastShownLevel => PlayerPrefs.GetInt(Key, 0);

        public void Remember(int levelNumber)
        {
            PlayerPrefs.SetInt(Key, levelNumber);
        }
    }
}
