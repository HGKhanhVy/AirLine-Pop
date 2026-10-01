using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// A soft darkening round the edges of the view, which pulls the eye to the board and
    /// makes the sea feel deeper towards the frame. A sprite held just in front of the
    /// camera and stretched to fill its view, far cheaper than a post-processing vignette.
    /// It is only re-fitted when the field of view or the screen shape changes.
    /// </summary>
    public sealed class CameraVignette : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private SpriteRenderer overlay;

        [Tooltip("Distance in front of the camera. Just past the near clip plane, so nothing comes between.")]
        [SerializeField, Min(0.01f)] private float distance = 1.5f;

        private float fittedFieldOfView = -1f;
        private float fittedAspect = -1f;

        private void Awake()
        {
            overlay.sortingOrder = BoardSortingOrder.Vignette;
        }

        private void LateUpdate()
        {
            float fieldOfView = targetCamera.fieldOfView;
            float aspect = targetCamera.aspect;

            if (Mathf.Approximately(fieldOfView, fittedFieldOfView) && Mathf.Approximately(aspect, fittedAspect))
            {
                return;
            }

            fittedFieldOfView = fieldOfView;
            fittedAspect = aspect;
            Fit(fieldOfView, aspect);
        }

        private void Fit(float fieldOfView, float aspect)
        {
            if (overlay.sprite == null)
            {
                return;
            }

            float viewHeight = 2f * distance * Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad);
            float viewWidth = viewHeight * aspect;
            Vector2 spriteSize = overlay.sprite.bounds.size;

            Transform target = overlay.transform;
            target.localPosition = new Vector3(0f, 0f, distance);
            target.localRotation = Quaternion.identity;
            target.localScale = new Vector3(viewWidth / spriteSize.x, viewHeight / spriteSize.y, 1f);
        }
    }
}
