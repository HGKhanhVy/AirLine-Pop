using ASTeams.Base.Data;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Paints the board with the skin the player wears. The colours still turn with the
    /// level the way the reference game does; the skin decides which colours turn.
    /// </summary>
    public sealed class BoardSkinPresenter : MonoBehaviour
    {
        [SerializeField] private BoardView boardView;

        [Tooltip("Asked to repaint when the skin changes while a level is open.")]
        [SerializeField] private GameplayController controller;
        [SerializeField] private Camera boardCamera;
        [SerializeField] private SkinCatalogSO catalog;

        private ISkinService skins;
        private int levelNumber = 1;

        private void Awake()
        {
            skins = new ProfileSkinService(catalog, UserProfileController.Instance);
        }

        private void OnEnable()
        {
            GameplayEvents.OnLevelLoaded += HandleLevelLoaded;
            ProfileSkinService.AnyChanged += Apply;
            Apply();
        }

        private void OnDisable()
        {
            GameplayEvents.OnLevelLoaded -= HandleLevelLoaded;
            ProfileSkinService.AnyChanged -= Apply;
        }

        private void HandleLevelLoaded(int loadedLevel, string levelId, int difficulty, int totalCells)
        {
            levelNumber = loadedLevel;
            Apply();
        }

        private void Apply()
        {
            SkinSO skin = skins.Equipped;

            if (skin == null || boardView == null)
            {
                return;
            }

            boardView.SetPalette(skin.GetPalette(levelNumber));

            if (boardCamera != null)
            {
                boardCamera.backgroundColor = skin.Background;
            }

            // The squares take their colour as they are set, so a board already on screen
            // has to be repainted; the path the player drew is untouched.
            if (controller != null)
            {
                controller.RepaintVisuals();
            }
        }
    }
}
