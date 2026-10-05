using System.Collections.Generic;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The daily gift in the lounge: a box over every regular with a present waiting, and a
    /// touch on that cat hands it over, the coins rising from its head.
    /// </summary>
    public sealed class CatGiftPresenter : MonoBehaviour
    {
        [SerializeField] private CatTapInput tapInput;
        [SerializeField] private CatRoster roster;
        [SerializeField] private CatGiftMarkersView markers;
        [SerializeField] private CatGiftPopView pop;

        private readonly List<Collider> waiting = new List<Collider>();
        private ICatGiftService gifts;

        public void Initialize(ICatGiftService giftService)
        {
            gifts = giftService;
            tapInput.OnCatTapped += HandleTouch;
            roster.OnRosterChanged += RefreshMarkers;
            RefreshMarkers();
        }

        private void OnDestroy()
        {
            if (gifts == null)
            {
                return;
            }

            tapInput.OnCatTapped -= HandleTouch;
            roster.OnRosterChanged -= RefreshMarkers;
        }

        private void HandleTouch(CatBrain cat)
        {
            int coins = gifts.Collect(cat.View.CatId);

            if (coins <= 0)
            {
                return;
            }

            pop.Play(cat.View.TapCollider.bounds.max, coins);
            RefreshMarkers();
        }

        private void RefreshMarkers()
        {
            waiting.Clear();

            for (int i = 0; i < roster.Active.Count; i++)
            {
                CatView cat = roster.Active[i].View;

                if (gifts.HasGift(cat.CatId))
                {
                    waiting.Add(cat.TapCollider);
                }
            }

            markers.Show(waiting);
        }

#if UNITY_EDITOR
        public void EditorLink(CatTapInput linkedTapInput, CatRoster linkedRoster, CatGiftMarkersView linkedMarkers, CatGiftPopView linkedPop)
        {
            tapInput = linkedTapInput;
            roster = linkedRoster;
            markers = linkedMarkers;
            pop = linkedPop;
        }
#endif
    }
}
