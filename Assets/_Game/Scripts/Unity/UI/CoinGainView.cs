using TMPro;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Says what a win paid, at the place the player looks for it.
    ///
    /// A "+30" rises into the coin counter and the counter itself gives a short kick, so
    /// the number changing is something that happened rather than something the player is
    /// left to notice. An earlier version of this floated the amount over the middle of
    /// the board, which pointed at nothing: the coins do not live there.
    ///
    /// It listens to the channel and holds no reference into gameplay. One label, reused;
    /// nothing is created or destroyed while the game runs.
    /// </summary>
    public sealed class CoinGainView : MonoBehaviour
    {
        [Tooltip("The '+30' itself. Shipped switched off; nothing shows it until a win pays.")]
        [SerializeField] private TMP_Text gainLabel;

        [Tooltip("The running total, kicked as the gain arrives. Optional.")]
        [SerializeField] private TMP_Text coinLabel;

        [SerializeField, Min(0.1f)] private float duration = 0.9f;

        [Tooltip("How far below the counter the amount starts, and it travels up to meet it.")]
        [SerializeField] private float riseFrom = -64f;

        [SerializeField] private float riseTo = -6f;

        [Tooltip("Share of the time spent fading. It holds still first so it can be read.")]
        [SerializeField, Range(0.1f, 1f)] private float fadeShare = 0.55f;

        [Tooltip("How far the counter swells as the amount lands.")]
        [SerializeField, Range(1f, 1.6f)] private float kickScale = 1.18f;

        [SerializeField, Min(0.05f)] private float kickDuration = 0.28f;

        private RectTransform gainRect;
        private Color gainBase;
        private Vector3 coinBaseScale = Vector3.one;
        private float elapsed;
        private float kickLeft;
        private bool isPlaying;

        private void Awake()
        {
            if (gainLabel != null)
            {
                gainRect = gainLabel.rectTransform;
                gainBase = gainLabel.color;
                gainLabel.enabled = false;
            }

            if (coinLabel != null)
            {
                coinBaseScale = coinLabel.rectTransform.localScale;
            }
        }

        private void OnEnable()
        {
            GameplayEvents.OnCoinsAwarded += HandleCoinsAwarded;
        }

        private void OnDisable()
        {
            GameplayEvents.OnCoinsAwarded -= HandleCoinsAwarded;
            Hide();
        }

        private void HandleCoinsAwarded(int amount, long balance)
        {
            if (amount <= 0)
            {
                return;
            }

            if (gainLabel != null)
            {
                // A format argument rather than "+" + amount: the concatenation would put a
                // string on the heap for every win.
                gainLabel.SetText("+{0}", amount);
                gainLabel.color = gainBase;
                gainRect.anchoredPosition = new Vector2(gainRect.anchoredPosition.x, riseFrom);
                gainLabel.enabled = true;
            }

            elapsed = 0f;
            kickLeft = kickDuration;
            isPlaying = true;
        }

        private void Update()
        {
            if (!isPlaying)
            {
                return;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (gainLabel != null)
            {
                float eased = 1f - (1f - t) * (1f - t);
                gainRect.anchoredPosition = new Vector2(
                    gainRect.anchoredPosition.x, Mathf.Lerp(riseFrom, riseTo, eased));

                float holdShare = 1f - fadeShare;
                float alpha = t <= holdShare
                    ? 1f
                    : 1f - ((t - holdShare) / fadeShare);

                gainLabel.color = new Color(gainBase.r, gainBase.g, gainBase.b, gainBase.a * alpha);
            }

            AdvanceKick();

            if (t >= 1f)
            {
                Hide();
            }
        }

        /// <summary>
        /// Up and back in one beat, the same shape a square gives when the path lands on
        /// it, so the whole game presses and settles the same way.
        /// </summary>
        private void AdvanceKick()
        {
            if (coinLabel == null || kickLeft <= 0f)
            {
                return;
            }

            kickLeft -= Time.deltaTime;

            if (kickLeft <= 0f)
            {
                coinLabel.rectTransform.localScale = coinBaseScale;
                return;
            }

            float k = 1f - (kickLeft / kickDuration);
            float swell = 1f + (kickScale - 1f) * Mathf.Sin(k * Mathf.PI);
            coinLabel.rectTransform.localScale = coinBaseScale * swell;
        }

        private void Hide()
        {
            isPlaying = false;
            kickLeft = 0f;

            if (gainLabel != null)
            {
                gainLabel.enabled = false;
            }

            if (coinLabel != null)
            {
                coinLabel.rectTransform.localScale = coinBaseScale;
            }
        }
    }
}
