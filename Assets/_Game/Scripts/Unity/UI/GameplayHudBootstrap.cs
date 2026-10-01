using ASTeams.Base;
using ASTeams.Base.Data;
using ASTeams.Base.UI;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Composition root of the gameplay HUD: builds the settings and navigation services
    /// over the SDK singletons, finds the player's companion cat in the save, and hands
    /// them to the pause and win controllers. The views below never look anything up.
    /// </summary>
    public sealed class GameplayHudBootstrap : MonoBehaviour
    {
        [SerializeField] private CatCatalogSO catalog;
        [SerializeField] private PauseController pause;
        [SerializeField] private WinSequenceController win;
        [SerializeField] private CatPortraitStage portraitStage;

        // Start rather than Awake: the SDK singletons load their saves in their own Awake.
        private void Start()
        {
            UserProfileController profile = UserProfileController.Instance;
            var settings = new SdkSettingsService(AudioController.Instance, VibrationController.Instance, profile);
            var navigator = new SdkSceneNavigator(UISceneController.Instance);

            portraitStage.Initialize(FindCompanion(profile));
            pause.Initialize(settings, navigator);
            win.Initialize(portraitStage, navigator);
        }

        private CatView FindCompanion(UserProfileController profile)
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
        public void EditorLink(CatCatalogSO linkedCatalog, PauseController linkedPause, WinSequenceController linkedWin, CatPortraitStage linkedStage)
        {
            catalog = linkedCatalog;
            pause = linkedPause;
            win = linkedWin;
            portraitStage = linkedStage;
        }
#endif
    }
}
