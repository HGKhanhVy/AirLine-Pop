using DG.Tweening;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The light a hint drags through the squares it is pointing at.
    ///
    /// It is the same art the player's own path head wears, shown at reduced alpha: the
    /// hint is meant to read as somebody playing the move, so what it drags has to be the
    /// thing that leads a real drag. Anything else would teach the player a mark that
    /// never appears again.
    ///
    /// It only knows how to appear, slide and leave. What it slides through, how fast and
    /// how often is the caller's business.
    /// </summary>
    public sealed class HintGhostView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer marker;

        [Tooltip("How solid the ghost is. Below the real path head on purpose: it is a " +
                 "suggestion, and it should not compete with the line the player owns.")]
        [SerializeField, Range(0f, 1f)] private float alpha = 0.7f;

        private Color tint = Color.white;

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
            marker.color = new Color(tint.r, tint.g, tint.b, alpha);
            marker.enabled = true;
        }

        /// <summary>
        /// Slides to the next square at a steady rate. Linear on purpose: a finger crossing
        /// a board does not ease into every square, and easing each leg turns one drag into
        /// a row of separate little moves.
        /// </summary>
        public Tween MoveTo(Vector3 worldPosition, float duration)
        {
            return marker.transform
                .DOMove(worldPosition, duration)
                .SetEase(Ease.Linear);
        }

        public Tween FadeOut(float duration)
        {
            return marker
                .DOFade(0f, duration)
                .OnComplete(Hide);
        }

        public void Hide()
        {
            if (marker == null)
            {
                return;
            }

            marker.color = new Color(tint.r, tint.g, tint.b, 0f);
            marker.enabled = false;
        }
    }
}
