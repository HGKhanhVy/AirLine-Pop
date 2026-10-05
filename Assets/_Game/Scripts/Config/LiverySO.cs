using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// One paint job for the airline's plane, sold in the shop: the plane seen from above
    /// on the board and side-on at the stand on Home. Its name is in the localization table.
    /// </summary>
    [CreateAssetMenu(menuName = "AirLine Pop/Livery", fileName = "Livery")]
    public sealed class LiverySO : ScriptableObject
    {
        [Tooltip("Saved with the player's profile, so it must not change once it has shipped.")]
        [SerializeField] private string id = "coral";
        [SerializeField, Min(0)] private int price;
        [SerializeField] private bool isOwnedByDefault;
        [SerializeField] private Sprite planeTopDown;
        [SerializeField] private Sprite planeParked;

        public string Id => id;

        public int Price => price;

        public bool IsOwnedByDefault => isOwnedByDefault;

        public Sprite PlaneTopDown => planeTopDown;

        public Sprite PlaneParked => planeParked;

        public string DisplayName => Localization.Get("livery." + id);

#if UNITY_EDITOR
        public void EditorConfigure(string liveryId, int liveryPrice, bool isStarter, Sprite topDown, Sprite parked)
        {
            id = liveryId;
            price = liveryPrice;
            isOwnedByDefault = isStarter;
            planeTopDown = topDown;
            planeParked = parked;
        }
#endif
    }
}
