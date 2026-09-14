using ASTeams.Base.Ads;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Decides when a level break is allowed to carry an interstitial, per GDD 12.2.
    ///
    /// It asks on the next level request, which is the one moment the document allows:
    /// after the win screen and before the next board, never mid gesture. Everything it
    /// decides here narrows what the ads SDK would already allow; the SDK keeps its own
    /// remote configured level gate and cooldown, and this never bypasses them.
    ///
    /// With the ADS define off the call is compiled away inside the SDK, so this costs a
    /// branch and nothing else until ads are switched on.
    /// </summary>
    public sealed class WinInterstitialPolicy : MonoBehaviour
    {
        [Tooltip("No ad while the player is still this early in the campaign. GDD 12.2 asks for five.")]
        [SerializeField, Min(0)] private int quietLevels = 5;

        [Tooltip("No ad while the session is younger than this. GDD 12.2 asks for ten minutes, " +
                 "and says to take whichever of the two quiet rules lasts longer.")]
        [SerializeField, Min(0f)] private float quietSeconds = 600f;

        [Tooltip("How many finished levels between breaks. GDD 12.2 defaults to three.")]
        [SerializeField, Min(1)] private int levelsPerBreak = 3;

        private int lastCompletedLevel;

        private void OnEnable()
        {
            GameplayEvents.OnLevelWon += HandleLevelWon;
            GameplayEvents.OnNextLevelRequested += HandleNextLevelRequested;
        }

        private void OnDisable()
        {
            GameplayEvents.OnLevelWon -= HandleLevelWon;
            GameplayEvents.OnNextLevelRequested -= HandleNextLevelRequested;
        }

        private void HandleLevelWon(int levelNumber, string levelId)
        {
            lastCompletedLevel = levelNumber;
        }

        private void HandleNextLevelRequested()
        {
            if (!IsBreakAllowed())
            {
                return;
            }

            AdsController.Instance?.ShowInterstitialAds("win");
        }

        private bool IsBreakAllowed()
        {
            if (lastCompletedLevel <= quietLevels)
            {
                return false;
            }

            if (Time.realtimeSinceStartup < quietSeconds)
            {
                return false;
            }

            return lastCompletedLevel % levelsPerBreak == 0;
        }
    }
}
