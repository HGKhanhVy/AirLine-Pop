using ASTeams.Base.Data;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Paints the board with the theme the player wears: the tiles and the start pad, and
    /// the sky gradient behind the board. Repaints as soon as the shop changes the theme.
    /// </summary>
    public sealed class BoardSkinPresenter : MonoBehaviour
    {
        [SerializeField] private BoardView boardView;

        [Tooltip("Asked to repaint when the skin changes while a level is open.")]
        [SerializeField] private GameplayController controller;
        [SerializeField] private Camera boardCamera;
        [SerializeField] private SkinCatalogSO catalog;

        [Tooltip("The gradient behind the board, given the theme's own sky.")]
        [SerializeField] private SpriteRenderer sky;

        [Tooltip("The runway: its centre marks are the path's line, its tarmac the line under it.")]
        [SerializeField] private PathView path;
        [SerializeField] private LineRenderer runway;
        [SerializeField] private SpriteRenderer[] clouds = new SpriteRenderer[0];

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

#if UNITY_EDITOR
        public void EditorLink(BoardView linkedBoard, GameplayController linkedController, Camera linkedCamera, SkinCatalogSO linkedCatalog,
            SpriteRenderer linkedSky, PathView linkedPath, LineRenderer linkedRunway, SpriteRenderer[] linkedClouds)
        {
            path = linkedPath;
            runway = linkedRunway;
            clouds = linkedClouds;
            boardView = linkedBoard;
            controller = linkedController;
            boardCamera = linkedCamera;
            catalog = linkedCatalog;
            sky = linkedSky;
        }
#endif

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

            if (sky != null && skin.BoardSky != null)
            {
                sky.sprite = skin.BoardSky;
            }

            if (path != null && skin.RunwayMarks != null)
            {
                path.SetDashMaterial(skin.RunwayMarks);
            }

            if (runway != null && skin.RunwaySurface != null)
            {
                runway.sharedMaterial = skin.RunwaySurface;
            }

            for (int i = 0; i < clouds.Length; i++)
            {
                clouds[i].color = skin.CloudTint;
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
