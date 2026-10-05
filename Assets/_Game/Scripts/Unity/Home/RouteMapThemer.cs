using ASTeams.Base.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Dresses the route map in the theme the player wears: its sky and its gate signs.
    /// Follows the shop, so a theme bought or put on repaints the map at once.
    /// </summary>
    public sealed class RouteMapThemer : MonoBehaviour
    {
        [SerializeField] private SkinCatalogSO catalog;
        [SerializeField] private Image sky;
        [SerializeField] private LevelNodeView[] gates = new LevelNodeView[0];
        [SerializeField] private RouteMapView map;
        [SerializeField] private RouteMapDecor decor;

        private void OnEnable()
        {
            ProfileSkinService.AnyChanged += Apply;
            Apply();
        }

        private void OnDisable()
        {
            ProfileSkinService.AnyChanged -= Apply;
        }

        private void Apply()
        {
            SkinSO theme = new ProfileSkinService(catalog, UserProfileController.Instance).Equipped;

            if (theme == null)
            {
                return;
            }

            if (theme.MapSky != null)
            {
                sky.sprite = theme.MapSky;
            }

            decor.TintClouds(theme.CloudTint);

            if (theme.GateFlown == null)
            {
                return;
            }

            for (int i = 0; i < gates.Length; i++)
            {
                gates[i].SetFaces(theme.GateFlown, theme.GateCurrent, theme.GateLocked);
            }

            map.Repaint();
        }

#if UNITY_EDITOR
        public void EditorLink(SkinCatalogSO linkedCatalog, Image linkedSky, LevelNodeView[] linkedGates, RouteMapView linkedMap,
            RouteMapDecor linkedDecor)
        {
            decor = linkedDecor;
            catalog = linkedCatalog;
            sky = linkedSky;
            gates = linkedGates;
            map = linkedMap;
        }
#endif
    }
}
