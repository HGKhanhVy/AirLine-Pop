using DG.Tweening;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The hint's guide: a paper plane that hops from square to square ahead of the player's
    /// plane. Its nose follows the arc, rising off one square and diving into the next; it
    /// squashes as it lands and springs up again for the next hop, leaving a small puff on
    /// every square it touches, so the hinted route reads as a row of footprints. A plane, so it speaks the airline's language rather than a generic
    /// glowing dot; paper, so it is never mistaken for the player's own.
    ///
    /// The puffs are made ahead of time and reused on every run.
    ///
    /// It only knows how to appear, turn, hop and leave. Where it hops, how fast and how
    /// often is the caller's business.
    /// </summary>
    public sealed class HintGhostView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer marker;

        [Tooltip("The landing puffs, used in order and reused on every run. Optional.")]
        [SerializeField] private SpriteRenderer[] puffs = new SpriteRenderer[0];

        [Tooltip("How high each hop arcs, in world units.")]
        [SerializeField, Min(0f)] private float hopHeight = 0.7f;

        [Tooltip("How much the plane swells at the top of a hop, as if it rose towards the camera.")]
        [SerializeField, Range(1f, 1.6f)] private float hopSwell = 1.2f;

        [Tooltip("Share of each hop spent in the air; the rest is the squash and spring on landing.")]
        [SerializeField, Range(0.5f, 0.95f)] private float airShare = 0.8f;

        [Tooltip("How quickly the nose turns to follow the curve. Higher is snappier, lower is softer.")]
        [SerializeField, Min(1f)] private float turnSharpness = 14f;

        [Tooltip("How flat the plane squashes as it lands.")]
        [SerializeField, Range(0f, 0.5f)] private float landingSquash = 0.22f;

        [Tooltip("The landing puffs' colour: a soft sky blue, so they show on the cream squares.")]
        [SerializeField] private Color puffTint = new Color(0.55f, 0.78f, 0.96f, 1f);

        [Tooltip("How long a landing puff takes to pop up to full size.")]
        [SerializeField, Min(0.02f)] private float puffPopSeconds = 0.18f;

        [Tooltip("How solid the guide and its puffs are: a suggestion, not the player's own path.")]
        [SerializeField, Range(0f, 1f)] private float alpha = 1f;

        private Color tint = Color.white;
        private float fade;
        private int usedPuffs;
        private Vector3 restScale = Vector3.one;
        private Vector3 hopFrom;
        private Vector3 hopTo;
        private Vector3 puffScale = Vector3.one;

        /// <summary>False when no marker was wired, which leaves the hint to run without one.</summary>
        public bool IsReady => marker != null;

        private void Awake()
        {
            if (marker != null)
            {
                restScale = marker.transform.localScale;
            }

            if (puffs.Length > 0)
            {
                puffScale = puffs[0].transform.localScale;
            }

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
            marker.transform.localScale = restScale;
            marker.enabled = true;
            HidePuffs();
            SetFade(1f);
        }

        /// <summary>Turns the guide to face the next square. Called as each hop begins.</summary>
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
        /// One hop: up off this square and nose-down into the next along an arc, then a squash
        /// on landing that springs back up, with a puff left on the square. Each square is a
        /// beat of its own, which is what makes the route easy to follow.
        /// </summary>
        public Tween MoveTo(Vector3 worldPosition, float duration)
        {
            Transform plane = marker.transform;
            float air = duration * airShare;
            float land = duration - air;

            return DOTween.Sequence()
                .AppendCallback(() => BeginHop(worldPosition))
                .Append(DOTween.To(() => 0f, Fly, 1f, air).SetEase(Ease.Linear))
                .AppendCallback(() => DropPuff(worldPosition))
                .Append(DOTween.To(() => 0f, Land, 1f, land).SetEase(Ease.Linear));
        }

        private void BeginHop(Vector3 to)
        {
            hopFrom = marker.transform.position;
            hopTo = to;
        }

        /// <summary>
        /// Places the plane <paramref name="progress"/> of the way along a parabola and points
        /// its nose along the curve's slope there: up off the square, then diving into the next.
        /// Worked out here rather than by a stock jump tween so the nose always matches the
        /// path, however the hop is nested in the caller's sequence.
        /// </summary>
        private void Fly(float progress)
        {
            Transform plane = marker.transform;
            Vector3 straight = hopTo - hopFrom;
            float lift = 4f * hopHeight * progress * (1f - progress);
            plane.position = hopFrom + straight * progress + Vector3.up * lift;

            // Swells smoothly towards the top of the arc and back, one sine over the whole hop.
            plane.localScale = restScale * (1f + (hopSwell - 1f) * Mathf.Sin(progress * Mathf.PI));

            Vector3 slope = straight + Vector3.up * (4f * hopHeight * (1f - 2f * progress));

            if (slope.sqrMagnitude > 1e-8f)
            {
                // The paper plane is drawn nose up. The nose eases round to the curve rather than
                // snapping, which keeps the turn from a dive into the next climb soft.
                float target = Mathf.Atan2(slope.y, slope.x) * Mathf.Rad2Deg - 90f;
                float follow = 1f - Mathf.Exp(-turnSharpness * Time.deltaTime);
                float angle = Mathf.LerpAngle(plane.eulerAngles.z, target, follow);
                plane.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        /// <summary>
        /// The touchdown: one smooth sine of squash and spring, wider and flatter at its
        /// deepest, back to the plane's own shape as the next hop begins.
        /// </summary>
        private void Land(float progress)
        {
            float squash = landingSquash * Mathf.Sin(progress * Mathf.PI);
            marker.transform.localScale = new Vector3(restScale.x * (1f + squash), restScale.y * (1f - squash), restScale.z);
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
            marker.transform.DOKill();
            marker.transform.localScale = restScale;
            marker.enabled = false;
            HidePuffs();
        }

        /// <summary>Pops a puff up on the square the plane just landed on.</summary>
        private void DropPuff(Vector3 position)
        {
            if (usedPuffs >= puffs.Length)
            {
                return;
            }

            SpriteRenderer puff = puffs[usedPuffs++];
            puff.transform.position = position;
            puff.color = PuffColour(fade);
            puff.enabled = true;
            puff.transform.localScale = puffScale * 0.3f;
            puff.transform.DOScale(puffScale, puffPopSeconds).SetEase(Ease.OutBack);
        }

        private void HidePuffs()
        {
            for (int i = 0; i < puffs.Length; i++)
            {
                puffs[i].transform.DOKill();
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
                puffs[i].color = PuffColour(value);
            }
        }

        private Color PuffColour(float value)
        {
            return new Color(puffTint.r, puffTint.g, puffTint.b, puffTint.a * alpha * value);
        }
    }
}
