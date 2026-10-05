using DG.Tweening;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Floats a piece of the loading screen up and down and rocks it about its pivot, such
    /// as the balloon drifting or the sun nodding, only while the screen is up.
    /// </summary>
    public sealed class LoadingBob : LoadingAnimation
    {
        [SerializeField] private RectTransform piece;
        [SerializeField] private float height = 24f;
        [SerializeField] private float degrees = 4f;
        [SerializeField, Min(0.2f)] private float seconds = 1.8f;

        private Vector2 rest;
        private bool hasRest;

        public override void Play()
        {
            if (!hasRest)
            {
                rest = piece.anchoredPosition;
                hasRest = true;
            }

            piece.DOAnchorPosY(rest.y + height, seconds).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
            piece.localRotation = Quaternion.Euler(0f, 0f, -degrees);
            piece.DOLocalRotate(new Vector3(0f, 0f, degrees), seconds * 1.3f).SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
        }

        public override void Stop()
        {
            piece.DOKill();

            if (hasRest)
            {
                piece.anchoredPosition = rest;
            }

            piece.localRotation = Quaternion.identity;
        }

#if UNITY_EDITOR
        public void EditorConfigure(RectTransform linkedPiece, float linkedHeight, float linkedDegrees, float linkedSeconds)
        {
            piece = linkedPiece;
            height = linkedHeight;
            degrees = linkedDegrees;
            seconds = linkedSeconds;
        }
#endif
    }
}
