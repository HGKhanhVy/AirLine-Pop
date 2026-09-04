using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Sizes the orthographic camera so the whole board fits with a margin.
    ///
    /// Boards run from three cells to ninety and from square to tall, and phone screens
    /// vary as much again, so a fixed camera size would either crop the big boards or
    /// leave the small ones lost in the middle of the screen.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class BoardCameraFramer : MonoBehaviour
    {
        [SerializeField] private ThemeSO theme;
        [SerializeField] private Camera boardCamera;

        private Vector2 framedSize;

        private void Reset()
        {
            boardCamera = GetComponent<Camera>();
        }

        private void Awake()
        {
            if (boardCamera == null)
            {
                boardCamera = GetComponent<Camera>();
            }

            boardCamera.orthographic = true;
            boardCamera.backgroundColor = theme.Background;
        }

        public void Frame(Vector2 boardWorldSize)
        {
            framedSize = boardWorldSize;
            Apply();
        }

        /// <summary>
        /// Re-frames when the window changes shape, which matters in the editor and on a
        /// device that rotates.
        /// </summary>
        private void OnRectTransformDimensionsChange()
        {
            Apply();
        }

        private void Apply()
        {
            if (framedSize.x <= 0f || framedSize.y <= 0f)
            {
                return;
            }

            float margin = theme.BoardMargin * theme.CellPitch;
            float halfHeight = (framedSize.y + margin * 2f) * 0.5f;
            float halfWidthAsHeight = (framedSize.x + margin * 2f) * 0.5f / Mathf.Max(0.01f, boardCamera.aspect);

            boardCamera.orthographicSize = Mathf.Max(halfHeight, halfWidthAsHeight);
        }
    }
}
