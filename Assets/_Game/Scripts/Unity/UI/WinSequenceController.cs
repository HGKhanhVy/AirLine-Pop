using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Runs the win beat of GDD 3. The board already plays the runway light wave and the
    /// take-off the moment the level is won, and the reward is already saved by then.
    ///
    /// The reward card then comes in, with the cat that belongs to the moment, see
    /// <see cref="WinMilestone"/>, within <see cref="maxDelay"/> whatever the board is doing.
    /// It has no buttons: a few seconds later, or sooner on a tap, the next flight opens, so
    /// playing on never needs a press. The way home is the pause menu.
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

        [Tooltip("How long the card stays before the next flight opens, unless a tap brings it sooner.")]
        [SerializeField, Min(0.5f)] private float cardSeconds = 3f;

        private CatPortraitStage stage;
        [Tooltip("Optional. The route map, for the destination, stamp and postcard on the win card.")]
        [SerializeField] private DestinationCatalogSO destinations;

        private int pendingCoins;
        private int passengersAboard;
        private CatCatalogSO catalog;
        private Coroutine pending;
        private bool isVipFlight;
        private bool isFirstClear;

        public void Initialize(CatPortraitStage portraitStage, CatCatalogSO cats)
        {
            stage = portraitStage;
            catalog = cats;
            panel.SetPortrait(stage != null && stage.HasCat ? stage.Texture : null);
        }

        private void OnEnable()
        {
            GameplayEvents.OnCoinsAwarded += HandleCoinsAwarded;
            GameplayEvents.OnFirstClear += HandleFirstClear;
            GameplayEvents.OnPassengersChanged += HandlePassengersChanged;
            GameplayEvents.OnLevelWon += HandleLevelWon;
            GameplayEvents.OnLevelLoaded += HandleLevelLoaded;
            GameplayEvents.OnLevelRules += HandleLevelRules;
        }

        private void OnDisable()
        {
            GameplayEvents.OnCoinsAwarded -= HandleCoinsAwarded;
            GameplayEvents.OnFirstClear -= HandleFirstClear;
            GameplayEvents.OnPassengersChanged -= HandlePassengersChanged;
            GameplayEvents.OnLevelWon -= HandleLevelWon;
            GameplayEvents.OnLevelLoaded -= HandleLevelLoaded;
            GameplayEvents.OnLevelRules -= HandleLevelRules;
            StopPending();
        }

        // Coins are paid, and announced, before the win itself is raised.
        private void HandleCoinsAwarded(int amount, long balance)
        {
            pendingCoins = amount;
        }

        private void HandleFirstClear(int levelNumber)
        {
            isFirstClear = true;
        }

        private void HandleLevelRules(ASTeams.SingleLine.Core.LevelRule rules)
        {
            isVipFlight = (rules & ASTeams.SingleLine.Core.LevelRule.Vip) != 0;
        }

        private void HandlePassengersChanged(int aboard, int total)
        {
            passengersAboard = aboard;
        }

        private void HandleLevelWon(int levelNumber, string levelId)
        {
            StopPending();
            pause.SetLocked(true);
            CatBreedSO arrival = isFirstClear ? FindArrival(levelNumber) : null;
            WinFlightSummary flight = Summarise(levelNumber, pendingCoins, arrival);
            pending = StartCoroutine(ShowCard(flight, arrival));
            pendingCoins = 0;
            isFirstClear = false;
        }

        private IEnumerator ShowCard(WinFlightSummary flight, CatBreedSO arrival)
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

            Feature(flight.Milestone, arrival);
            panel.Show(flight);

            // The card stays a moment, then the next flight opens; a tap gets there sooner.
            waited = 0f;

            while (waited < cardSeconds)
            {
                waited += Time.unscaledDeltaTime;

                if (waited >= skippableAfter && WasTapped())
                {
                    break;
                }

                yield return null;
            }

            pending = null;
            CloseCard();
            GameplayEvents.RequestNextLevel();
        }

        /// <summary>The cat the card shows: His Majesty after a VIP flight, the newcomer when one arrives, else the companion.</summary>
        private void Feature(WinMilestone milestone, CatBreedSO arrival)
        {
            if (stage == null)
            {
                return;
            }

            if (milestone == WinMilestone.Vip)
            {
                stage.FeatureGuest();
            }
            else if (milestone == WinMilestone.Arrival)
            {
                stage.FeatureArrival(arrival.Prefab);
            }
            else
            {
                stage.FeatureCompanion();
            }

            stage.SetRendering(true);
            stage.Play(CatAnimatorParams.Celebrate);
        }

        /// <summary>
        /// The regular who joins the lounge once this flight is flown. Asked only on a first
        /// clear: a replay brings nobody new.
        /// </summary>
        private CatBreedSO FindArrival(int flightNumber)
        {
            if (catalog == null)
            {
                return null;
            }

            foreach (CatBreedSO breed in catalog.Breeds)
            {
                if (!breed.IsOwnedByDefault && breed.ArrivesAfterFlight == flightNumber && breed.Prefab != null)
                {
                    return breed;
                }
            }

            return null;
        }

        private WinFlightSummary Summarise(int flightNumber, int coins, CatBreedSO arrival)
        {
            Destination destination = destinations != null ? destinations.ForFlight(flightNumber) : null;
            Sprite postcard = null;
            string place = null;
            int stamp = 0;
            int stampsNeeded = 0;

            if (destination != null)
            {
                ASTeams.SingleLine.Core.DestinationSchedule schedule = destinations.Schedule;
                postcard = schedule.CompletesPostcard(flightNumber) ? destination.Postcard : null;
                place = destination.DisplayName;
                stamp = schedule.StampOf(flightNumber);
                stampsNeeded = schedule.FlightsPerDestination;
            }

            WinMilestone milestone = MilestoneOf(flightNumber, arrival, postcard);
            return new WinFlightSummary(flightNumber, coins, passengersAboard, place, stamp, stampsNeeded, postcard,
                milestone, arrival != null ? arrival.DisplayName : null);
        }

        /// <summary>The biggest reason this flight is worth stopping for, if any.</summary>
        private WinMilestone MilestoneOf(int flightNumber, CatBreedSO arrival, Sprite postcard)
        {
            if (isVipFlight)
            {
                return WinMilestone.Vip;
            }

            if (arrival != null)
            {
                return WinMilestone.Arrival;
            }

            if (postcard != null)
            {
                return WinMilestone.Postcard;
            }

            bool closesChapter = flightNumber % ASTeams.SingleLine.Data.CampaignLevelAddress.LevelsPerChapter == 0 ||
                flightNumber >= ASTeams.SingleLine.Data.CampaignLevelAddress.MaxLevelNumber;
            return closesChapter ? WinMilestone.ChapterEnd : WinMilestone.None;
        }

        private void HandleLevelLoaded(int levelNumber, string levelId, int difficulty, int totalCells)
        {
            StopPending();
            CloseCard();
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
