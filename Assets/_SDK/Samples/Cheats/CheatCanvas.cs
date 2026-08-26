using System.Collections;
using ASTeams.Base;
using ASTeams.Base.Data;
using ASTeams.Base.Gameplay;
using ASTeams.Base.UI;
using TMPro;
using UnityEngine;

namespace ASTeams.Template
{
    public class CheatCanvas : MonoSingleton<CheatCanvas>
    {
        [Header("UI")]
        [SerializeField] private GameObject cheatRoot;
        [SerializeField] private UIBaseButton prevBtn;
        [SerializeField] private TMP_InputField levelInput;
        [SerializeField] private UIBaseButton loadBtn;
        [SerializeField] private UIBaseButton nextBtn;
        [SerializeField] private UIBaseButton winBtn;
        [SerializeField] private UIBaseButton failBtn;
        [SerializeField] private UIBaseButton coinBtn;

        [Header("Gesture - Editor Mouse (5 clicks in 1s)")]
        [SerializeField] private int requiredClicks = 5;
        [SerializeField] private float windowSeconds = 1.0f;

        private int clickCount;
        private float firstClickTime = -1f;
        private LevelService levelService;

        private IEnumerator Start()
        {
            if (cheatRoot != null)
            {
                cheatRoot.SetActive(false);
            }

            yield return new WaitForEndOfFrame();
            yield return new WaitForSeconds(3f);

            ResolveReferences();
            BindCheatButtons();

            UserProfileController.Instance.OnUserChanged.AddListener(FetchData);
            FetchData(null);
        }

        private void ResolveReferences()
        {
            if (GameController.Instance != null)
            {
                GameController.Instance.Services.TryGet(out levelService);
            }
        }

        private void BindCheatButtons()
        {
            prevBtn.onClick.AddListener(HandlePreviousLevelClicked);
            nextBtn.onClick.AddListener(HandleNextLevelClicked);
            loadBtn.onClick.AddListener(HandleLoadLevelClicked);

            winBtn.onClick.AddListener(() =>
            {
                var service = GameController.Instance.Services.Get<GameStateService>();
                service.Win();
            });

            failBtn.onClick.AddListener(() =>
            {
                var service = GameController.Instance.Services.Get<GameStateService>();
                service.Lose(FailType.TimeUp);
            });

            coinBtn.onClick.AddListener(() =>
            {
                UserProfileController.Instance.AddCoin(1000);
            });
        }

        private void HandlePreviousLevelClicked()
        {
            if (TryLoadLevel(levelService != null ? levelService.CurrentLevelNumber - 1 : 1))
            {
                RefreshLevelInput();
                return;
            }

            UserProfileController.Instance.LEVEL -= 1;
            UISceneController.Instance.ReloadCurrentScene();
        }

        private void HandleNextLevelClicked()
        {
            var nextLevel = levelService != null ? levelService.CurrentLevelNumber + 1 : UserProfileController.Instance.LEVEL + 1;
            if (TryLoadLevel(nextLevel))
            {
                RefreshLevelInput();
                return;
            }

            UserProfileController.Instance.LEVEL += 1;
            UISceneController.Instance.ReloadCurrentScene();
        }

        private void HandleLoadLevelClicked()
        {
            if (!int.TryParse(levelInput.text, out var levelNumber))
            {
                return;
            }

            if (TryLoadLevel(levelNumber))
            {
                RefreshLevelInput();
                return;
            }

            UserProfileController.Instance.LEVEL = levelNumber;
            UISceneController.Instance.ReloadCurrentScene();
        }

        private bool TryLoadLevel(int levelNumber)
        {
            if (levelService == null)
            {
                return false;
            }

            var clampedLevel = Mathf.Max(1, levelNumber);
            if (levelService.LevelCount > 0)
            {
                clampedLevel = Mathf.Clamp(clampedLevel, 1, levelService.LevelCount);
            }

            levelService.LoadLevel(clampedLevel);
            return true;
        }

        private void RefreshLevelInput()
        {
            if (levelInput == null)
            {
                return;
            }

            if (levelService != null)
            {
                levelInput.text = levelService.CurrentLevelNumber.ToString();
                return;
            }

            levelInput.text = UserProfileController.Instance.LEVEL.ToString();
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                RegisterClick();
            }
        }

        private void RegisterClick()
        {
            var now = Time.unscaledTime;

            if (firstClickTime < 0f)
            {
                firstClickTime = now;
                clickCount = 1;
                return;
            }

            if (now - firstClickTime > windowSeconds)
            {
                firstClickTime = now;
                clickCount = 1;
                return;
            }

            clickCount++;

            if (clickCount >= requiredClicks)
            {
                ToggleCheat();
                ResetWindow();
            }
        }

        private void ResetWindow()
        {
            clickCount = 0;
            firstClickTime = -1f;
        }

        private void ToggleCheat()
        {
            if (cheatRoot != null)
            {
                cheatRoot.SetActive(!cheatRoot.activeSelf);
            }
            else
            {
                gameObject.SetActive(!gameObject.activeSelf);
            }

            if (cheatRoot != null && cheatRoot.activeSelf)
            {
                RefreshLevelInput();
            }
        }

        private void FetchData(object data)
        {
            RefreshLevelInput();
        }
    }
}
