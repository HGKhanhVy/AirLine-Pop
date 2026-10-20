using UnityEngine;

namespace AirlinePop.SingleLine.Unity
{
    /// <summary>
    /// Composition root of the gameplay HUD: builds the settings and navigation services
    /// over the app's audio, haptics and scene cover, finds the player's companion cat in the save, and hands
    /// them to the pause and win controllers. The views below never look anything up.
    /// </summary>
    public sealed class GameplayHudBootstrap : MonoBehaviour
    {
        [SerializeField] private CatCatalogSO catalog;
        [SerializeField] private PauseController pause;
        [SerializeField] private WinSequenceController win;
        [SerializeField] private CatPortraitStage portraitStage;

        [Tooltip("His Majesty the VIP guest, shown on the win card after a VIP flight.")]
        [SerializeField] private CatView vipGuest;

        // Start rather than Awake: the app root's services register in their own Awake.
        private void Start()
        {
            PlayerProfile profile = PlayerProfile.Instance;
            var settings = new SettingsService(GameAudio.Instance, DeviceHaptics.Instance);
            var navigator = new SceneNavigator(SceneTransition.Instance);

            portraitStage.Initialize(FindCompanion(profile));
            portraitStage.InitializeGuest(vipGuest);
            pause.Initialize(settings, navigator);
            win.Initialize(portraitStage, catalog);
        }

        private CatView FindCompanion(PlayerProfile profile)
        {
            if (profile == null || catalog == null)
            {
                return null;
            }

            CatBreedSO breed = catalog.Find(new ProfileCollectionStore(profile).State.companionCatId);

            // A save from before the cat room has no companion yet; show the starter.
            if (breed == null && catalog.Breeds.Count > 0)
            {
                breed = catalog.Breeds[0];
            }

            return breed == null ? null : breed.Prefab;
        }

#if UNITY_EDITOR
        public void EditorLink(CatCatalogSO linkedCatalog, PauseController linkedPause, WinSequenceController linkedWin, CatPortraitStage linkedStage,
            CatView linkedVipGuest)
        {
            vipGuest = linkedVipGuest;
            catalog = linkedCatalog;
            pause = linkedPause;
            win = linkedWin;
            portraitStage = linkedStage;
        }
#endif
    }
}
