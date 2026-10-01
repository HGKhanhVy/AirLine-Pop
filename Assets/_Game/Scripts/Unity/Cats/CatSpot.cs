using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// A place in the room a cat can walk to for a reason: the yarn to play with, the carrier
    /// to nap in, the seats to rub against. A cat stops just to one side of it, facing it.
    /// </summary>
    public sealed class CatSpot : MonoBehaviour
    {
        [SerializeField] private CatSpotKind kind;

        [Tooltip("How far to the side of the spot a cat stands while using it.")]
        [SerializeField, Min(0f)] private float standOff = 0.45f;

        [Tooltip("Which side a cat stands on: -1 left, 1 right, 0 whichever side it comes from. " +
                 "The end of a row of seats only has room on its outer side.")]
        [SerializeField, Range(-1, 1)] private int side;

        [Tooltip("Optional. A toy that reacts when a cat bats it.")]
        [SerializeField] private YarnToyView toy;

        private object claimant;

        public CatSpotKind Kind => kind;

        /// <summary>One cat at a time: a second cat does not queue behind a napping one.</summary>
        public bool IsFree => claimant == null;

        public void Claim(object owner)
        {
            claimant = owner;
        }

        public void Unclaim(object owner)
        {
            if (claimant == owner)
            {
                claimant = null;
            }
        }

        public Vector3 Position => transform.position;

        /// <summary>Where a cat coming from <paramref name="from"/> stands to use the spot.</summary>
        public Vector3 StandingPoint(Vector3 from)
        {
            float direction = side != 0 ? side : (from.x >= transform.position.x ? 1f : -1f);
            return transform.position + new Vector3(direction * standOff, 0f, 0f);
        }

        /// <summary>A cat using the spot pokes it; a toy rolls away from the cat.</summary>
        public void Poke(Vector3 catPosition)
        {
            if (toy != null)
            {
                toy.Nudge(transform.position - catPosition);
            }
        }

#if UNITY_EDITOR
        public void EditorConfigure(CatSpotKind spotKind, float spotStandOff, int spotSide, YarnToyView spotToy)
        {
            kind = spotKind;
            standOff = spotStandOff;
            side = spotSide;
            toy = spotToy;
        }
#endif
    }
}
