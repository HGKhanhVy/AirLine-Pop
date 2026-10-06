using System;
using ASTeams.SingleLine.Core;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// What the board tells the rest of the game, and what the rest of the game may ask
    /// the board for.
    ///
    /// Both directions run through this one class so a screen can be built against it
    /// without holding a reference to any gameplay object, and without an asset to drag
    /// into an Inspector field: subscribing is a line of code and nothing to wire.
    ///
    /// The Raise methods belong to gameplay and the Request methods to whoever is driving
    /// it; nothing here decides what a popup or a label does with either.
    ///
    /// Static events outlive a play session when the Editor is set to enter play mode
    /// without reloading the domain, so <see cref="Clear"/> runs before every run and
    /// drops whatever the last one left subscribed.
    /// </summary>
    public static class GameplayEvents
    {
        public static event Action<int, string, int, int> OnLevelLoaded;
        public static event Action<int, int> OnProgressChanged;

        /// <summary>
        /// Cats aboard and cats on this level, raised when a level opens and whenever a cat
        /// boards or, after an undo, hops back to its square.
        /// </summary>
        public static event Action<int, int> OnPassengersChanged;
        public static event Action<PathState, PathState> OnStateChanged;
        public static event Action<int> OnInvalidMove;

        /// <summary>Raised when a hint starts being searched for, so a button can show it is busy.</summary>
        public static event Action OnHintStarted;

        public static event Action<HintResult> OnHintResolved;
        public static event Action<int, string> OnLevelWon;

        /// <summary>
        /// The level just won had never been finished before. Raised just before
        /// <see cref="OnLevelWon"/>; a replay pays a few coins too, so coins alone cannot tell.
        /// </summary>
        public static event Action<int> OnFirstClear;

        /// <summary>
        /// How long the board is going to celebrate for, raised the moment the wave along
        /// the path starts. The wave's length depends on how many squares the level has,
        /// so a screen that waits for it has to be told rather than assume a number.
        /// </summary>
        public static event Action<float> OnCelebrationStarted;

        /// <summary>
        /// Coins paid for finishing a level, with the balance they landed in. Already
        /// saved by the time this fires, so a screen may animate at its own pace.
        /// </summary>
        public static event Action<int, long> OnCoinsAwarded;

        /// <summary>
        /// What the player owns now. Raised when a level opens and again after every
        /// award, so a coin label never has to read the save file to know where to start.
        /// </summary>
        public static event Action<long> OnCoinBalanceChanged;

        /// <summary>
        /// True while the board is rewinding itself after Restart. Undo and Restart have
        /// to be dead for that stretch, and a screen with no reference into gameplay has
        /// no other way to know.
        /// </summary>
        public static event Action<bool> OnRewindChanged;

        /// <summary>
        /// Share of the screen height the HUD covers at the top and at the bottom, safe
        /// area included. The board camera keeps the board clear of both.
        /// </summary>
        public static event Action<float, float> OnHudInsetsChanged;

        /// <summary>Asks the board to take back one step.</summary>
        public static event Action OnUndoRequested;

        /// <summary>Asks the board to rewind to the start square.</summary>
        public static event Action OnRestartRequested;

        /// <summary>Asks the board for a hint.</summary>
        public static event Action OnHintRequested;

        /// <summary>
        /// Asks the board to open the next level. A win screen raises this from its
        /// Continue button when the board is set not to advance on its own.
        /// </summary>
        public static event Action OnNextLevelRequested;

        /// <summary>
        /// Asks the board to pass over the current level without finishing it, once the
        /// player has paid for that (a rewarded ad). Unlike the next-level request this
        /// moves the save on and is not a win: no coins, no win break.
        /// </summary>
        public static event Action OnSkipLevelRequested;

        /// <summary>
        /// Asks the board to say again where it stands. A screen that opens after the
        /// level did missed every event that described it; rather than keep a copy of the
        /// state here, which would make this a save file, the board repeats itself.
        /// </summary>
        public static event Action OnSnapshotRequested;

        public static void RaiseLevelLoaded(int levelNumber, string levelId, int difficulty, int totalCells)
        {
            OnLevelLoaded?.Invoke(levelNumber, levelId, difficulty, totalCells);
        }

        /// <summary>
        /// Raised right after every level loads: the picture a special board shows, such as
        /// "plane", or null for an ordinary board, so a screen can tell the two apart.
        /// </summary>
        public static event Action<string> OnSpecialFlight;

        public static void RaiseSpecialFlight(string pictureName)
        {
            OnSpecialFlight?.Invoke(pictureName);
        }

        /// <summary>Raised right after every level loads: the extra conditions it sets, if any.</summary>
        public static event Action<LevelRule> OnLevelRules;

        public static void RaiseLevelRules(LevelRule rules)
        {
            OnLevelRules?.Invoke(rules);
        }

        /// <summary>
        /// Raised with true when a card explaining a new rule opens over the board, and with
        /// false once the last one has closed, so the board can hold anything the player
        /// should see until the card is out of the way.
        /// </summary>
        public static event Action<bool> OnRuleIntroChanged;

        public static void RaiseRuleIntroChanged(bool isShown)
        {
            OnRuleIntroChanged?.Invoke(isShown);
        }

        public static void RaiseProgressChanged(int visitedCells, int totalCells)
        {
            OnProgressChanged?.Invoke(visitedCells, totalCells);
        }

        public static void RaisePassengersChanged(int boarded, int total)
        {
            OnPassengersChanged?.Invoke(boarded, total);
        }

        public static void RaiseStateChanged(PathState previous, PathState current)
        {
            OnStateChanged?.Invoke(previous, current);
        }

        public static void RaiseInvalidMove(int cellIndex)
        {
            OnInvalidMove?.Invoke(cellIndex);
        }

        public static void RaiseHintStarted()
        {
            OnHintStarted?.Invoke();
        }

        public static void RaiseHintResolved(HintResult result)
        {
            OnHintResolved?.Invoke(result);
        }

        public static void RaiseCelebrationStarted(float seconds)
        {
            OnCelebrationStarted?.Invoke(seconds);
        }

        public static void RaiseLevelWon(int levelNumber, string levelId)
        {
            OnLevelWon?.Invoke(levelNumber, levelId);
        }

        public static void RaiseFirstClear(int levelNumber)
        {
            OnFirstClear?.Invoke(levelNumber);
        }

        public static void RaiseCoinsAwarded(int amount, long balance)
        {
            OnCoinsAwarded?.Invoke(amount, balance);
        }

        public static void RaiseCoinBalanceChanged(long balance)
        {
            OnCoinBalanceChanged?.Invoke(balance);
        }

        public static void RaiseRewindChanged(bool isRewinding)
        {
            OnRewindChanged?.Invoke(isRewinding);
        }

        public static void RaiseHudInsetsChanged(float topShare, float bottomShare)
        {
            OnHudInsetsChanged?.Invoke(topShare, bottomShare);
        }

        public static void RequestUndo()
        {
            OnUndoRequested?.Invoke();
        }

        public static void RequestRestart()
        {
            OnRestartRequested?.Invoke();
        }

        public static void RequestHint()
        {
            OnHintRequested?.Invoke();
        }

        public static void RequestNextLevel()
        {
            OnNextLevelRequested?.Invoke();
        }

        public static void RequestSkipLevel()
        {
            OnSkipLevelRequested?.Invoke();
        }

        public static void RequestSnapshot()
        {
            OnSnapshotRequested?.Invoke();
        }

        /// <summary>
        /// Drops every subscriber. Called automatically before a run starts; call it by
        /// hand only in a test that wants a clean slate.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear()
        {
            OnLevelLoaded = null;
            OnSpecialFlight = null;
            OnLevelRules = null;
            OnProgressChanged = null;
            OnPassengersChanged = null;
            OnStateChanged = null;
            OnInvalidMove = null;
            OnHintStarted = null;
            OnHintResolved = null;
            OnLevelWon = null;
            OnFirstClear = null;
            OnCelebrationStarted = null;
            OnCoinsAwarded = null;
            OnCoinBalanceChanged = null;
            OnRewindChanged = null;
            OnHudInsetsChanged = null;
            OnUndoRequested = null;
            OnRestartRequested = null;
            OnHintRequested = null;
            OnNextLevelRequested = null;
            OnSkipLevelRequested = null;
            OnSnapshotRequested = null;
        }
    }
}
