using DG.Tweening;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Rocks a piece of scenery gently about its foot, as a tree or a windsock moves in the
    /// breeze: a looping tween on top of the rotation it was placed with.
    /// </summary>
    public sealed class SwayMotion : MonoBehaviour
    {
        [SerializeField] private float degrees = 2f;
        [SerializeField, Min(0.2f)] private float period = 2.6f;

        [Tooltip("Where in its swing the piece starts, 0..1, so neighbours do not sway in step.")]
        [SerializeField, Range(0f, 1f)] private float phase;

        private Quaternion rest;

        private void Awake()
        {
            rest = transform.localRotation;
        }

        private void OnEnable()
        {
            transform.localRotation = rest * Quaternion.Euler(0f, 0f, -degrees);
            transform.DOLocalRotateQuaternion(rest * Quaternion.Euler(0f, 0f, degrees), period * 0.5f)
                .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).Goto(phase * period * 0.5f, true);
        }

        private void OnDisable()
        {
            transform.DOKill();
            transform.localRotation = rest;
        }

#if UNITY_EDITOR
        public void EditorConfigure(float linkedDegrees, float linkedPeriod, float linkedPhase)
        {
            degrees = linkedDegrees;
            period = linkedPeriod;
            phase = linkedPhase;
        }
#endif
    }
}
