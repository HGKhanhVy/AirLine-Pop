using System;
using ASTeams.Base.Ads;
using ASTeams.Base.Data;
#if IAP
using ASTeams.Base.IAP;
#endif

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Purchases through the SDK's IAP controller.
    ///
    /// The SDK only compiles its store with the IAP define, which also needs the Unity
    /// Purchasing package and product ids on the store. Until then this stands in: Editor
    /// and development builds grant the purchase so the flow can be tried, a release
    /// build refuses it. Turning the define on switches to real purchases with no change
    /// to the screens that use this.
    ///
    /// What a product gives is granted here in both cases. The SDK's default reward
    /// handler belongs to another game (lives, boosters) and must not be registered for
    /// these products.
    /// </summary>
    public sealed class SdkPurchaseService : IPurchaseService
    {
        // Mirror of the ads controller's save key; the SDK is read-only here.
        private const string NoAdsKey = "isNoAds";
        private const string StarterPackKey = "starterPackOwned";
        private const string OwnedThemePrefix = "theme_owned_";

        private readonly AdsController ads;
        private readonly UserProfileController profile;
        private readonly StoreConfigSO config;

        public SdkPurchaseService(AdsController ads, UserProfileController profile, StoreConfigSO config)
        {
            this.ads = ads;
            this.profile = profile;
            this.config = config;
        }

        public bool IsOwned(StoreProduct product)
        {
            return profile != null && profile.GetParam<bool>(OwnershipKey(product));
        }

        public string GetPrice(StoreProduct product)
        {
#if IAP
            IAPController store = IAPController.Instance;
            UnityEngine.Purchasing.Product item = store == null ? null : store.GetUnityProduct(IAPIdMap.GetID(ToSdk(product)));

            if (item != null && !string.IsNullOrEmpty(item.metadata.localizedPriceString))
            {
                return item.metadata.localizedPriceString;
            }
#endif
            return config == null ? string.Empty : config.GetFallbackPrice(product);
        }

        public void Buy(StoreProduct product, Action<bool> onFinished)
        {
            if (IsOwned(product))
            {
                onFinished?.Invoke(true);
                return;
            }

#if IAP
            IAPController store = IAPController.Instance;

            if (store == null)
            {
                onFinished?.Invoke(false);
                return;
            }

            store.BuyProduct(ToSdk(product), isPurchased =>
            {
                if (isPurchased)
                {
                    Grant(product);
                }

                onFinished?.Invoke(isPurchased);
            });
#elif UNITY_EDITOR || DEVELOPMENT_BUILD
            Grant(product);
            onFinished?.Invoke(true);
#else
            onFinished?.Invoke(false);
#endif
        }

        public void Restore(Action<bool> onFinished)
        {
            // Google Play hands owned non-consumables back when the store starts, and they
            // are applied then; restoring here means reporting what is now owned.
            onFinished?.Invoke(IsOwned(StoreProduct.RemoveAds) || IsOwned(StoreProduct.StarterPack));
        }

        private void Grant(StoreProduct product)
        {
            if (product == StoreProduct.StarterPack && profile != null)
            {
                profile.SetParam(StarterPackKey, true);

                if (config != null)
                {
                    profile.AddCoin(config.StarterPackCoins);
                    profile.SetParam(OwnedThemePrefix + config.StarterPackThemeId, true);
                }
            }

            // Both products remove ads. The setter saves the flag and takes the banner down.
            if (ads != null)
            {
                ads.IsNoAds = true;
            }
        }

        private static string OwnershipKey(StoreProduct product)
        {
            return product == StoreProduct.StarterPack ? StarterPackKey : NoAdsKey;
        }

#if IAP
        private static IAPProductType ToSdk(StoreProduct product)
        {
            return product == StoreProduct.StarterPack ? IAPProductType.NoAdsBundle : IAPProductType.NoAds;
        }
#endif
    }
}
