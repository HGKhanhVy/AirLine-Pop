using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// A rounded progress bar: the fill is a sliced pill that grows in width, so its round
    /// ends keep their shape at any value instead of being cut off as a filled image would.
    /// </summary>
    public sealed class ProgressBarView : MonoBehaviour
    {
        [SerializeField] private RectTransform fill;

        [Tooltip("Share of the track the fill takes up at the smallest non-zero value, so the pill never collapses.")]
        [SerializeField, Range(0f, 0.5f)] private float minVisible = 0.08f;

        public void SetProgress(float progress)
        {
            float clamped = Mathf.Clamp01(progress);
            fill.gameObject.SetActive(clamped > 0f);

            Vector2 max = fill.anchorMax;
            max.x = Mathf.Lerp(minVisible, 1f, clamped);
            fill.anchorMax = max;
        }

#if UNITY_EDITOR
        public void EditorLink(RectTransform linkedFill)
        {
            fill = linkedFill;
        }
#endif
    }
}
