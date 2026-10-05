using DG.Tweening;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Another of the airline's planes taking off on the far runway now and then: it rolls
    /// in along the stretch of runway seen beside the terminal, lifts its nose and climbs
    /// away over the terminal roof into the sky, then the next one comes a while later.
    /// Moves only its own transform, along its local x and y.
    /// </summary>
    public sealed class TakeoffLoop : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private float rollFrom = 9f;
        [SerializeField] private float liftAt = 2.6f;
        [SerializeField] private float climbTo = -8f;
        [SerializeField] private float climbHeight = 7.5f;
        [SerializeField, Min(0.1f)] private float rollSeconds = 3.2f;
        [SerializeField, Min(0.1f)] private float climbSeconds = 3.4f;
        [SerializeField, Min(0f)] private float noseUpDegrees = 10f;
        [SerializeField] private Vector2 waitSeconds = new Vector2(8f, 14f);

        private Vector3 home;
        private Quaternion rest;
        private Sequence flight;
        private int turn;

        private void Awake()
        {
            home = transform.localPosition;
            rest = transform.localRotation;
        }

        private void OnEnable()
        {
            body.enabled = false;
            Schedule(waitSeconds.x * 0.5f);
        }

        private void OnDisable()
        {
            flight?.Kill();
            transform.localPosition = home;
            transform.localRotation = rest;
        }

        private void Schedule(float wait)
        {
            flight = DOTween.Sequence().AppendInterval(wait).AppendCallback(TakeOff);
        }

        private void TakeOff()
        {
            turn++;
            transform.localPosition = new Vector3(home.x + rollFrom, home.y, home.z);
            transform.localRotation = rest;
            body.enabled = true;

            // The art faces left, so lifting the nose turns it clockwise.
            Quaternion noseUp = rest * Quaternion.Euler(0f, 0f, -noseUpDegrees);
            flight = DOTween.Sequence()
                .Append(transform.DOLocalMoveX(home.x + liftAt, rollSeconds).SetEase(Ease.InQuad))
                .Append(transform.DOLocalMoveX(home.x + climbTo, climbSeconds).SetEase(Ease.Linear))
                .Join(transform.DOLocalMoveY(home.y + climbHeight, climbSeconds).SetEase(Ease.InQuad))
                .Join(transform.DOLocalRotateQuaternion(noseUp, climbSeconds * 0.3f))
                .AppendCallback(() => body.enabled = false)
                .AppendCallback(() => Schedule(Mathf.Lerp(waitSeconds.x, waitSeconds.y, (turn * 37 % 10) / 10f)));
        }

#if UNITY_EDITOR
        public void EditorLink(SpriteRenderer linkedBody)
        {
            body = linkedBody;
        }
#endif
    }
}
