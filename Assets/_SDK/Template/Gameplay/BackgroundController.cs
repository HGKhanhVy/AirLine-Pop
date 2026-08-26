using ASTeams.Base.Level;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.Base.Gameplay
{
    public sealed class BackgroundController : MonoBehaviour
    {
        [Header("Background Target")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private SpriteRenderer backgroundRenderer;

        [Header("Background Sprites")]
        [SerializeField] private Sprite easySprite;
        [SerializeField] private Sprite hardSprite;
        [SerializeField] private Sprite superHardSprite;

        [Header("Service")]
        [SerializeField] private LevelService levelService;

        private bool isBound;

        private void OnEnable()
        {
            BindLevelService();
            ApplyCurrentLevelBackground();
        }

        private void OnDisable()
        {
            UnbindLevelService();
        }

        private void BindLevelService()
        {
            if (isBound)
            {
                return;
            }

            ResolveLevelService();

            if (levelService == null)
            {
                return;
            }

            levelService.OnLevelLoaded += LevelLoadedHandler;
            isBound = true;
        }

        private void UnbindLevelService()
        {
            if (!isBound || levelService == null)
            {
                isBound = false;
                return;
            }

            levelService.OnLevelLoaded -= LevelLoadedHandler;
            isBound = false;
        }

        private void ResolveLevelService()
        {
            if (levelService != null)
            {
                return;
            }

            if (GameController.Instance == null || GameController.Instance.Services == null)
            {
                return;
            }

            GameController.Instance.Services.TryGet(out levelService);
        }

        private void LevelLoadedHandler(int levelNumber, LevelConfig levelConfig)
        {
            ApplyBackground(levelConfig);
        }

        private void ApplyCurrentLevelBackground()
        {
            if (levelService == null || levelService.CurrentLevel == null)
            {
                return;
            }

            ApplyBackground(levelService.CurrentLevel);
        }

        private void ApplyBackground(LevelConfig levelConfig)
        {
            if (levelConfig == null)
            {
                ApplySprite(easySprite);
                return;
            }

            switch (levelConfig.type)
            {
                case LevleType.SuperHard:
                    ApplySprite(superHardSprite);
                    break;

                case LevleType.Hard:
                    ApplySprite(hardSprite);
                    break;

                default:
                    ApplySprite(easySprite);
                    break;
            }
        }

        private void ApplySprite(Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            if (backgroundImage != null)
            {
                backgroundImage.sprite = sprite;
            }

            if (backgroundRenderer != null)
            {
                backgroundRenderer.sprite = sprite;
            }
        }
    }
}
