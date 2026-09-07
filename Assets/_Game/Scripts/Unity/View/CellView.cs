using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// One square on the board. Owns nothing but its own appearance: which cell it stands
    /// for and what state it is in are told to it by <see cref="BoardView"/>.
    ///
    /// The renderer is resolved once when the pool builds the object, never in a loop.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CellView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        /// <summary>Board index this square currently stands for.</summary>
        public int CellIndex { get; private set; }

        /// <summary>The colour the square is drawn in, for effects that match a cell.</summary>
        public Color Color => spriteRenderer.color;

        /// <summary>Where the square belongs, so a shake has somewhere to return to.</summary>
        public Vector3 BaseLocalPosition { get; private set; }

        private Color fillFrom;
        private Color fillTo;
        private float fillDuration;
        private float fillLeft;

        private float popAmount;
        private float popDuration;
        private float popLeft;

        private void Reset()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// Called once by the pool when the object is created, so nothing has to look the
        /// renderer up again for the lifetime of the object.
        /// </summary>
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

        /// <summary>
        /// Eases into a colour instead of snapping to it, so a square fills as the path
        /// reaches it rather than lighting up before the line has arrived.
        /// </summary>
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
        /// Swells and settles back, so the square answers the path arriving at it. GDD 10
        /// calls this the raise half of "fill/raise".
        /// </summary>
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

        /// <summary>Advances fill and pop. Returns false once there is nothing left to do.</summary>
        public bool Advance(float deltaTime)
        {
            bool running = false;

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
                    // One half sine: out and straight back, no overshoot to settle.
                    float t = 1f - (popLeft / popDuration);
                    transform.localScale = Vector3.one * (1f + popAmount * Mathf.Sin(t * Mathf.PI));
                    running = true;
                }
            }

            return running;
        }
    }
}
