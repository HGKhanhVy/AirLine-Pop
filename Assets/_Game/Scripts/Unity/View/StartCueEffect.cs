using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The white dot on the start square, the ring that swells out of it, and the timer
    /// for the reminder the square gives while nobody has touched the board.
    ///
    /// It only owns the dot and the ring. Playing the reminder clip is left to the square,
    /// because the same animator also carries the connect and goal clips.
    /// </summary>
    public sealed class StartCueEffect
    {
        private readonly SpriteRenderer dot;
        private readonly SpriteRenderer pulse;
        private readonly StartCueSettings settings;
        private readonly Vector3 dotBaseScale;
        private readonly Vector3 pulseBaseScale;

        private Color haloColor = Color.white;
        private float elapsed;
        private float nextReminderTime;

        public StartCueEffect(SpriteRenderer dot, SpriteRenderer pulse, StartCueSettings settings)
        {
            this.dot = dot;
            this.pulse = pulse;
            this.settings = settings;

            if (dot != null)
            {
                dotBaseScale = dot.transform.localScale;
            }

            if (pulse != null)
            {
                pulseBaseScale = pulse.transform.localScale;
            }
        }

        public bool IsVisible { get; private set; }

        /// <summary>Returns false when the prefab has no dot or ring to show.</summary>
        public bool Show(Color dotColor, Color ringColor)
        {
            if (dot == null || pulse == null)
            {
                return false;
            }

            IsVisible = true;
            elapsed = 0f;
            nextReminderTime = settings.ReminderDelay;
            haloColor = ringColor;
            dot.enabled = true;
            dot.color = dotColor;
            pulse.enabled = true;
            return true;
        }

        /// <summary>Stops the ring but leaves the dot, which stays for the whole level.</summary>
        public void Hide()
        {
            IsVisible = false;
            elapsed = 0f;

            if (dot != null)
            {
                dot.transform.localScale = dotBaseScale;
            }

            if (pulse != null)
            {
                pulse.enabled = false;
                pulse.transform.localScale = pulseBaseScale;
            }
        }

        public void Clear()
        {
            Hide();

            if (dot != null)
            {
                dot.enabled = false;
            }
        }

        /// <summary>Returns true on the frame the idle reminder is due.</summary>
        public bool Advance(float deltaTime)
        {
            elapsed += deltaTime;

            float pulseT = settings.PulseDuration <= 0f
                ? 0f
                : Mathf.Repeat(elapsed, settings.PulseDuration) / settings.PulseDuration;
            float scale = Mathf.Lerp(1f, settings.PulseScale, pulseT);
            float alpha = settings.PulseAlpha * (1f - pulseT);
            pulse.transform.localScale = pulseBaseScale * scale;
            pulse.color = new Color(haloColor.r, haloColor.g, haloColor.b, alpha);

            if (elapsed < nextReminderTime)
            {
                return false;
            }

            nextReminderTime = elapsed + settings.ReminderInterval;
            return true;
        }
    }
}
