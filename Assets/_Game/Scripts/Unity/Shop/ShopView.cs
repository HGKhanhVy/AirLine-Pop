using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The shop sheet: three shelves, snacks, planes and themes, switched by the tabs at the
    /// top, and a line that says how many snacks the player has or how the last purchase
    /// went. The cards on every shelf are built into the scene, one per item.
    /// </summary>
    public sealed class ShopView : MonoBehaviour
    {
        [SerializeField] private Button[] tabs = new Button[0];
        [SerializeField] private Image[] tabFaces = new Image[0];
        [SerializeField] private GameObject[] shelves = new GameObject[0];
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private ShopItemCardView[] snackCards = new ShopItemCardView[0];
        [SerializeField] private ShopItemCardView[] planeCards = new ShopItemCardView[0];
        [SerializeField] private ShopItemCardView[] themeCards = new ShopItemCardView[0];

        [Header("Tab faces")]
        [SerializeField] private Sprite selectedTab;
        [SerializeField] private Sprite idleTab;

        public event Action<int> OnSnackPressed;
        public event Action<int> OnPlanePressed;
        public event Action<int> OnThemePressed;
        public event Action OnShelfChanged;

        public ShopShelf Shelf { get; private set; }

        public ShopItemCardView SnackCard(int index) => snackCards[index];

        public ShopItemCardView PlaneCard(int index) => planeCards[index];

        public ShopItemCardView ThemeCard(int index) => themeCards[index];

        public int SnackCardCount => snackCards.Length;

        public int PlaneCardCount => planeCards.Length;

        public int ThemeCardCount => themeCards.Length;

        private void OnEnable()
        {
            tabs[(int)ShopShelf.Snacks].onClick.AddListener(ShowSnacks);
            tabs[(int)ShopShelf.Planes].onClick.AddListener(ShowPlanes);
            tabs[(int)ShopShelf.Themes].onClick.AddListener(ShowThemes);
            Listen(snackCards, HandleSnack, true);
            Listen(planeCards, HandlePlane, true);
            Listen(themeCards, HandleTheme, true);
            ShowShelf(Shelf);
        }

        private void OnDisable()
        {
            tabs[(int)ShopShelf.Snacks].onClick.RemoveListener(ShowSnacks);
            tabs[(int)ShopShelf.Planes].onClick.RemoveListener(ShowPlanes);
            tabs[(int)ShopShelf.Themes].onClick.RemoveListener(ShowThemes);
            Listen(snackCards, HandleSnack, false);
            Listen(planeCards, HandlePlane, false);
            Listen(themeCards, HandleTheme, false);
        }

        public void SetStatus(string text)
        {
            statusLabel.text = text;
        }

        private static void Listen(ShopItemCardView[] cards, Action<ShopItemCardView> handler, bool isOn)
        {
            for (int i = 0; i < cards.Length; i++)
            {
                if (isOn)
                {
                    cards[i].OnPressed += handler;
                }
                else
                {
                    cards[i].OnPressed -= handler;
                }
            }
        }

        private void ShowSnacks()
        {
            ShowShelf(ShopShelf.Snacks);
        }

        private void ShowPlanes()
        {
            ShowShelf(ShopShelf.Planes);
        }

        private void ShowThemes()
        {
            ShowShelf(ShopShelf.Themes);
        }

        private void ShowShelf(ShopShelf shelf)
        {
            Shelf = shelf;

            for (int i = 0; i < shelves.Length; i++)
            {
                bool isShown = i == (int)shelf;
                shelves[i].SetActive(isShown);
                tabFaces[i].sprite = isShown ? selectedTab : idleTab;
            }

            OnShelfChanged?.Invoke();
        }

        private void HandleSnack(ShopItemCardView card)
        {
            OnSnackPressed?.Invoke(Array.IndexOf(snackCards, card));
        }

        private void HandlePlane(ShopItemCardView card)
        {
            OnPlanePressed?.Invoke(Array.IndexOf(planeCards, card));
        }

        private void HandleTheme(ShopItemCardView card)
        {
            OnThemePressed?.Invoke(Array.IndexOf(themeCards, card));
        }

#if UNITY_EDITOR
        public void EditorLink(Button[] linkedTabs, Image[] linkedFaces, GameObject[] linkedShelves, TMP_Text linkedStatus,
            ShopItemCardView[] linkedSnackCards, ShopItemCardView[] linkedPlaneCards, ShopItemCardView[] linkedThemeCards,
            Sprite linkedSelected, Sprite linkedIdle)
        {
            tabs = linkedTabs;
            tabFaces = linkedFaces;
            shelves = linkedShelves;
            statusLabel = linkedStatus;
            snackCards = linkedSnackCards;
            planeCards = linkedPlaneCards;
            themeCards = linkedThemeCards;
            selectedTab = linkedSelected;
            idleTab = linkedIdle;
        }
#endif
    }
}
