using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The visible cat: its animator, and the id it answers to when tapped. Baked onto
    /// every cat prefab by the builder so nothing is looked up at runtime.
    /// </summary>
    public sealed class CatView : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Collider tapCollider;

        [Tooltip("Optional. Turns a flat cat to face its viewer; a modelled cat leaves it empty.")]
        [SerializeField] private CatBillboard billboard;

        public string CatId { get; private set; }

        public Transform Root => transform;

        public Collider TapCollider => tapCollider;

        public void Bind(string catId)
        {
            CatId = catId;
        }

        public void SetSpeed(float speed)
        {
            animator.SetFloat(CatAnimatorParams.Speed, speed);
        }

        /// <summary>Scales animation playback so the walk cycle matches the ground speed.</summary>
        public void SetPlaybackRate(float rate)
        {
            animator.speed = rate;
        }

        public void Trigger(int trigger)
        {
            animator.SetTrigger(trigger);
        }

        /// <summary>Holds a pose (sleeping, grooming, sitting…) until another replaces it.</summary>
        public void SetPose(CatPose pose)
        {
            animator.SetInteger(CatAnimatorParams.Action, (int)pose);
        }

        /// <summary>Names the camera looking at this cat, for a flat cat that has to face it.</summary>
        public void SetViewer(Transform viewer)
        {
            if (billboard != null)
            {
                billboard.SetViewer(viewer);
            }
        }

#if UNITY_EDITOR
        public void EditorLink(Animator linkedAnimator, Collider linkedCollider, CatBillboard linkedBillboard = null)
        {
            animator = linkedAnimator;
            tapCollider = linkedCollider;
            billboard = linkedBillboard;
        }
#endif
    }
}
