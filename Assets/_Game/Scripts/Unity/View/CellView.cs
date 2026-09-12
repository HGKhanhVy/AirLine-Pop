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

        private static readonly int StartIdleState = Animator.StringToHash("StartIdle");

        public int CellIndex { get; private set; }

        public Color Color => spriteRenderer.color;

        public Vector3 BaseLocalPosition { get; private set; }

        private Color fillFrom;
        private Color fillTo;
        private float fillDuration;
        private float fillLeft;
        private float popAmount;
        private float popDuration;
        private float popLeft;
        private bool isStartCueVisible;
        private float cueElapsed;
        private float nextIdleAnimationTime;
        private Vector3 startDotBaseScale;
        private Vector3 startPulseBaseScale;

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
            popLeft = 0f;
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

        public void Pop(float amount, float duration)
        {
            if (amount <= 0f || duration <= 0f)
            {
                return;
            }

            popAmount = amount;
            popDuration = duration;
            popLeft = duration;
        }

        public void ShowStartCue()
        {
            if (startDotRenderer == null || startPulseRenderer == null)
            {
                return;
            }

            isStartCueVisible = true;
            cueElapsed = 0f;
            nextIdleAnimationTime = idleReminderDelay;
            transform.localPosition = BaseLocalPosition;
            startDotRenderer.enabled = true;
            startDotRenderer.color = Color.white;
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

            if (popLeft > 0f)
            {
                popLeft -= deltaTime;

                if (popLeft <= 0f)
                {
                    transform.localScale = Vector3.one;
                }
                else
                {
                    float t = 1f - (popLeft / popDuration);
                    transform.localScale = Vector3.one * (1f + popAmount * Mathf.Sin(t * Mathf.PI));
                    running = true;
                }
            }

            if (isStartCueVisible)
            {
                AdvanceStartCue(deltaTime);
            }

            return running;
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
            startPulseRenderer.color = new Color(1f, 1f, 1f, alpha);

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
