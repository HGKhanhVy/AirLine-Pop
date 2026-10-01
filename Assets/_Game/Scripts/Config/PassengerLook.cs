using System;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>One way a waiting cat can look: sitting, and happy as it hops aboard.</summary>
    [Serializable]
    public sealed class PassengerLook
    {
        [SerializeField] private Sprite sitting;

        [Tooltip("Eyes shut in a smile. Shown for blinks and for the hop into the plane.")]
        [SerializeField] private Sprite happy;

        public PassengerLook(Sprite sitting, Sprite happy)
        {
            this.sitting = sitting;
            this.happy = happy;
        }

        public Sprite Sitting => sitting;

        public Sprite Happy => happy;
    }
}
