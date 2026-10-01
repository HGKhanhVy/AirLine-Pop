using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The white sheet that spreads from the middle of a square over its whole face the
    /// moment the path lands on it, then fades. Opening and fading together is what makes
    /// the square look lit from within rather than covered over.
    /// </summary>
    public sealed class ConnectFlashEffect
    {
        private readonly SpriteRenderer sheet;
        private readonly ConnectFlashSettings settings;

        private float timeLeft;

        public ConnectFlashEffect(SpriteRenderer sheet, ConnectFlashSettings settings)
        {
            this.sheet = sheet;
            this.settings = settings;

            // Above the block's face but below the start and goal marks, so it washes over
            // the colour without swallowing what the level is telling the player.
            if (sheet != null)
            {
                sheet.sortingOrder = BoardSortingOrder.ConnectFlash;
            }
        }

        public void Play()
        {
            if (sheet == null || settings.Duration <= 0f)
            {
                return;
            }

            timeLeft = settings.Duration;
            sheet.enabled = true;
            Apply();
        }

        /// <summary>Returns true while the flash is still running.</summary>
        public bool Advance(float deltaTime)
        {
            if (timeLeft <= 0f)
            {
                return false;
            }

            timeLeft -= deltaTime;

            if (timeLeft <= 0f)
            {
                Hide();
                return false;
            }

            Apply();
            return true;
        }

        public void Hide()
        {
            timeLeft = 0f;

            if (sheet == null)
            {
                return;
            }

            sheet.transform.localScale = Vector3.zero;
            sheet.color = new Color(1f, 1f, 1f, 0f);
            sheet.enabled = false;
        }

        private void Apply()
        {
            float t = 1f - (timeLeft / settings.Duration);
            float eased = t * t * (3f - 2f * t);
            float scale = Mathf.Lerp(settings.StartScale, settings.EndScale, eased);
            float rise = settings.RiseShare;

            float alpha = t < rise
                ? settings.PeakAlpha * (t / rise)
                : settings.PeakAlpha * (1f - ((t - rise) / (1f - rise)));

            sheet.transform.localScale = new Vector3(scale, scale, 1f);
            sheet.color = new Color(1f, 1f, 1f, alpha);
        }
    }
}
