using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>Free-roam timings and movement for cats (GDD 4, 11). Playtest starting points.</summary>
    [CreateAssetMenu(menuName = "AirLine Pop/Cat Behaviour Config", fileName = "CatBehaviourConfig")]
    public sealed class CatBehaviourConfigSO : ScriptableObject
    {
        [Header("Movement")]
        [SerializeField, Min(0.05f)] private float walkSpeed = 0.55f;
        [SerializeField, Min(10f)] private float turnSpeed = 300f;

        [Tooltip("A cat only starts walking once it faces within this many degrees of its target.")]
        [SerializeField, Range(1f, 90f)] private float facingTolerance = 20f;

        [SerializeField, Min(0.1f)] private float personalSpace = 0.7f;

        [Tooltip("Metres per second the Walk clip was authored at, so feet do not slide.")]
        [SerializeField, Min(0.05f)] private float walkClipSpeed = 0.55f;

        [SerializeField, Min(0.5f)] private float giveUpAfter = 5f;

        [Header("Behaviour lengths (seconds)")]
        [SerializeField] private Vector2 idleDuration = new Vector2(2f, 5f);

        [Header("Choice weights (routines below carry their own)")]
        [SerializeField, Min(0f)] private float wanderWeight = 5f;
        [SerializeField, Min(0f)] private float idleWeight = 3f;

        [Header("Routines")]
        [Tooltip("Things a cat does of its own accord, picked by weight alongside wandering and idling.")]
        [SerializeField] private CatRoutine[] routines =
        {
            new CatRoutine("Waiting", CatPose.Wait, 2f, new Vector2(3f, 6f), CatSpotKind.None, 0f),
            new CatRoutine("Sightseeing", CatPose.Wait, 1.5f, new Vector2(3f, 6f), CatSpotKind.Furniture, 0f),
            new CatRoutine("Rolling", CatPose.Play, 1.6f, new Vector2(2f, 3.5f), CatSpotKind.Toy, 0.6f),
            new CatRoutine("Hopping", CatPose.Hop, 0.8f, new Vector2(1.6f, 2.4f), CatSpotKind.None, 0f),
            new CatRoutine("Dizzy", CatPose.Dizzy, 0.3f, new Vector2(2f, 2.5f), CatSpotKind.None, 0f),
        };

        public System.Collections.Generic.IReadOnlyList<CatRoutine> Routines => routines;

        public float WalkSpeed => walkSpeed;

        public float TurnSpeed => turnSpeed;

        public float FacingTolerance => facingTolerance;

        public float PersonalSpace => personalSpace;

        public float WalkClipSpeed => walkClipSpeed;

        public float GiveUpAfter => giveUpAfter;

        public Vector2 IdleDuration => idleDuration;

        public float WanderWeight => wanderWeight;

        public float IdleWeight => idleWeight;
    }
}
