using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Something a regular can wear, on its head or round its neck, unlocked by how close it
    /// has grown to the player: a bow at Bronze, a pilot's cap at Silver and so on.
    /// </summary>
    [CreateAssetMenu(menuName = "AirLine Pop/Accessory", fileName = "Accessory")]
    public sealed class AccessorySO : ScriptableObject
    {
        [Tooltip("Saved with each cat that wears it, so never rename one already shipped.")]
        [SerializeField] private string id;

        [Tooltip("Localization key of its name, such as accessory.bow.")]
        [SerializeField] private string nameKey;

        [Tooltip("The picture in the wardrobe picker.")]
        [SerializeField] private Sprite icon;

        [Tooltip("Where it is worn: a hat rests on the crown of the head, a bow sits under the chin.")]
        [SerializeField] private AccessorySlot slot = AccessorySlot.Head;

        [Tooltip("The drawing laid on the cat; its pivot is the point placed on the slot.")]
        [SerializeField] private Sprite worn;

        [Tooltip("The loyalty card that unlocks it: 1 Bronze, 2 Silver, 3 Gold, 4 Diamond.")]
        [SerializeField, Min(0)] private int requiredTier = 1;

        [Tooltip("Its size on the cat, as a share of the cat's own drawing scale.")]
        [SerializeField, Min(0.1f)] private float scale = 1f;

        public string Id => id;

        public string NameKey => nameKey;

        public Sprite Icon => icon;

        public AccessorySlot Slot => slot;

        public Sprite Worn => worn;

        public int RequiredTier => requiredTier;

        public float Scale => scale;

#if UNITY_EDITOR
        public void EditorConfigure(string linkedId, string linkedNameKey, Sprite linkedIcon, Sprite linkedWorn, AccessorySlot linkedSlot,
            int linkedTier, float linkedScale)
        {
            slot = linkedSlot;
            id = linkedId;
            nameKey = linkedNameKey;
            icon = linkedIcon;
            worn = linkedWorn;
            requiredTier = linkedTier;
            scale = linkedScale;
        }
#endif
    }
}
