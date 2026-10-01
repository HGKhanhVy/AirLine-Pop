using System;
using System.Collections.Generic;
using ASTeams.Base;
using ASTeams.Base.Data;
using ASTeams.Base.UI;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Composition root of the Home scene: builds the economy services over the saved
    /// profile and hands them to the views. Nothing below this class creates a service or
    /// looks one up, so each view can be reused or tested with its own stand-ins.
    /// </summary>
    public sealed class HomeBootstrap : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private EconomyConfigSO economy;
        [SerializeField] private CatCatalogSO catalog;

        [Header("Views")]
        [SerializeField] private CoinCounterView coins;
        [SerializeField] private AirportPanelView airport;
        [SerializeField] private HomeTabController tabs;
        [SerializeField] private CatRoster airportCats;
        [SerializeField] private CatRoster roomCats;

        [Tooltip("Optional. The route map: where the next flight goes, and the postcard album.")]
        [SerializeField] private DestinationCatalogSO destinations;

        [SerializeField] private PostcardAlbumView album;
        [SerializeField] private SettingsPanelView settingsPanel;

        [Header("Lounge")]
        [SerializeField] private CatCarePresenter carePresenter;
        [SerializeField] private ArrivalToastView arrivalToast;
        [SerializeField] private DeparturesBoardView departures;

        private ProfileWallet wallet;
        private ISceneNavigator navigator;
        private bool isLeaving;

        public ICatCollectionService Collection { get; private set; }

        public ICatCareService Care { get; private set; }

        // Start rather than Awake: the SDK profile singleton loads its save in its own Awake.
        private void Start()
        {
            UserProfileController profile = UserProfileController.Instance;
            var store = new ProfileCollectionStore(profile);
            CollectionSeeder.EnsureSeeded(store, catalog, economy);

            wallet = new ProfileWallet(profile);
            Collection = new CatCollectionService(store, catalog, economy);
            Care = new CatCareService(store, Collection, economy, new LocalDayClock());
            navigator = new SdkSceneNavigator(UISceneController.Instance);

            int nextFlight = Math.Max(1, new UserProfileLevelProgressStore(profile).CurrentLevelNumber);

            // Regulars who have earned their place join before the rosters spawn, so a
            // new arrival is already in the lounge when it is announced.
            IReadOnlyList<CatBreedSO> arrived = Collection.WelcomeArrivals(nextFlight);

            coins.Initialize(wallet);
            airportCats.Initialize(Collection, catalog);
            roomCats.Initialize(Collection, catalog);

            Destination destination = destinations != null ? destinations.ForFlight(nextFlight) : null;
            airport.SetLevel(nextFlight, destination);

            if (album != null)
            {
                album.Initialize(destinations, nextFlight);
            }

            if (settingsPanel != null)
            {
                settingsPanel.Initialize(new SdkSettingsService(AudioController.Instance, VibrationController.Instance, profile));
            }

            if (departures != null)
            {
                departures.Show(destinations, nextFlight);
            }

            if (carePresenter != null)
            {
                carePresenter.Initialize(Care, Collection);
            }

            if (arrivalToast != null)
            {
                arrivalToast.Announce(arrived);
            }

            airport.OnPlayRequested += HandlePlay;
            tabs.ShowInstant(HomeTab.Airport);
        }

        private void OnDestroy()
        {
            wallet?.Dispose();

            if (airport != null)
            {
                airport.OnPlayRequested -= HandlePlay;
            }
        }

        private void HandlePlay()
        {
            // A second tap during the transition would queue a second scene load.
            if (isLeaving)
            {
                return;
            }

            isLeaving = true;
            airport.SetInteractable(false);
            navigator.GoToGameplay();
        }

#if UNITY_EDITOR
        public void EditorLink(EconomyConfigSO linkedEconomy, CatCatalogSO linkedCatalog, CoinCounterView linkedCoins,
            AirportPanelView linkedAirport, HomeTabController linkedTabs, CatRoster linkedAirportCats, CatRoster linkedRoomCats)
        {
            economy = linkedEconomy;
            catalog = linkedCatalog;
            coins = linkedCoins;
            airport = linkedAirport;
            tabs = linkedTabs;
            airportCats = linkedAirportCats;
            roomCats = linkedRoomCats;
        }

        public void EditorLinkRoutes(DestinationCatalogSO linkedDestinations, PostcardAlbumView linkedAlbum)
        {
            destinations = linkedDestinations;
            album = linkedAlbum;
        }

        public void EditorLinkSettings(SettingsPanelView linkedSettings)
        {
            settingsPanel = linkedSettings;
        }

        public void EditorLinkLounge(CatCarePresenter linkedCare, ArrivalToastView linkedToast, DeparturesBoardView linkedDepartures)
        {
            carePresenter = linkedCare;
            arrivalToast = linkedToast;
            departures = linkedDepartures;
        }
#endif
    }
}
