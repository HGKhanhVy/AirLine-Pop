using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Tells the board camera how much of the screen the HUD bars really cover.
    ///
    /// A fixed reserve cannot be right everywhere: the bars sit inside the safe area, so a
    /// notch pushes the top bar down, and the canvas scaling makes the same bar a larger
    /// share of a squat tablet than of a tall phone. Measuring the bars themselves keeps
    /// the board clear on every screen.
    ///
    /// Sits on the stretched safe-area container, whose size changes exactly when the
    /// screen or the safe area does; no Update.
    /// </summary>
    public sealed class HudInsetReporter : MonoBehaviour
    {
        [SerializeField] private RectTransform topBar;
        [SerializeField] private RectTransform bottomBar;

        private readonly Vector3[] corners = new Vector3[4];
        private bool isPending;

        private void OnEnable()
        {
            GameplayEvents.OnSnapshotRequested += MarkDirty;
            MarkDirty();
        }

        private void OnDisable()
        {
            GameplayEvents.OnSnapshotRequested -= MarkDirty;
            Canvas.willRenderCanvases -= ReportPending;
            isPending = false;
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled)
            {
                MarkDirty();
            }
        }

        /// <summary>
        /// Measuring inside the resize callback reads a layout still in motion: on one
        /// screen change the canvas rescales and the safe area re-anchors in separate
        /// steps, each raising the callback, and the last one can see stale values.
        /// The measurement waits for the canvases to settle, once, before they draw.
        /// </summary>
        private void MarkDirty()
        {
            if (isPending)
            {
                return;
            }

            isPending = true;
            Canvas.willRenderCanvases += ReportPending;
        }

        private void ReportPending()
        {
            Canvas.willRenderCanvases -= ReportPending;
            isPending = false;
            Report();
        }

        private void Report()
        {
            if (topBar == null || bottomBar == null || Screen.height <= 0)
            {
                return;
            }

            // Screen-space overlay canvas: world units are screen pixels.
            topBar.GetWorldCorners(corners);
            float topShare = 1f - corners[0].y / Screen.height;

            bottomBar.GetWorldCorners(corners);
            float bottomShare = corners[1].y / Screen.height;

            GameplayEvents.RaiseHudInsetsChanged(Mathf.Clamp01(topShare), Mathf.Clamp01(bottomShare));
        }
    }
}
