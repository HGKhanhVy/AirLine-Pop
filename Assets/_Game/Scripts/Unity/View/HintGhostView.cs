using DG.Tweening;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The hint's guide: a paper plane that glides the hinted squares ahead of the player's
    /// plane, turning to face each leg, dropping little white puffs behind it like a vapour
    /// trail. A plane, so it speaks the airline's language rather than a generic glowing dot;
    /// paper, so it is never mistaken for the player's own.
    ///
    /// The puffs are separate sprites laid at even spacing rather than a texture stretched
    /// along a line, so they stay round through every turn. They are made ahead of time and
    /// reused on every run.
    ///
    /// It only knows how to appear, turn, glide and leave. What it glides through, how fast
    /// and how often is the caller's business.
    /// </summary>
    public sealed class HintGhostView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer marker;

        [Tooltip("The trail's puffs, used in order and reused on every run. Optional.")]
        [SerializeField] private SpriteRenderer[] puffs = new SpriteRenderer[0];

        [Tooltip("World distance between two puffs of the trail.")]
        [SerializeField, Min(0.05f)] private float puffSpacing = 0.24f;

        [Tooltip("How solid the guide and its trail are: a suggestion, not the player's own path.")]
        [SerializeField, Range(0f, 1f)] private float alpha = 1f;

        private Color tint = Color.white;
        private float fade;
        private int usedPuffs;
        private Vector3 lastPuff;

        /// <summary>False when no marker was wired, which leaves the hint to run without one.</summary>
        public bool IsReady => marker != null;

        private void Awake()
        {
            Hide();
        }

        public void Show(Vector3 worldPosition, Color colour)
        {
            if (marker == null)
            {
                return;
            }

            tint = colour;
            marker.transform.position = worldPosition;
            marker.enabled = true;
            HidePuffs();
            lastPuff = worldPosition;
            SetFade(1f);
        }

        /// <summary>Turns the guide to face the next square. Called as each leg begins.</summary>
        public void BeginLeg(Vector3 towards)
        {
            Vector3 direction = towards - marker.transform.position;

            if (direction.sqrMagnitude > 0f)
            {
                // The paper plane is drawn nose up.
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
                marker.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        /// <summary>
        /// Glides to the next square at a steady rate, dropping puffs as it goes. Linear on
        /// purpose: a plane in flight does not stop at every square.
        /// </summary>
        public Tween MoveTo(Vector3 worldPosition, float duration)
        {
            return marker.transform
                .DOMove(worldPosition, duration)
                .SetEase(Ease.Linear)
                .OnUpdate(DropPuffs);
        }

        public Tween FadeOut(float duration)
        {
            return DOTween.To(() => fade, SetFade, 0f, duration).OnComplete(Hide);
        }

        public void Hide()
        {
            if (marker == null)
            {
                return;
            }

            SetFade(0f);
            marker.enabled = false;
            HidePuffs();
        }

        /// <summary>Lays a puff each time the guide has covered another spacing since the last.</summary>
        private void DropPuffs()
        {
            Vector3 position = marker.transform.position;
            Vector3 travelled = position - lastPuff;
            float distance = travelled.magnitude;

            while (distance >= puffSpacing && usedPuffs < puffs.Length)
            {
                lastPuff += travelled / distance * puffSpacing;
                SpriteRenderer puff = puffs[usedPuffs++];
                puff.transform.position = lastPuff;
                puff.color = marker.color;
                puff.enabled = true;
                travelled = position - lastPuff;
                distance = travelled.magnitude;
            }
        }

        private void HidePuffs()
        {
            for (int i = 0; i < puffs.Length; i++)
            {
                puffs[i].enabled = false;
            }

            usedPuffs = 0;
        }

        private void SetFade(float value)
        {
            fade = value;
            var colour = new Color(tint.r, tint.g, tint.b, alpha * value);
            marker.color = colour;

            for (int i = 0; i < usedPuffs; i++)
            {
                puffs[i].color = colour;
            }
        }
    }
}
