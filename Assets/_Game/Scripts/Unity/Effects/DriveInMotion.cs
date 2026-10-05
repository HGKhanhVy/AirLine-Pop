using DG.Tweening;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// A vehicle drives out onto the apron once and parks where it was placed, like the
    /// baggage train pulling up beside the plane. It starts a little way off along its
    /// local x and keeps facing the way it drives; it never turns round.
    /// </summary>
    public sealed class DriveInMotion : MonoBehaviour
    {
        [Tooltip("How far back along local x the drive starts; the art is drawn driving to the left, so a positive start comes from the right.")]
        [SerializeField] private float startOffset = 2.4f;

        [SerializeField, Min(0.1f)] private float driveSeconds = 3.5f;
        [SerializeField, Min(0f)] private float delaySeconds = 1.2f;

        private Vector3 parked;

        private void Awake()
        {
            parked = transform.localPosition;
        }

        private void OnEnable()
        {
            transform.localPosition = parked + new Vector3(startOffset, 0f, 0f);
            transform.DOLocalMoveX(parked.x, driveSeconds).SetDelay(delaySeconds).SetEase(Ease.OutSine);
        }

        private void OnDisable()
        {
            transform.DOKill();
            transform.localPosition = parked;
        }

#if UNITY_EDITOR
        public void EditorConfigure(float linkedStartOffset)
        {
            startOffset = linkedStartOffset;
        }
#endif
    }
}
