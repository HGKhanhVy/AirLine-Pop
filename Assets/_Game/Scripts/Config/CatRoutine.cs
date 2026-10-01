using System;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// One thing a cat does of its own accord: which pose, how often, for how long, and
    /// whether it walks somewhere first (to the yarn, the carrier, the seats).
    /// </summary>
    [Serializable]
    public sealed class CatRoutine
    {
        [SerializeField] private string label = "Routine";
        [SerializeField] private CatPose pose = CatPose.Wait;
        [SerializeField, Min(0f)] private float weight = 1f;
        [SerializeField] private Vector2 duration = new Vector2(3f, 6f);

        [Tooltip("Where the cat goes first. None does it on the spot; a room without that spot skips the routine.")]
        [SerializeField] private CatSpotKind spot = CatSpotKind.None;

        [Tooltip("Seconds between pokes at the spot while posing, such as batting the yarn. 0 never pokes.")]
        [SerializeField, Min(0f)] private float pokeEvery;

        public CatRoutine(string label, CatPose pose, float weight, Vector2 duration, CatSpotKind spot, float pokeEvery)
        {
            this.label = label;
            this.pose = pose;
            this.weight = weight;
            this.duration = duration;
            this.spot = spot;
            this.pokeEvery = pokeEvery;
        }

        public string Label => label;

        public CatPose Pose => pose;

        public float Weight => weight;

        public Vector2 Duration => duration;

        public CatSpotKind Spot => spot;

        public float PokeEvery => pokeEvery;
    }
}
