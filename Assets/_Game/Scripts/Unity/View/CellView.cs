using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CellView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private SpriteRenderer startDotRenderer;
        [SerializeField] private SpriteRenderer startPulseRenderer;
        [SerializeField, Min(0.1f)] private float startPulseDuration = 1.1f;
        [SerializeField, Min(1f)] private float startPulseScale = 2.2f;
        [SerializeField, Range(0f, 1f)] private float startPulseAlpha = 0.24f;
        [SerializeField, Min(0f)] private float idleReminderDelay = 3f;
        [SerializeField, Min(0f)] private float idleShakeStrength = 0.035f;
        [SerializeField, Min(0.1f)] private float idleShakeDuration = 0.45f;
        [SerializeField, Min(0.1f)] private float idleShakeInterval = 1.6f;

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

        private void Reset()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
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

            spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
            spriteRenderer.drawMode = SpriteDrawMode.Sliced;
            spriteRenderer.size = new Vector2(size, size);
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
            transform.localPosition = BaseLocalPosition;
            startDotRenderer.enabled = true;
            startDotRenderer.color = Color.white;
            startPulseRenderer.enabled = true;
        }

        public void HideStartCue()
        {
            isStartCueVisible = false;
            cueElapsed = 0f;
            transform.localPosition = BaseLocalPosition;

            if (startDotRenderer != null)
            {
                startDotRenderer.enabled = false;
            }

            if (startPulseRenderer != null)
            {
                startPulseRenderer.enabled = false;
                startPulseRenderer.transform.localScale = Vector3.one;
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
            startPulseRenderer.transform.localScale = Vector3.one * scale;
            startPulseRenderer.color = new Color(1f, 1f, 1f, alpha);

            if (cueElapsed < idleReminderDelay || idleShakeDuration <= 0f)
            {
                transform.localPosition = BaseLocalPosition;
                return;
            }

            float reminderTime = cueElapsed - idleReminderDelay;
            float cycle = Mathf.Repeat(reminderTime, idleShakeInterval);

            if (cycle >= idleShakeDuration)
            {
                transform.localPosition = BaseLocalPosition;
                return;
            }

            float envelope = Mathf.Sin(cycle / idleShakeDuration * Mathf.PI);
            float offset = Mathf.Sin(cycle * 42f) * idleShakeStrength * envelope;
            transform.localPosition = BaseLocalPosition + Vector3.right * offset;
        }
    }
}