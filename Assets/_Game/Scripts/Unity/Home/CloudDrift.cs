using DG.Tweening;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>Slow side-to-side float for a scenery cloud; a looping tween, no per-frame script.</summary>
    public sealed class CloudDrift : MonoBehaviour
    {
        [SerializeField] private float distance = 0.8f;
        [SerializeField, Min(0.5f)] private float period = 9f;

        private float originX;

        private void Awake()
        {
            originX = transform.localPosition.x;
        }

        private void OnEnable()
        {
            Vector3 start = transform.localPosition;
            start.x = originX;
            transform.localPosition = start;
            transform.DOLocalMoveX(originX + distance, period * 0.5f)
                .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetDelay(Random.value * period * 0.5f);
        }

        private void OnDisable()
        {
            transform.DOKill();
        }

#if UNITY_EDITOR
        public void EditorConfigure(float linkedDistance, float linkedPeriod)
        {
            distance = linkedDistance;
            period = linkedPeriod;
        }
#endif
    }
}
