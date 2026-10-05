using System;
using System.Collections.Generic;
using ASTeams.Base.Data;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>Liveries kept in the saved profile, the same way board skins are.</summary>
    public sealed class ProfileLiveryService : ILiveryService
    {
        private const string OwnedPrefix = "livery_owned_";
        private const string EquippedKey = "equippedLivery";

        private readonly LiveryCatalogSO catalog;
        private readonly UserProfileController profile;

        public ProfileLiveryService(LiveryCatalogSO catalog, UserProfileController profile)
        {
            this.catalog = catalog;
            this.profile = profile;
        }

        public IReadOnlyList<LiverySO> Liveries => catalog == null ? Array.Empty<LiverySO>() : catalog.Liveries;

        public event Action Changed;

        /// <summary>
        /// Raised by every instance, so buying in the shop repaints the plane parked on Home
        /// even though each reads the profile through its own service.
        /// </summary>
        public static event Action AnyChanged;

        public LiverySO Equipped
        {
            get
            {
                if (catalog == null)
                {
                    return null;
                }

                LiverySO saved = catalog.Find(profile == null ? null : profile.GetParam<string>(EquippedKey));
                return saved != null && IsOwned(saved) ? saved : catalog.Default;
            }
        }

        public bool IsOwned(LiverySO livery)
        {
            if (livery == null)
            {
                return false;
            }

            return livery.IsOwnedByDefault || (profile != null && profile.GetParam<bool>(OwnedPrefix + livery.Id));
        }

        public bool TryBuy(LiverySO livery)
        {
            if (livery == null || IsOwned(livery) || profile == null || !profile.UseCoin(livery.Price))
            {
                return false;
            }

            profile.SetParam(OwnedPrefix + livery.Id, true);
            GameplayEvents.RaiseCoinBalanceChanged(profile.userData.coin);
            Announce();
            return true;
        }

        public void Equip(LiverySO livery)
        {
            if (livery == null || !IsOwned(livery) || profile == null)
            {
                return;
            }

            profile.SetParam(EquippedKey, livery.Id);
            Announce();
        }

        /// <summary>Drops subscribers between play sessions, as the domain survives them.</summary>
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
