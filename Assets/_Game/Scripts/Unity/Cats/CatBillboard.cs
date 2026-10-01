using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Keeps a flat cat facing whoever looks at it, and mirrors it to the side it walks.
    ///
    /// The motor still turns the cat's root to walk, exactly as for a modelled cat; only the
    /// drawing is held square to the viewer. The art faces left of the viewer (tail on the
    /// right), so walking to the viewer's right mirrors it.
    /// </summary>
    public sealed class CatBillboard : MonoBehaviour
    {
        // Below this share of sideways travel the cat keeps the side it last faced, so a cat
        // walking straight at the camera does not flicker between the two.
        private const float TurnThreshold = 0.2f;

        [SerializeField] private Transform body;
        [SerializeField] private Transform visual;

        private Transform viewer;
        private float facing = 1f;

        public void SetViewer(Transform viewerTransform)
        {
            viewer = viewerTransform;
            enabled = viewer != null;
            Face();
        }

        private void Awake()
        {
            enabled = viewer != null;
        }

        private void LateUpdate()
        {
            Face();
        }

        private void Face()
        {
            if (viewer == null)
            {
                return;
            }

            visual.rotation = viewer.rotation;

            float side = Vector3.Dot(body.forward, viewer.right);

            if (Mathf.Abs(side) > TurnThreshold)
            {
                facing = side > 0f ? -1f : 1f;
            }

            Vector3 scale = visual.localScale;
            scale.x = Mathf.Abs(scale.x) * facing;
            visual.localScale = scale;
        }

#if UNITY_EDITOR
        public void EditorLink(Transform linkedBody, Transform linkedVisual)
        {
            body = linkedBody;
            visual = linkedVisual;
        }
#endif
    }
}
