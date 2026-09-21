using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Shrinks a fixed-size design block uniformly until it fits the area it sits in.
    ///
    /// The canvases match width and height half-and-half (GDD 5), so a 20:9 phone loses
    /// about a tenth of the reference width and a 4:3 tablet a seventh of its height.
    /// A popup laid out for 1080x1920 then spills over the edge on one or the other.
    /// Scaling the whole block keeps its proportions instead of letting stretched and
    /// fixed children drift apart.
    ///
    /// Put it on the stretched container (normally the one carrying SafeArea) so the
    /// resize callback fires when the screen or the safe area changes; no Update.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class ContentScaleFitter : MonoBehaviour
    {
        [SerializeField] private RectTransform container;
        [SerializeField] private RectTransform content;

        [Tooltip("Space kept free on every side, in canvas units.")]
        [SerializeField, Min(0f)] private float margin = 24f;

        [Tooltip("1 means the block never grows past its designed size on large screens.")]
        [SerializeField, Min(0.1f)] private float maxScale = 1f;

        private void Reset()
        {
            container = (RectTransform)transform;
        }

        private void OnEnable()
        {
            Fit();
        }

        private void OnRectTransformDimensionsChange()
        {
            Fit();
        }

        private void Fit()
        {
            if (container == null || content == null)
            {
                return;
            }

            Vector2 available = container.rect.size - new Vector2(margin * 2f, margin * 2f);
            Vector2 needed = content.rect.size;

            if (available.x <= 0f || available.y <= 0f || needed.x <= 0f || needed.y <= 0f)
            {
                return;
            }

            float scale = Mathf.Min(maxScale, available.x / needed.x, available.y / needed.y);
            content.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
