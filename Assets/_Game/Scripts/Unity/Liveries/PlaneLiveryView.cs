using ASTeams.Base.Data;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Paints a plane in the livery the player chose: the one flying the board, or the one
    /// parked at the stand on Home. Repaints as soon as the shop changes it.
    /// </summary>
    public sealed class PlaneLiveryView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer target;
        [SerializeField] private LiveryCatalogSO catalog;

        [Tooltip("On for the side-on plane at the stand; off for the plane seen from above on the board.")]
        [SerializeField] private bool isParked;

        private void Start()
        {
            ProfileLiveryService.AnyChanged += Apply;
            Apply();
        }

        private void OnDestroy()
        {
            ProfileLiveryService.AnyChanged -= Apply;
        }

        private void Apply()
        {
            LiverySO livery = new ProfileLiveryService(catalog, UserProfileController.Instance).Equipped;

            if (livery == null)
            {
                return;
            }

            Sprite sprite = isParked ? livery.PlaneParked : livery.PlaneTopDown;

            if (sprite != null)
            {
                target.sprite = sprite;
            }
        }

#if UNITY_EDITOR
        public void EditorLink(SpriteRenderer linkedTarget, LiveryCatalogSO linkedCatalog, bool parked)
        {
            target = linkedTarget;
            catalog = linkedCatalog;
            isParked = parked;
        }
#endif
    }
}
