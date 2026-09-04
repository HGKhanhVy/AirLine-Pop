using ASTeams.SingleLine.Core;
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

        [Header("Readouts")]
        [SerializeField] private TMP_Text levelLabel;
        [SerializeField] private TMP_Text progressLabel;

        [Header("Buttons")]
        [SerializeField] private Button undoButton;
        [SerializeField] private Button restartButton;

        [Header("Stuck")]
        [SerializeField] private CanvasGroup stuckBanner;
        [SerializeField, Min(0.01f)] private float bannerFadeSpeed = 4f;

        private float bannerTarget;

        private void OnEnable()
        {
            controller.OnStateChanged += HandleStateChanged;
            controller.OnPathChanged += Refresh;

            undoButton.onClick.AddListener(OnUndoClicked);
            restartButton.onClick.AddListener(OnRestartClicked);

            Refresh();
        }

        private void OnDisable()
        {
            controller.OnStateChanged -= HandleStateChanged;
            controller.OnPathChanged -= Refresh;

            undoButton.onClick.RemoveListener(OnUndoClicked);
            restartButton.onClick.RemoveListener(OnRestartClicked);
        }

        private void Update()
        {
            if (stuckBanner == null)
            {
                return;
            }

            // A plain fade rather than a tween: the banner appears and disappears often
            // enough that it should never queue up behind an animation.
            stuckBanner.alpha = Mathf.MoveTowards(
                stuckBanner.alpha, bannerTarget, bannerFadeSpeed * Time.unscaledDeltaTime);
        }

        private void HandleStateChanged(PathState previous, PathState current)
        {
            bannerTarget = current == PathState.Stuck ? 1f : 0f;
            Refresh();
        }

        private void Refresh()
        {
            if (levelLabel != null)
            {
                levelLabel.text = bootstrap == null ? string.Empty : bootstrap.CurrentLevelId;
            }

            if (progressLabel != null)
            {
                progressLabel.text = controller.Progress + " / " + controller.Target;
            }

            // Rewinding is meaningless on an untouched board and on a finished one, and
            // must not be re-triggered while it is already running.
            bool hasPath = controller.Progress > 1 && controller.State != PathState.Won;
            undoButton.interactable = hasPath && !controller.IsRewinding;
            restartButton.interactable = hasPath;
        }

        private void OnUndoClicked()
        {
            bootstrap.Undo();
        }

        private void OnRestartClicked()
        {
            bootstrap.Restart();
        }
    }
}
