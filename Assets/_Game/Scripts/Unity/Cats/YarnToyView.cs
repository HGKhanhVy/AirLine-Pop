using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The yarn ball: a batted ball rolls a little way and spins, then settles back to where
    /// it lives, so a cat can play with it forever without it wandering off the floor.
    /// Only ticks while it is moving.
    /// </summary>
    public sealed class YarnToyView : MonoBehaviour
    {
        [SerializeField] private Transform ball;

        [SerializeField, Min(0f)] private float nudgeDistance = 0.18f;
        [SerializeField, Min(0.05f)] private float rollSeconds = 0.35f;
        [SerializeField, Min(0.05f)] private float returnSeconds = 1.2f;
        [SerializeField] private float spinDegrees = 220f;

        private Vector3 home;
        private Vector3 rolledTo;
        private float elapsed = -1f;
        private float spinDirection = 1f;

        private void Awake()
        {
            home = ball.localPosition;
            enabled = false;
        }

        public void Nudge(Vector3 worldDirection)
        {
            Vector3 flat = new Vector3(worldDirection.x, 0f, 0f);
            spinDirection = flat.x >= 0f ? -1f : 1f;
            rolledTo = home + ball.parent.InverseTransformDirection(flat.normalized) * nudgeDistance;
            elapsed = 0f;
            enabled = true;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float total = rollSeconds + returnSeconds;

            if (elapsed >= total)
            {
                ball.localPosition = home;
                elapsed = -1f;
                enabled = false;
                return;
            }

            float outT = Mathf.Clamp01(elapsed / rollSeconds);
            float backT = Mathf.Clamp01((elapsed - rollSeconds) / returnSeconds);
            float outEase = 1f - (1f - outT) * (1f - outT);
            float backEase = backT * backT * (3f - 2f * backT);
            ball.localPosition = Vector3.Lerp(Vector3.Lerp(home, rolledTo, outEase), home, backEase);
            ball.localRotation = ball.localRotation * Quaternion.Euler(0f, 0f, spinDirection * spinDegrees * Time.deltaTime * (1f - backEase));
        }

#if UNITY_EDITOR
        public void EditorLink(Transform linkedBall)
        {
            ball = linkedBall;
        }
#endif
    }
}
