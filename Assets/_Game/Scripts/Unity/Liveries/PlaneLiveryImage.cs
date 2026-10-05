using ASTeams.Base.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The UI twin of <see cref="PlaneLiveryView"/>: shows the plane the player chose in an
    /// Image, such as the plane flying the route map, and changes as soon as the shop does.
    /// </summary>
    public sealed class PlaneLiveryImage : MonoBehaviour
    {
        [SerializeField] private Image target;
        [SerializeField] private LiveryCatalogSO catalog;

        [Tooltip("On for the side-on plane; off for the plane seen from above.")]
        [SerializeField] private bool isParked;

        private void OnEnable()
        {
            ProfileLiveryService.AnyChanged += Apply;
            Apply();
        }

        private void OnDisable()
        {
            ProfileLiveryService.AnyChanged -= Apply;
        }

        private void Apply()
        {
            LiverySO livery = new ProfileLiveryService(catalog, UserProfileController.Instance).Equipped;
            Sprite sprite = livery == null ? null : isParked ? livery.PlaneParked : livery.PlaneTopDown;

            if (sprite != null)
            {
                target.sprite = sprite;
            }
        }

#if UNITY_EDITOR
        public void EditorLink(Image linkedTarget, LiveryCatalogSO linkedCatalog, bool parked)
        {
            target = linkedTarget;
            catalog = linkedCatalog;
            isParked = parked;
        }
#endif
    }
}
