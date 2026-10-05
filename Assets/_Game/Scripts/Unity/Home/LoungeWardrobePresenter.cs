using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Dresses the regulars on screen in what they have chosen to wear: in the lounge and at
    /// the gate alike, when they arrive and the moment the player changes their outfit.
    /// </summary>
    public sealed class LoungeWardrobePresenter : MonoBehaviour
    {
        [SerializeField] private CatRoster[] rosters = new CatRoster[0];

        private ICatWardrobeService wardrobe;

        public void Initialize(ICatWardrobeService wardrobeService)
        {
            wardrobe = wardrobeService;
            wardrobe.OnWornChanged += HandleWornChanged;

            for (int i = 0; i < rosters.Length; i++)
            {
                rosters[i].OnRosterChanged += DressAll;
            }

            DressAll();
        }

        private void OnDestroy()
        {
            if (wardrobe == null)
            {
                return;
            }

            wardrobe.OnWornChanged -= HandleWornChanged;

            for (int i = 0; i < rosters.Length; i++)
            {
                rosters[i].OnRosterChanged -= DressAll;
            }
        }

        private void HandleWornChanged(string catId)
        {
            DressAll();
        }

        private void DressAll()
        {
            for (int r = 0; r < rosters.Length; r++)
            {
                for (int i = 0; i < rosters[r].Active.Count; i++)
                {
                    CatView cat = rosters[r].Active[i].View;

                    if (cat.Accessory != null)
                    {
                        cat.Accessory.Wear(wardrobe.GetWorn(cat.CatId));
                    }
                }
            }
        }

#if UNITY_EDITOR
        public void EditorLink(CatRoster[] linkedRosters)
        {
            rosters = linkedRosters;
        }
#endif
    }
}
