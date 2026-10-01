using ASTeams.SingleLine.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The gameplay screen furniture: which level this is, how much of it is covered, what
    /// the player owns, and the three things they reach for when a path goes wrong.
    ///
    /// It holds no reference into gameplay at all. Everything it shows arrives on the
    /// event channel and everything it does leaves on the same asset as a request, which
    /// is the contract the UI side is built against. That also means this file is the
    /// worked example: a screen that needs more than this needs another event, not a
    /// reference to the board.
    ///
    /// Because a screen can be enabled long after the level opened, it asks the board to
    /// repeat itself on enable rather than assuming it heard the opening events.
    /// </summary>
    public sealed class GameplayHud : MonoBehaviour
    {
        [Header("Readouts")]
        [SerializeField] private TMP_Text levelLabel;
        [SerializeField] private TMP_Text progressLabel;
        [SerializeField] private TMP_Text coinLabel;

        [Header("Buttons")]
        [SerializeField] private Button hintButton;
        [SerializeField] private Button undoButton;
        [SerializeField] private Button restartButton;

        [Header("Stuck")]
        [SerializeField] private CanvasGroup stuckBanner;
        [SerializeField, Min(0.01f)] private float bannerFadeSpeed = 4f;

        private int levelNumber;
        private int visitedCells;
        private int totalCells;
        private PathState state = PathState.Ready;
        private bool isRewinding;
        private bool isHintPending;
        private HintResult hintResult;
        private float bannerAlpha;
        private float bannerTarget;

        private void OnEnable()
        {
            GameplayEvents.OnLevelLoaded += HandleLevelLoaded;
            GameplayEvents.OnProgressChanged += HandleProgressChanged;
            GameplayEvents.OnStateChanged += HandleStateChanged;
            GameplayEvents.OnHintStarted += HandleHintStarted;
            GameplayEvents.OnHintResolved += HandleHintResolved;
            GameplayEvents.OnRewindChanged += HandleRewindChanged;
            GameplayEvents.OnCoinBalanceChanged += HandleCoinBalanceChanged;

            AddListener(hintButton, OnHintClicked);
            AddListener(undoButton, OnUndoClicked);
            AddListener(restartButton, OnRestartClicked);
            Localization.Service.OnLanguageChanged += Refresh;

            // Catches up with a board that opened before this screen did.
            GameplayEvents.RequestSnapshot();
            Refresh();
        }

        private void OnDisable()
        {
            GameplayEvents.OnLevelLoaded -= HandleLevelLoaded;
            GameplayEvents.OnProgressChanged -= HandleProgressChanged;
            GameplayEvents.OnStateChanged -= HandleStateChanged;
            GameplayEvents.OnHintStarted -= HandleHintStarted;
            GameplayEvents.OnHintResolved -= HandleHintResolved;
            GameplayEvents.OnRewindChanged -= HandleRewindChanged;
            GameplayEvents.OnCoinBalanceChanged -= HandleCoinBalanceChanged;

            RemoveListener(hintButton, OnHintClicked);
            RemoveListener(undoButton, OnUndoClicked);
            RemoveListener(restartButton, OnRestartClicked);
            Localization.Service.OnLanguageChanged -= Refresh;
        }

        private void Update()
        {
            if (stuckBanner == null || Mathf.Approximately(bannerAlpha, bannerTarget))
            {
                return;
            }

            bannerAlpha = Mathf.MoveTowards(bannerAlpha, bannerTarget, bannerFadeSpeed * Time.deltaTime);
            stuckBanner.alpha = bannerAlpha;
        }

        private void HandleLevelLoaded(int number, string levelId, int difficulty, int cells)
        {
            levelNumber = number;
            totalCells = cells;
            visitedCells = 0;
            hintResult = null;
            isHintPending = false;
            Refresh();
        }

        private void HandleProgressChanged(int visited, int total)
        {
            visitedCells = visited;
            totalCells = total;
            Refresh();
        }

        private void HandleStateChanged(PathState previous, PathState current)
        {
            state = current;
            bannerTarget = current == PathState.Stuck ? 1f : 0f;
            Refresh();
        }

        private void HandleHintStarted()
        {
            isHintPending = true;
            Refresh();
        }

        private void HandleHintResolved(HintResult result)
        {
            isHintPending = false;
            hintResult = result;
            Refresh();
        }

        private void HandleRewindChanged(bool rewinding)
        {
            isRewinding = rewinding;
            Refresh();
        }

        private void HandleCoinBalanceChanged(long balance)
        {
            if (coinLabel != null)
            {
                coinLabel.SetText("{0}", balance);
            }
        }

        private void Refresh()
        {
            if (levelLabel != null)
            {
                levelLabel.SetText(Localization.Get("gameplay.level"), levelNumber);
            }

            if (progressLabel != null)
            {
                if (hintResult != null && hintResult.NeedsRestart)
                {
                    progressLabel.SetText(Localization.Get("gameplay.hint.restart"));
                }
                else if (hintResult != null && hintResult.BacktrackCount > 0)
                {
                    progressLabel.SetText(Localization.Get("gameplay.hint.undo"), hintResult.BacktrackCount);
                }
                else
                {
                    // The reference layout carries no cell count; the line only speaks
                    // when a hint has something to say.
                    progressLabel.SetText(string.Empty);
                }
            }

            // Both are meaningless on an untouched board and on a finished one, and neither
            // may fire while the path is already rewinding.
            bool hasPath = visitedCells > 1 && state != PathState.Won;
            SetInteractable(undoButton, hasPath && !isRewinding);
            SetInteractable(restartButton, hasPath && !isRewinding);

            // A hint works from the very first cell, unlike undo which needs a step to take back.
            SetInteractable(hintButton, visitedCells > 0 && state != PathState.Won && !isRewinding && !isHintPending);
        }

        private void OnHintClicked()
        {
            GameplayEvents.RequestHint();
        }

        private void OnUndoClicked()
        {
            GameplayEvents.RequestUndo();
        }

        private void OnRestartClicked()
        {
            GameplayEvents.RequestRestart();
        }

        private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }

        private static void RemoveListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(action);
            }
        }

        private static void SetInteractable(Button button, bool value)
        {
            if (button != null)
            {
                button.interactable = value;
            }
        }
    }
}
