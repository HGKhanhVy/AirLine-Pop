using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    public sealed class CellView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Transform visualSizeRoot;
        [SerializeField] private Animator blockAnimator;
        [SerializeField] private Transform animatedInner;
        [SerializeField] private SpriteRenderer startDotRenderer;
        [SerializeField] private SpriteRenderer startPulseRenderer;
        [SerializeField] private bool useConfiguredSprite;
        [SerializeField, Min(0.01f)] private float sourceVisualSize = 0.9f;
        [SerializeField, Min(0.1f)] private float startPulseDuration = 1.1f;
        [SerializeField, Min(1f)] private float startPulseScale = 2.2f;
        [SerializeField, Range(0f, 1f)] private float startPulseAlpha = 0.24f;
        [SerializeField, Min(0f)] private float idleReminderDelay = 3f;
        [SerializeField, Min(0.1f)] private float idleAnimationInterval = 4f;

        [Header("Connect")]
        [Tooltip("How long the press and bounce take. GDD 10 asks for 80 to 120 ms so the " +
                 "feedback keeps up with a fast drag.")]
        [SerializeField, Min(0.02f)] private float connectDuration = 0.14f;

        [Tooltip("How far the square sinks under the finger, as a share of its size.")]
        [SerializeField, Range(0f, 0.4f)] private float connectPressDepth = 0.12f;

        [Tooltip("How far it overshoots on the way back up.")]
        [SerializeField, Range(0f, 0.4f)] private float connectPopHeight = 0.14f;

        [Tooltip("Share of the move spent sinking. The rest is the bounce back.")]
        [SerializeField, Range(0.1f, 0.6f)] private float connectPressShare = 0.3f;

        private static readonly int StartIdleState = Animator.StringToHash("StartIdle");

        public int CellIndex { get; private set; }

        public Color Color => spriteRenderer.color;

        public Vector3 BaseLocalPosition { get; private set; }

        private Color fillFrom;
        private Color fillTo;
        private float fillDuration;
        private float fillLeft;
        private float connectLeft;
        private bool isStartCueVisible;
        private float cueElapsed;
        private float nextIdleAnimationTime;
        private Vector3 startDotBaseScale;
        private Vector3 startPulseBaseScale;
        private Color startHaloColor = Color.white;

        private void Awake()
        {
            if (startDotRenderer != null)
            {
                startDotBaseScale = startDotRenderer.transform.localScale;
            }

            if (startPulseRenderer != null)
            {
                startPulseBaseScale = startPulseRenderer.transform.localScale;
            }

            if (blockAnimator != null)
            {
                blockAnimator.enabled = false;
            }
        }

        public void Bind(SpriteRenderer renderer)
        {
            spriteRenderer = renderer;
        }

        public void Place(int cellIndex, Vector3 localPosition, float size, Sprite sprite, Color color)
        {
            CellIndex = cellIndex;
            BaseLocalPosition = localPosition;
            transform.localPosition = localPosition;
            transform.localScale = Vector3.one;
            fillLeft = 0f;
            connectLeft = 0f;
            HideStartCue();

            if (useConfiguredSprite && sprite != null)
            {
                spriteRenderer.sprite = sprite;
            }

            spriteRenderer.color = color;

            if (visualSizeRoot != null)
            {
                float scale = size / sourceVisualSize;
                visualSizeRoot.localScale = new Vector3(scale, scale, 1f);
            }
            else
            {
                spriteRenderer.drawMode = SpriteDrawMode.Sliced;
                spriteRenderer.size = new Vector2(size, size);
            }
        }

        public void SetColor(Color color)
        {
            fillLeft = 0f;
            spriteRenderer.color = color;
        }

        public void FillTo(Color color, float duration)
        {
            if (duration <= 0f)
            {
                SetColor(color);
                return;
            }

            fillFrom = spriteRenderer.color;
            fillTo = color;
            fillDuration = duration;
            fillLeft = duration;
        }

        /// <summary>
        /// The press and bounce a square makes when the path reaches it.
        ///
        /// The shape is taken from the reference game's own selection clip, which sinks
        /// the block and then lets it overshoot its size before settling. The clip itself
        /// cannot be played here: it stores absolute scales belonging to the source
        /// prefab's hierarchy, while these squares are resized at runtime to fit the
        /// board, and at 0.62 s it would still be moving two cells after the finger.
        /// </summary>
        public void PlayConnect()
        {
            if (connectDuration <= 0f)
            {
                return;
            }

            connectLeft = connectDuration;
        }

        /// <summary>
        /// Shows the white dot and the ring that swells out of it. Colours come from the
        /// theme rather than from the prefab, so one asset still decides how the board
        /// reads and a second theme needs no second prefab.
        /// </summary>
        public void ShowStartCue(Color dotColor, Color haloColor)
        {
            if (startDotRenderer == null || startPulseRenderer == null)
            {
                return;
            }

            isStartCueVisible = true;
            cueElapsed = 0f;
            nextIdleAnimationTime = idleReminderDelay;
            startHaloColor = haloColor;
            transform.localPosition = BaseLocalPosition;
            startDotRenderer.enabled = true;
            startDotRenderer.color = dotColor;
            startPulseRenderer.enabled = true;

            if (blockAnimator != null)
            {
                blockAnimator.enabled = false;
            }
        }

        public void HideStartCue()
        {
            isStartCueVisible = false;
            cueElapsed = 0f;
            transform.localPosition = BaseLocalPosition;

            if (startDotRenderer != null)
            {
                startDotRenderer.enabled = false;
                startDotRenderer.transform.localScale = startDotBaseScale;
            }

            if (startPulseRenderer != null)
            {
                startPulseRenderer.enabled = false;
                startPulseRenderer.transform.localScale = startPulseBaseScale;
            }

            if (blockAnimator != null)
            {
                blockAnimator.enabled = false;
            }

            if (animatedInner != null)
            {
                animatedInner.localRotation = Quaternion.identity;
            }
        }

        public bool Advance(float deltaTime)
        {
            bool running = isStartCueVisible;

            if (fillLeft > 0f)
            {
                fillLeft -= deltaTime;

                if (fillLeft <= 0f)
                {
                    spriteRenderer.color = fillTo;
                }
                else
                {
                    float t = 1f - (fillLeft / fillDuration);
                    spriteRenderer.color = Color.Lerp(fillFrom, fillTo, t * t * (3f - 2f * t));
                    running = true;
                }
            }

            if (connectLeft > 0f)
            {
                connectLeft -= deltaTime;

                if (connectLeft <= 0f)
                {
                    transform.localScale = Vector3.one;
                }
                else
                {
                    float t = 1f - (connectLeft / connectDuration);
                    transform.localScale = Vector3.one * ConnectScale(t);
                    running = true;
                }
            }

            if (isStartCueVisible)
            {
                AdvanceStartCue(deltaTime);
            }

            return running;
        }

        /// <summary>
        /// Sinks to <see cref="connectPressDepth"/> below its size, then eases back with a
        /// bump past it. Both ends land exactly on 1, so nothing is left scaled when the
        /// move finishes and a square can be released to the pool at any moment.
        /// </summary>
        private float ConnectScale(float t)
        {
            if (t < connectPressShare)
            {
                float press = t / connectPressShare;
                return 1f - connectPressDepth * Mathf.Sin(press * Mathf.PI * 0.5f);
            }

            float r = (t - connectPressShare) / (1f - connectPressShare);
            float ease = 1f - (1f - r) * (1f - r);
            float settled = Mathf.Lerp(1f - connectPressDepth, 1f, ease);
            return settled + connectPopHeight * Mathf.Sin(r * Mathf.PI);
        }

        private void AdvanceStartCue(float deltaTime)
        {
            cueElapsed += deltaTime;

            float pulseT = startPulseDuration <= 0f
                ? 0f
                : Mathf.Repeat(cueElapsed, startPulseDuration) / startPulseDuration;
            float scale = Mathf.Lerp(1f, startPulseScale, pulseT);
            float alpha = startPulseAlpha * (1f - pulseT);
            startPulseRenderer.transform.localScale = startPulseBaseScale * scale;
            startPulseRenderer.color = new Color(
                startHaloColor.r, startHaloColor.g, startHaloColor.b, alpha);

            if (blockAnimator == null || cueElapsed < nextIdleAnimationTime)
            {
                return;
            }

            blockAnimator.enabled = true;
            blockAnimator.Play(StartIdleState, 0, 0f);
            nextIdleAnimationTime = cueElapsed + idleAnimationInterval;
        }
    }
}
