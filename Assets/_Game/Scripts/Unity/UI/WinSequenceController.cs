using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Runs the win beat of GDD 3. The board already plays the runway light wave and the
    /// take-off the moment the level is won, and the reward is already saved by then; this
    /// waits for that show, then brings in the reward card with the companion cat.
    ///
    /// A tap after the short guard skips straight to the card, and the card appears within
    /// <see cref="maxDelay"/> whatever the board is doing, so a stalled animation never
    /// hides the reward.
    /// </summary>
    public sealed class WinSequenceController : MonoBehaviour
    {
        [SerializeField] private WinPanelView panel;
        [SerializeField] private PauseController pause;

        [Tooltip("GDD 3: the reward shows from about 2 seconds after the win.")]
        [SerializeField, Min(0f)] private float panelDelay = 1.8f;

        [Tooltip("GDD 3: the reward card opens by this time even if an animation fails.")]
        [SerializeField, Min(0.5f)] private float maxDelay = 4f;

        [Tooltip("Taps before this are ignored, so the finishing drag cannot skip the show.")]
        [SerializeField, Min(0f)] private float skippableAfter = 0.4f;

        private CatPortraitStage stage;
        private ISceneNavigator navigator;
        [Tooltip("Optional. The route map, for the destination, stamp and postcard on the win card.")]
        [SerializeField] private DestinationCatalogSO destinations;

        private int pendingCoins;
        private int passengersAboard;
        private Coroutine pending;
        private bool isLeaving;

        public void Initialize(CatPortraitStage portraitStage, ISceneNavigator sceneNavigator)
        {
            stage = portraitStage;
            navigator = sceneNavigator;
            panel.SetPortrait(stage != null && stage.HasCat ? stage.Texture : null);
        }

        private void OnEnable()
        {
            GameplayEvents.OnCoinsAwarded += HandleCoinsAwarded;
            GameplayEvents.OnPassengersChanged += HandlePassengersChanged;
            GameplayEvents.OnLevelWon += HandleLevelWon;
            GameplayEvents.OnLevelLoaded += HandleLevelLoaded;
            panel.OnContinue += HandleContinue;
            panel.OnHome += HandleHome;
        }

        private void OnDisable()
        {
            GameplayEvents.OnCoinsAwarded -= HandleCoinsAwarded;
            GameplayEvents.OnPassengersChanged -= HandlePassengersChanged;
            GameplayEvents.OnLevelWon -= HandleLevelWon;
            GameplayEvents.OnLevelLoaded -= HandleLevelLoaded;
            panel.OnContinue -= HandleContinue;
            panel.OnHome -= HandleHome;
            StopPending();
        }

        // Coins are paid, and announced, before the win itself is raised.
        private void HandleCoinsAwarded(int amount, long balance)
        {
            pendingCoins = amount;
        }

        private void HandlePassengersChanged(int aboard, int total)
        {
            passengersAboard = aboard;
        }

        private void HandleLevelWon(int levelNumber, string levelId)
        {
            StopPending();
            pause.SetLocked(true);
            pending = StartCoroutine(ShowAfterCelebration(levelNumber, pendingCoins, passengersAboard));
            pendingCoins = 0;
        }

        private IEnumerator ShowAfterCelebration(int levelNumber, int coins, int passengers)
        {
            float waited = 0f;
            float wait = Mathf.Min(panelDelay, maxDelay);

            while (waited < wait)
            {
                waited += Time.unscaledDeltaTime;

                if (waited >= skippableAfter && WasTapped())
                {
                    break;
                }

                yield return null;
            }

            pending = null;

            if (stage != null)
            {
                stage.SetRendering(true);
                stage.Play(CatAnimatorParams.Celebrate);
            }

            panel.Show(Summarise(levelNumber, coins, passengers));
        }

        private WinFlightSummary Summarise(int flightNumber, int coins, int passengers)
        {
            Destination destination = destinations != null ? destinations.ForFlight(flightNumber) : null;

            if (destination == null)
            {
                return new WinFlightSummary(flightNumber, coins, passengers, null, 0, 0, null);
            }

            ASTeams.SingleLine.Core.DestinationSchedule schedule = destinations.Schedule;
            Sprite postcard = schedule.CompletesPostcard(flightNumber) ? destination.Postcard : null;

            return new WinFlightSummary(flightNumber, coins, passengers, destination.DisplayName,
                schedule.StampOf(flightNumber), schedule.FlightsPerDestination, postcard);
        }

        private void HandleLevelLoaded(int levelNumber, string levelId, int difficulty, int totalCells)
        {
            StopPending();
            CloseCard();
        }

        private void HandleContinue()
        {
            CloseCard();
            GameplayEvents.RequestNextLevel();
        }

        private void HandleHome()
        {
            if (isLeaving)
            {
                return;
            }

            isLeaving = true;
            navigator.GoHome();
        }

        private void CloseCard()
        {
            if (panel.isActiveAndEnabled)
            {
                panel.Hide();
            }

            if (stage != null)
            {
                stage.SetRendering(false);
            }

            pause.SetLocked(false);
        }

        private void StopPending()
        {
            if (pending == null)
            {
                return;
            }

            StopCoroutine(pending);
            pending = null;
        }

        private static bool WasTapped()
        {
            Pointer pointer = Pointer.current;
            return pointer != null && pointer.press.wasPressedThisFrame;
        }

#if UNITY_EDITOR
        public void EditorLink(WinPanelView linkedPanel, PauseController linkedPause)
        {
            panel = linkedPanel;
            pause = linkedPause;
        }
#endif
    }
}
