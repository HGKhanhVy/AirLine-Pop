using System;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// One moment wired to one particle prefab from the reference art, with how many of
    /// it may be alive at once.
    ///
    /// Capacity is not a guess to be tuned away: a drag crosses squares faster than a
    /// burst finishes, so the pool has to hold several, while a win burst only ever needs
    /// one. Pooling both at the same size would either stutter or waste.
    /// </summary>
    [Serializable]
    public sealed class EffectBinding
    {
        [SerializeField] private GameplayEffect effect;

        [Tooltip("Particle prefab, taken from Asset_Resources. Leave empty to play nothing.")]
        [SerializeField] private ParticleSystem prefab;

        [Tooltip("How many bursts of this effect may run at the same time.")]
        [SerializeField, Min(1)] private int capacity = 4;

        [Tooltip("Tint the burst with the colour of what it came from, when the prefab allows it.")]
        [SerializeField] private bool isTinted = true;

        [Tooltip("Resizes the whole burst. The reference art was authored inside a block " +
                 "that carried its own scale, so a lifted effect usually needs one number " +
                 "here rather than an edit to the art.")]
        [SerializeField, Min(0.01f)] private float scale = 1f;

        public GameplayEffect Effect => effect;

        public ParticleSystem Prefab => prefab;

        public int Capacity => capacity;

        public bool IsTinted => isTinted;

        public float Scale => scale;
    }
}
