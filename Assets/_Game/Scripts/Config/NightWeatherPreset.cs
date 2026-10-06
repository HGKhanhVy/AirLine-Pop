using System;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// One kind of weather a night flight can fly through: how thick the mist is, how hard
    /// it rains, and how often lightning strikes.
    /// </summary>
    [Serializable]
    public struct NightWeatherPreset
    {
        [Tooltip("For the designer only.")]
        public string name;

        [Tooltip("The first level this weather can come on, so the wilder weather waits for later flights.")]
        [Min(1)] public int fromLevel;

        [Range(0f, 1f)] public float fog;
        [Range(0f, 1f)] public float rain;

        [Tooltip("Seconds between lightning strikes, on average. 0 for none.")]
        [Min(0f)] public float lightningEvery;

        public bool HasLightning => lightningEvery > 0f;
    }
}
