using ASTeams.SingleLine.Core;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The gameplay screen furniture: which level this is, how much of it is covered, and
    /// the two things a player reaches for when a path goes wrong.
    ///
    /// The UI never touches the rules. It reads what the controller reports and calls the
    /// two public actions the controller exposes, so nothing here can put the board into a
    /// state the core would not allow.
    /// </summary>
    public sealed class GameplayHud : MonoBehaviour
    {
        [SerializeField] private LevelBootstrap bootstrap;
        [SerializeField] private GameplayController controller;

        [Tooltip("Buttons ask the board through this asset, so nothing here holds the board itself.")]
        [SerializeField] private GameplayEventChannelSO eventChannel;

        [Header("Readouts")]
        [SerializeField] private TMP_Text levelLabel;
        [SerializeField] private TMP_Text progressLabel;

        [Header("Buttons")]
        [SerializeField] private Button hintButton;
        [SerializeField] private Button undoButton;
        [SerializeField] private Button restartButton;

        [Header("Stuck")]
        [SerializeField] private CanvasGroup stuckBanner;
        [SerializeField, Min(0.01f)] private float bannerFadeSpeed = 4f;

        private float bannerTarget;
        private HintResult hintResult;
        private ITextCatalog textCatalog;
        private Tweener bannerFade;

        public void Initialize(ITextCatalog catalog)
        {
            textCatalog = catalog;
            Refresh();
        }

        private void EnsureBannerFade()
        {
            if (stuckBanner != null && (bannerFade == null || !bannerFade.IsActive()))
            {
                bannerFade = stuckBanner.DOFade(stuckBanner.alpha, 1f)
                    .SetEase(Ease.Linear).SetUpdate(true).SetAutoKill(false).Pause();
            }
        }

        private void OnEnable()
        {
            EnsureBannerFade();
            controller.OnStateChanged += HandleStateChanged;
            controller.OnPathChanged += Refresh;
            controller.OnHintReady += HandleHintReady;
            controller.OnHintChanged += Refresh;

            hintButton.onClick.AddListener(OnHintClicked);
            undoButton.onClick.AddListener(OnUndoClicked);
            restartButton.onClick.AddListener(OnRestartClicked);

            HandleStateChanged(controller.State, controller.State);
        }

        private void OnDisable()
        {
            bannerFade?.Pause();
            controller.OnStateChanged -= HandleStateChanged;
            controller.OnPathChanged -= Refresh;
            controller.OnHintReady -= HandleHintReady;
            controller.OnHintChanged -= Refresh;

            hintButton.onClick.RemoveListener(OnHintClicked);
            undoButton.onClick.RemoveListener(OnUndoClicked);
            restartButton.onClick.RemoveListener(OnRestartClicked);
        }

        private void OnDestroy()
        {
            bannerFade?.Kill();
        }

        private void HandleStateChanged(PathState previous, PathState current)
        {
            bannerTarget = current == PathState.Stuck ? 1f : 0f;
            if (bannerFade != null)
            {
                float duration = Mathf.Max(0.01f, Mathf.Abs(stuckBanner.alpha - bannerTarget) / bannerFadeSpeed);
                bannerFade.ChangeEndValue(bannerTarget, duration, true).Restart();
            }

            Refresh();
        }

        private void Refresh()
        {
            if (levelLabel != null)
            {
                // The player counts levels, not chapter slots, so the label reads the
                // campaign number the save file also holds.
                levelLabel.SetText(
                    textCatalog == null ? "{0}" : textCatalog.Get("gameplay.level"),
                    bootstrap == null ? 0 : bootstrap.CurrentLevelNumber);
            }

            if (progressLabel != null && textCatalog != null)
            {
                if (hintResult != null && hintResult.NeedsRestart)
                {
                    progressLabel.SetText(textCatalog.Get("gameplay.hint.restart"));
                }
                else if (hintResult != null && hintResult.BacktrackCount > 0)
                {
                    progressLabel.SetText(textCatalog.Get("gameplay.hint.undo"), hintResult.BacktrackCount);
                }
                else
                {
                    progressLabel.SetText(textCatalog.Get("gameplay.progress"), controller.Progress, controller.Target);
                }
            }

            // Both are meaningless on an untouched board and on a finished one, and
            // neither may fire while the path is already rewinding.
            bool hasPath = controller.Progress > 1 && controller.State != PathState.Won;
            undoButton.interactable = hasPath && !controller.IsRewinding;
            restartButton.interactable = hasPath && !controller.IsRewinding;

            // A hint works from the very first cell, unlike undo which needs a step to take back.
            hintButton.interactable = controller.Progress > 0
                                      && controller.State != PathState.Won
                                      && !controller.IsRewinding
                                      && !controller.IsHintPending;
        }

        private void HandleHintReady(HintResult result)
        {
            hintResult = result;
            Refresh();
        }

        private void OnHintClicked()
        {
            eventChannel?.RequestHint();
        }

        private void OnUndoClicked()
        {
            eventChannel?.RequestUndo();
        }

        private void OnRestartClicked()
        {
            eventChannel?.RequestRestart();
        }
    }
}
