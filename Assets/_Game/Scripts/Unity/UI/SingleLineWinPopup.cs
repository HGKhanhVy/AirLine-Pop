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
    /// asks for, but nothing on screen says so yet: every coin label still reads the old
    /// balance. This screen is what pays the player as far as they are concerned, so it
    /// raises the new balance on the channel as the coins land, and the header behind it
    /// climbs in step with the number on the panel. The flight is the template's own coin
    /// package, played for the feel of it.
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

        [Tooltip("The running total the coins fly into. It counts up as they land, so the " +
                 "number and the coins tell the same story.")]
        [SerializeField] private TMP_Text coinTotalText;


        [SerializeField, Min(0f)] private float afterCollectDelay = 0.15f;

        [Tooltip("One chime per coin as it lands.")]
        [SerializeField] private AudioClip coinClip;

        [Tooltip("The chime plays through its own source so each coin cuts the one before " +
                 "it. Coins land 30 to 90 ms apart and the chime rings for half a second, " +
                 "so shared one-shot sources stacked six and eight of them into a wash. " +
                 "Cut short they read as what they are: separate coins.")]
        [SerializeField] private AudioSource coinSource;

        [SerializeField, Range(0f, 1f)] private float coinVolume = 0.35f;

        private int reward;
        private long balance;
        private long shownTotal;
        private bool isClosing;
        private bool isCollecting;
        private WinPopupFeedback feedback;

        // Resolved on the first win rather than in Awake: the SDK services live on the
        // managers prefab, which is guaranteed to be up by the time a level is finished.
        private WinPopupFeedback Feedback => feedback ??= CreateFeedback();

        /// <summary>Fills the screen in and opens it. The caller owns when that happens.</summary>
        public void Present(int levelNumber, int coinReward, long coinBalance)
        {
            reward = coinReward;
            balance = coinBalance;
            isClosing = false;
            isCollecting = false;

            // The coins are already in the profile, so this would open on the final number.
            // Start from what the player had, and let the flight add them.
            shownTotal = coinBalance - coinReward;
            ShowTotal(shownTotal);

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
            Feedback.PlayOpen();

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
                Feedback.PlayReveal();
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

            Feedback.PlayContinue();

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
                coinSpawner.CollectCoinsFromUI(StartInSpawnerSpace(), reward, AddToTotal, () => arrived = true);

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

            // Whatever the flight managed to count, the player is owed the whole amount.
            ShowTotal(balance);
            GameplayEvents.RaiseCoinBalanceChanged(balance);
            Continue();
        }

        /// <summary>One landed coin is worth a share of the reward, so the number climbs with them.</summary>
        private void AddToTotal(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            shownTotal = System.Math.Min(balance, shownTotal + amount);
            ShowTotal(shownTotal);

            Feedback.PlayCoin();

            // Every other coin label climbs with this one. Leaving them behind would have
            // the header disagree with the panel for as long as the screen is open.
            GameplayEvents.RaiseCoinBalanceChanged(shownTotal);
        }

        private WinPopupFeedback CreateFeedback()
        {
            VibrationController vibration = VibrationController.Instance;
            IHapticService haptics = vibration == null ? null : new SdkHapticService(vibration);
            return new WinPopupFeedback(AudioController.Instance, haptics, coinClip, coinSource, coinVolume);
        }

        private void ShowTotal(long value)
        {
            if (coinTotalText != null)
            {
                coinTotalText.SetText("{0}", value);
            }
        }

        private void Continue()
        {
            if (isClosing)
            {
                return;
            }

            isClosing = true;
            Hide();
            GameplayEvents.RequestNextLevel();
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
