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
            transform.localPosition = localPosition;
            transform.localScale = Vector3.one;

            spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
            spriteRenderer.drawMode = SpriteDrawMode.Sliced;
            spriteRenderer.size = new Vector2(size, size);
        }

        public void SetColor(Color color)
        {
            spriteRenderer.color = color;
        }
    }
}
