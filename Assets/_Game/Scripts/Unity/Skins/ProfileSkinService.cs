using System;
using System.Collections.Generic;
using ASTeams.Base.Data;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Skins kept in the saved profile. Ownership uses the same key the Starter Pack
    /// writes, so a skin that arrives with a purchase is already owned here.
    /// </summary>
    public sealed class ProfileSkinService : ISkinService
    {
        private const string OwnedPrefix = "theme_owned_";
        private const string EquippedKey = "equippedTheme";

        private readonly SkinCatalogSO catalog;
        private readonly UserProfileController profile;

        public ProfileSkinService(SkinCatalogSO catalog, UserProfileController profile)
        {
            this.catalog = catalog;
            this.profile = profile;
        }

        public IReadOnlyList<SkinSO> Skins => catalog == null ? Array.Empty<SkinSO>() : catalog.Skins;

        public event Action Changed;

        /// <summary>
        /// Raised by every instance, so a screen that changes the skin reaches the board
        /// even though each of them reads the profile through its own service.
        /// </summary>
        public static event Action AnyChanged;

        public SkinSO Equipped
        {
            get
            {
                if (catalog == null)
                {
                    return null;
                }

                SkinSO saved = catalog.Find(profile == null ? null : profile.GetParam<string>(EquippedKey));

                // A skin the player no longer owns, or one taken out of the catalogue,
                // must not leave the board unpainted.
                return saved != null && IsOwned(saved) ? saved : catalog.Default;
            }
        }

        public bool IsOwned(SkinSO skin)
        {
            if (skin == null)
            {
                return false;
            }

            return skin.IsOwnedByDefault || (profile != null && profile.GetParam<bool>(OwnedPrefix + skin.Id));
        }

        public bool TryBuy(SkinSO skin)
        {
            if (skin == null || IsOwned(skin) || !skin.IsSoldInShop || profile == null || !profile.UseCoin(skin.Price))
            {
                return false;
            }

            Grant(skin);
            GameplayEvents.RaiseCoinBalanceChanged(profile.userData.coin);
            return true;
        }

        public void Grant(SkinSO skin)
        {
            if (skin == null || profile == null)
            {
                return;
            }

            profile.SetParam(OwnedPrefix + skin.Id, true);
            Announce();
        }

        public void Equip(SkinSO skin)
        {
            if (skin == null || !IsOwned(skin) || profile == null)
            {
                return;
            }

            profile.SetParam(EquippedKey, skin.Id);
            Announce();
        }

        /// <summary>
        /// Drops the subscribers between play sessions. The project keeps its domain
        /// alive across them, so a static event would otherwise hold dead objects.
        /// </summary>
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearSubscribers()
        {
            AnyChanged = null;
        }

        private void Announce()
        {
            Changed?.Invoke();
            AnyChanged?.Invoke();
        }
    }
}
