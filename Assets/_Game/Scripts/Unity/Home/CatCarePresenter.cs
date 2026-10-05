using System.Collections;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Runs caring for a regular. A touch only holds the cat still to react (see
    /// <see cref="CatTouchReactor"/>) and pops a paw button over it; pressing that opens the
    /// care card, whose buttons become care actions that play their animation and answer in
    /// the card's status line. The card never jumps up over a reaction the player just
    /// started. Bond and snacks live in <see cref="ICatCareService"/>; this only drives the moment.
    /// </summary>
    public sealed class CatCarePresenter : MonoBehaviour
    {
        [SerializeField] private CatTapInput tapInput;
        [SerializeField] private CatMenuView menu;
        [SerializeField] private CatCareBubbleView bubble;

        [Tooltip("Where the paw bubble's tail touches the cat, as shares of the cat's size: " +
                 "up to about the top of its head, and a little to the side, like a speech bubble.")]
        [SerializeField] private float bubbleHeadHeight = 0.42f;
        [SerializeField] private float bubbleSide = 0.18f;

        [SerializeField] private HomeTabController tabs;
        [SerializeField] private EconomyConfigSO economy;
        [SerializeField] private Transform viewer;

        [Header("How long each action keeps the cat busy, in seconds")]
        [SerializeField, Min(0.1f)] private float petSeconds = 1.8f;
        [SerializeField, Min(0.1f)] private float playSeconds = 1.2f;
        [SerializeField, Min(0.1f)] private float eatSeconds = 2.4f;
        [SerializeField, Min(0.1f)] private float refuseSeconds = 1f;

        [Header("Text")]

        private ICatCareService care;
        private ICatCollectionService collection;
        private CatBrain current;

        // The cat last touched, held still while its paw button is up.
        private CatBrain held;
        private Coroutine busy;

        /// <summary>The id of the cat whose menu is open; null when none is.</summary>
        public string CurrentCatId => current == null ? null : current.View.CatId;

        public void Initialize(ICatCareService careService, ICatCollectionService collectionService)
        {
            care = careService;
            collection = collectionService;
        }

        private void OnEnable()
        {
            tapInput.OnCatTapped += HandleTouch;
            bubble.OnPressed += HandleBubblePressed;
            bubble.OnExpired += HandleBubbleExpired;
            menu.OnPet += HandlePet;
            menu.OnPlay += HandlePlay;
            menu.OnFeed += HandleFeed;
            menu.OnClose += Close;
            tabs.OnTabChanged += HandleTabChanged;
        }

        private void OnDisable()
        {
            tapInput.OnCatTapped -= HandleTouch;
            bubble.OnPressed -= HandleBubblePressed;
            bubble.OnExpired -= HandleBubbleExpired;
            menu.OnPet -= HandlePet;
            menu.OnPlay -= HandlePlay;
            menu.OnFeed -= HandleFeed;
            menu.OnClose -= Close;
            tabs.OnTabChanged -= HandleTabChanged;
        }

        private void HandleTabChanged(HomeTab tab)
        {
            if (tab != HomeTab.Cats)
            {
                Close();
            }
        }

        /// <summary>
        /// A touch holds the cat facing the player for its reaction and offers the paw
        /// button. Touching the cat whose card is open leaves the card be.
        /// </summary>
        private void HandleTouch(CatBrain brain)
        {
            if (care == null || (menu.IsOpen && current == brain))
            {
                return;
            }

            if (menu.IsOpen)
            {
                Close();
            }

            if (held != null && held != brain)
            {
                held.Deselect();
            }

            held = brain;
            held.Select(viewer.position);
            Transform cat = held.View.Root;
            float size = cat.lossyScale.y;
            bubble.ShowBeside(cat.position + Vector3.up * (size * bubbleHeadHeight), viewer.right * (size * bubbleSide));
        }

        private void HandleBubblePressed()
        {
            if (held == null)
            {
                return;
            }

            CatBrain brain = held;
            held = null;
            Open(brain);
        }

        private void HandleBubbleExpired()
        {
            if (held != null && held != current)
            {
                held.Deselect();
            }

            held = null;
        }

        private void Open(CatBrain brain)
        {
            if (care == null)
            {
                return;
            }

            if (current != null && current != brain)
            {
                StopBusy();
                current.Deselect();
            }

            current = brain;
            current.Select(viewer.position);

            // Round the cat's own body, so the menu reads as choosing what to do with this cat.
            menu.PlaceAt(current.View.TapCollider.bounds);
            Refresh(string.Empty);
        }

        private void Close()
        {
            StopBusy();
            bubble.Hide();

            if (held != null && held != current)
            {
                held.Deselect();
            }

            held = null;

            if (current != null)
            {
                current.Deselect();
                current = null;
            }

            menu.Hide();
        }

        private void HandlePet()
        {
            if (!CanAct())
            {
                return;
            }

            int before = care.GetBondLevel(CatId);
            CareOutcome outcome = care.Pet(CatId);
            Perform(CatAnimatorParams.Pet, petSeconds);
            Refresh(Describe(outcome, economy.PetBond, before, Localization.Get("care.alreadyGreeted")));
        }

        private void HandlePlay()
        {
            if (!CanAct())
            {
                return;
            }

            int before = care.GetBondLevel(CatId);
            CareOutcome outcome = care.Play(CatId);
            Perform(CatAnimatorParams.Play, playSeconds);
            Refresh(Describe(outcome, economy.PlayBond, before, Localization.Get("care.alreadyPlayed")));
        }

        private void HandleFeed()
        {
            if (!CanAct())
            {
                return;
            }

            // Out of snacks, the button buys one instead; coins are what the snacks cost.
            if (care.FoodCount <= 0)
            {
                PurchaseOutcome bought = care.TryBuyFood(1);
                Refresh(Localization.Get(bought == PurchaseOutcome.Purchased ? "care.bought" : "care.noCoins"));
                return;
            }

            int before = care.GetBondLevel(CatId);
            CareOutcome outcome = care.Feed(CatId);

            if (outcome == CareOutcome.AlreadyFed)
            {
                Perform(CatAnimatorParams.Refuse, refuseSeconds);
                Refresh(Localization.Format("care.full", DisplayName));
                return;
            }

            Perform(CatAnimatorParams.Eat, eatSeconds);
            Refresh(Describe(outcome, economy.MealBond, before, string.Empty));
        }

        private string Describe(CareOutcome outcome, int bond, int levelBefore, string noBondText)
        {
            if (outcome != CareOutcome.BondGained)
            {
                return noBondText;
            }

            int levelAfter = care.GetBondLevel(CatId);

            return levelAfter > levelBefore
                ? Localization.Format("care.tierUp", economy.GetLoyaltyTier(levelAfter))
                : Localization.Format("care.bond", bond);
        }

        /// <summary>Plays an action on the cat and keeps it still until the action is over.</summary>
        private void Perform(int trigger, float seconds)
        {
            StopBusy();
            current.Occupy();
            current.View.Trigger(trigger);
            busy = StartCoroutine(ReleaseAfter(seconds));
        }

        private IEnumerator ReleaseAfter(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            busy = null;

            if (current != null)
            {
                current.Release(menu.IsOpen);
            }
        }

        private void StopBusy()
        {
            if (busy != null)
            {
                StopCoroutine(busy);
                busy = null;
            }
        }

        private bool CanAct()
        {
            return current != null && care != null && busy == null;
        }

        private void Refresh(string status)
        {
            int level = care.GetBondLevel(CatId);
            string feed = care.FoodCount > 0
                ? Localization.Get("care.feedShort")
                : Localization.Format("care.buyShort", care.FoodPrice);

            menu.Show(new CatMenuModel(DisplayName, economy.GetLoyaltyTier(level), care.GetBondProgress(CatId), feed, care.FoodCount, status));
        }

        private string CatId => current.View.CatId;

        private string DisplayName
        {
            get
            {
                CatSave save = collection?.GetSave(CatId);
                return save != null && !string.IsNullOrEmpty(save.name) ? save.name : CatId;
            }
        }

#if UNITY_EDITOR
        public void EditorLink(CatTapInput linkedInput, CatMenuView linkedMenu, HomeTabController linkedTabs,
            EconomyConfigSO linkedEconomy, Transform linkedViewer, CatCareBubbleView linkedBubble)
        {
            bubble = linkedBubble;
            tapInput = linkedInput;
            menu = linkedMenu;
            tabs = linkedTabs;
            economy = linkedEconomy;
            viewer = linkedViewer;
        }
#endif
    }
}
