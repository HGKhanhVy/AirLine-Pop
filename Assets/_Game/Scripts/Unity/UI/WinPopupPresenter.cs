using System.Collections;
using ASTeams.Base.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Opens the win screen once the board has finished celebrating.
    ///
    /// It listens to the channel rather than to the board, so gameplay neither knows nor
    /// cares that a screen exists. The wait is what GDD 10 asks for: the wave and the
    /// confetti on the board get their second before a panel covers them.
    /// </summary>
    public sealed class WinPopupPresenter : MonoBehaviour
    {
        [Tooltip("How long the board keeps the celebration before the screen opens. " +
                 "GDD 10 gives the win wave 0.8 to 1.2 seconds.")]
        [SerializeField, Min(0f)] private float showDelay = 0.9f;

        [Tooltip("After this much of the celebration a tap opens the screen at once. " +
                 "GDD 10 asks for the celebration to be skippable after 0.4 seconds.")]
        [SerializeField, Min(0f)] private float skippableAfter = 0.4f;

        private int lastReward;
        private long lastBalance;
        private Coroutine pending;

        private void OnEnable()
        {
            GameplayEvents.OnCoinsAwarded += HandleCoinsAwarded;
            GameplayEvents.OnLevelWon += HandleLevelWon;
        }

        private void OnDisable()
        {
            GameplayEvents.OnCoinsAwarded -= HandleCoinsAwarded;
            GameplayEvents.OnLevelWon -= HandleLevelWon;

            if (pending != null)
            {
                StopCoroutine(pending);
                pending = null;
            }
        }

        /// <summary>
        /// The coins are raised before the win, so the amount is already known by the time
        /// the screen is asked for.
        /// </summary>
        private void HandleCoinsAwarded(int amount, long balance)
        {
            lastReward = amount;
            lastBalance = balance;
        }

        private void HandleLevelWon(int levelNumber, string levelId)
        {
            if (pending != null)
            {
                StopCoroutine(pending);
            }

            pending = StartCoroutine(ShowAfterCelebration(levelNumber));
        }

        private IEnumerator ShowAfterCelebration(int levelNumber)
        {
            float waited = 0f;

            while (waited < showDelay)
            {
                waited += Time.deltaTime;

                if (waited >= skippableAfter && WasTapped())
                {
                    break;
                }

                yield return null;
            }

            pending = null;

            if (UIPopupController.Instance == null)
            {
                Debug.LogWarning("No UIPopupController in the scene, the win screen cannot open.", this);
                yield break;
            }

            SingleLineWinPopup popup = UIPopupController.Instance.GetActivePopup<SingleLineWinPopup>();

            if (popup == null)
            {
                Debug.LogWarning("SingleLineWinPopup is not in the popup config.", this);
                yield break;
            }

            popup.Present(levelNumber, lastReward, lastBalance);
            lastReward = 0;
        }

        /// <summary>
        /// A press anywhere, mouse or finger. The board stops taking input on a win, so
        /// nothing else is listening for this one.
        /// </summary>
        private static bool WasTapped()
        {
            Pointer pointer = Pointer.current;
            return pointer != null && pointer.press.wasPressedThisFrame;
        }
    }
}
