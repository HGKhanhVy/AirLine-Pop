using System.Collections;
using ASTeams.Base;
using ASTeams.Base.UI;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The win screen GDD 9.1 asks for: celebration, reward, next.
    ///
    /// The art is the template's own Popup Win, kept as it was drawn, but the flow is
    /// this game's. The template's version hands Continue to
    /// <c>GameResultHandleService.FinalizeWinAndReload</c>, which advances the profile
    /// level itself and reloads the whole scene between levels; this board loads the next
    /// level in place in a frame, and a scene reload for every win would throw that away.
    /// So Continue raises the next level request on the channel and the board answers it.
    ///
    /// The coins are already paid and saved by the time this opens, which is what GDD 14
    /// asks for. The flight is the template's own coin package, played for the feel of it.
    /// </summary>
    public sealed class SingleLineWinPopup : UIBasePopup
    {
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text rewardText;
        [SerializeField] private UIBaseButton continueButton;

        [Tooltip("The template's coin burst, reused. Optional: without it the popup still works.")]
        [SerializeField] private UICoinCollectSpawner coinSpawner;

        [Tooltip("Where the coins fly from. Falls back to the reward label.")]
        [SerializeField] private RectTransform coinFlyStart;

        [Tooltip("Asks the board for the next level, so this screen holds no gameplay reference.")]
        [SerializeField] private GameplayEventChannelSO eventChannel;

        [SerializeField, Min(0f)] private float afterCollectDelay = 0.15f;

        private int reward;
        private bool isClosing;
        private bool isCollecting;

        /// <summary>Fills the screen in and opens it. The caller owns when that happens.</summary>
        public void Present(int levelNumber, int coinReward)
        {
            reward = coinReward;
            isClosing = false;
            isCollecting = false;

            if (levelText != null)
            {
                levelText.SetText("Level {0}", levelNumber);
            }

            if (rewardText != null)
            {
                rewardText.SetText("+{0}", coinReward);
            }

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
            }

            Show();
        }

        public override void Show()
        {
            AudioController.Instance?.PlaySound(SoundName.UI_LevelComplete);
            VibrationController.Instance?.PlayMedium();

            canvasGroup.alpha = 0f;
            panel.localScale = Vector3.zero;
            gameObject.SetActive(true);

            // The pause between the fade and the pop is what makes the celebration read as
            // a moment rather than a menu: the confetti on this prefab plays into it.
            Sequence seq = DOTween.Sequence().SetUpdate(true);
            seq.Join(canvasGroup.DOFade(1f, ANIM_DURATION).SetUpdate(true));
            seq.AppendInterval(0.35f);
            seq.AppendCallback(() =>
            {
                AudioController.Instance?.PlaySound(SoundName.UI_Win);
                VibrationController.Instance?.PlayHeavy();
                panel.localScale = Vector3.one * 0.25f;
            });
            seq.Append(panel.DOScale(Vector3.one, ANIM_DURATION).SetEase(Ease.OutBack).SetUpdate(true));
            seq.OnComplete(ShowCompleted);
        }

        private void OnEnable()
        {
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(HandleContinue);
            }
        }

        private void OnDisable()
        {
            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(HandleContinue);
            }

            isClosing = false;
            isCollecting = false;
        }

        private void HandleContinue()
        {
            if (isClosing || isCollecting)
            {
                return;
            }

            AudioController.Instance?.PlaySound(SoundName.UI_ClaimReward);
            VibrationController.Instance?.PlayLight();

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(false);
            }

            StartCoroutine(CollectThenContinue());
        }

        private IEnumerator CollectThenContinue()
        {
            isCollecting = true;

            if (reward > 0 && coinSpawner != null)
            {
                bool arrived = false;
                coinSpawner.CollectCoinsFromUI(StartInSpawnerSpace(), reward, null, () => arrived = true);

                // The burst is on unscaled time, so this waits the same way.
                float guard = 0f;

                while (!arrived && guard < 3f)
                {
                    guard += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            if (afterCollectDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(afterCollectDelay);
            }

            isCollecting = false;
            Continue();
        }

        private void Continue()
        {
            if (isClosing)
            {
                return;
            }

            isClosing = true;
            Hide();
            eventChannel?.RequestNextLevel();
        }

        /// <summary>
        /// The spawner bursts inside its own rect, so the start point has to be expressed
        /// there however the popup is laid out on this screen.
        /// </summary>
        private Vector2 StartInSpawnerSpace()
        {
            RectTransform start = coinFlyStart != null
                ? coinFlyStart
                : (rewardText == null ? null : rewardText.rectTransform);

            var target = coinSpawner.transform as RectTransform;

            if (start == null || target == null)
            {
                return Vector2.zero;
            }

            Vector3 world = start.TransformPoint(start.rect.center);
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(target, screen, null, out Vector2 local);
            return local;
        }
    }
}
