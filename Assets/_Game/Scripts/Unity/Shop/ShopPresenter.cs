using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Runs the shop: fills the shelves from the economy, the plane catalogue and the
    /// themes, sells snack packs, planes and themes, and puts a bought plane or theme on.
    /// Keeps the cards in step with the coins, the snack stock and the language.
    /// </summary>
    public sealed class ShopPresenter : MonoBehaviour
    {
        [SerializeField] private ShopView view;

        private ILiveryService liveries;
        private ISkinService themes;
        private ICatCareService care;
        private EconomyConfigSO economy;
        private IWallet wallet;
        private string lastPurchase;

        public void Initialize(ILiveryService liveryService, ISkinService themeService, ICatCareService careService,
            EconomyConfigSO economyConfig, IWallet coinWallet)
        {
            liveries = liveryService;
            themes = themeService;
            care = careService;
            economy = economyConfig;
            wallet = coinWallet;

            view.OnSnackPressed += HandleSnack;
            view.OnPlanePressed += HandlePlane;
            view.OnThemePressed += HandleTheme;
            view.OnShelfChanged += HandleShelfChanged;
            wallet.OnCoinsChanged += HandleCoins;
            care.OnCareChanged += Refresh;
            liveries.Changed += Refresh;
            themes.Changed += Refresh;
            Localization.Service.OnLanguageChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (wallet == null)
            {
                return;
            }

            view.OnSnackPressed -= HandleSnack;
            view.OnPlanePressed -= HandlePlane;
            view.OnThemePressed -= HandleTheme;
            view.OnShelfChanged -= HandleShelfChanged;
            wallet.OnCoinsChanged -= HandleCoins;
            care.OnCareChanged -= Refresh;
            liveries.Changed -= Refresh;
            themes.Changed -= Refresh;
            Localization.Service.OnLanguageChanged -= Refresh;
        }

        private void HandleSnack(int index)
        {
            SnackPack pack = economy.SnackPacks[index];
            PurchaseOutcome outcome = care.TryBuyFoodPack(pack);
            lastPurchase = outcome == PurchaseOutcome.Purchased
                ? Localization.Format("shop.boughtSnacks", pack.Quantity)
                : Localization.Get("shop.noCoins");
            Refresh();
        }

        private void HandlePlane(int index)
        {
            LiverySO livery = liveries.Liveries[index];

            if (!liveries.IsOwned(livery))
            {
                if (!liveries.TryBuy(livery))
                {
                    lastPurchase = Localization.Get("shop.noCoins");
                    Refresh();
                    return;
                }

                lastPurchase = Localization.Format("shop.boughtLivery", livery.DisplayName);
            }

            liveries.Equip(livery);
        }

        private void HandleTheme(int index)
        {
            SkinSO theme = themes.Skins[index];

            if (!themes.IsOwned(theme))
            {
                if (!themes.TryBuy(theme))
                {
                    lastPurchase = Localization.Get("shop.noCoins");
                    Refresh();
                    return;
                }

                lastPurchase = Localization.Format("shop.boughtTheme", theme.DisplayName);
            }

            themes.Equip(theme);
        }

        private void HandleShelfChanged()
        {
            lastPurchase = null;
            Refresh();
        }

        private void HandleCoins(long coins)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (economy == null)
            {
                return;
            }

            RefreshSnacks();
            RefreshPlanes();
            RefreshThemes();
            view.SetStatus(lastPurchase ?? Hint());
        }

        private string Hint()
        {
            switch (view.Shelf)
            {
                case ShopShelf.Planes:
                    return Localization.Get("shop.planesHint");
                case ShopShelf.Themes:
                    return Localization.Get("shop.themesHint");
                default:
                    return Localization.Format("shop.snackStock", care.FoodCount);
            }
        }

        private void RefreshSnacks()
        {
            int shown = Mathf.Min(view.SnackCardCount, economy.SnackPacks.Count);

            for (int i = 0; i < shown; i++)
            {
                SnackPack pack = economy.SnackPacks[i];
                string packName = pack.Quantity == 1 ? Localization.Get("shop.snackPackOne") : Localization.Format("shop.snackPack", pack.Quantity);
                view.SnackCard(i).Show(pack.Artwork, packName, ShopCardState.Buy,
                    pack.Price, wallet.Coins >= pack.Price);
            }
        }

        private void RefreshPlanes()
        {
            int shown = Mathf.Min(view.PlaneCardCount, liveries.Liveries.Count);
            LiverySO equipped = liveries.Equipped;

            for (int i = 0; i < shown; i++)
            {
                LiverySO livery = liveries.Liveries[i];
                ShopCardState state = !liveries.IsOwned(livery) ? ShopCardState.Buy
                    : livery == equipped ? ShopCardState.InUse
                    : ShopCardState.Use;
                view.PlaneCard(i).Show(livery.PlaneParked, livery.DisplayName, state, livery.Price, wallet.Coins >= livery.Price);
            }
        }

        private void RefreshThemes()
        {
            int shown = Mathf.Min(view.ThemeCardCount, themes.Skins.Count);
            SkinSO equipped = themes.Equipped;

            for (int i = 0; i < shown; i++)
            {
                SkinSO theme = themes.Skins[i];
                ShopCardState state = !themes.IsOwned(theme) ? ShopCardState.Buy
                    : theme == equipped ? ShopCardState.InUse
                    : ShopCardState.Use;
                view.ThemeCard(i).Show(theme.Preview, theme.DisplayName, state, theme.Price, wallet.Coins >= theme.Price);
            }
        }

#if UNITY_EDITOR
        public void EditorLink(ShopView linkedView)
        {
            view = linkedView;
        }
#endif
    }
}
