using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Runs Dress up from a regular's menu: folds the menu's actions away, opens the row of
    /// accessories over the cat, puts on whatever unlocked one the player picks, and brings
    /// the actions back on Back. Locked accessories show the loyalty card that unlocks them.
    /// </summary>
    public sealed class CatWardrobePresenter : MonoBehaviour
    {
        [SerializeField] private CatCarePresenter care;
        [SerializeField] private CatMenuView menu;
        [SerializeField] private CatWardrobePickerView picker;

        [Tooltip("The picture on the first slot, which takes the accessory off.")]
        [SerializeField] private Sprite noneIcon;

        private ICatWardrobeService wardrobe;

        public void Initialize(ICatWardrobeService wardrobeService)
        {
            wardrobe = wardrobeService;
        }

        private void OnEnable()
        {
            menu.OnWardrobe += Open;
            menu.OnHidden += HandleMenuHidden;
            picker.OnPicked += HandlePicked;
            picker.OnBack += Back;
        }

        private void OnDisable()
        {
            menu.OnWardrobe -= Open;
            menu.OnHidden -= HandleMenuHidden;
            picker.OnPicked -= HandlePicked;
            picker.OnBack -= Back;
        }

        private void Open()
        {
            if (wardrobe == null || care.CurrentCatId == null)
            {
                return;
            }

            menu.SetActionsShown(false);
            picker.Show(BuildSlots(care.CurrentCatId), menu.FreeArea);
        }

        private void Back()
        {
            picker.Hide();
            menu.SetActionsShown(true);
        }

        private void HandleMenuHidden()
        {
            picker.Hide();
        }

        /// <summary>Slot 0 takes the accessory off; slot n puts on the n-th accessory, if it is unlocked.</summary>
        private void HandlePicked(int slot)
        {
            string catId = care.CurrentCatId;

            if (catId == null)
            {
                return;
            }

            AccessorySO item = slot == 0 ? null : wardrobe.Accessories[slot - 1];
            wardrobe.Wear(catId, item);
            picker.Show(BuildSlots(catId), menu.FreeArea);
        }

        private WardrobeSlotModel[] BuildSlots(string catId)
        {
            var items = wardrobe.Accessories;
            AccessorySO worn = wardrobe.GetWorn(catId);
            var slots = new WardrobeSlotModel[items.Count + 1];
            slots[0] = new WardrobeSlotModel(noneIcon, Localization.Get("accessory.none"), true, worn == null);

            for (int i = 0; i < items.Count; i++)
            {
                AccessorySO item = items[i];
                bool isUnlocked = wardrobe.IsUnlocked(catId, item);
                string label = isUnlocked ? Localization.Get(item.NameKey) : Localization.Get("tier." + item.RequiredTier);
                slots[i + 1] = new WardrobeSlotModel(item.Icon, label, isUnlocked, item == worn);
            }

            return slots;
        }

#if UNITY_EDITOR
        public void EditorLink(CatCarePresenter linkedCare, CatMenuView linkedMenu, CatWardrobePickerView linkedPicker, Sprite linkedNoneIcon)
        {
            care = linkedCare;
            menu = linkedMenu;
            picker = linkedPicker;
            noneIcon = linkedNoneIcon;
        }
#endif
    }
}
