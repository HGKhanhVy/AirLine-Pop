using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Opens the next level once the board has finished celebrating.
    ///
    /// There is no win screen: a finished board rolls straight into the next one, so the
    /// only thing left to decide is when. It listens to the channel rather than to the
    /// board, so gameplay neither knows nor cares what happens after a win.
    ///
    /// <see cref="LevelBootstrap"/> can advance on its own timer, but that timer is a
    /// fixed number and the celebration is not: a three square level waves for half a
    /// second and a sixty nine square one for over two. So the board's own length wins,
    /// and the bootstrap is left with its automatic advance switched off.
    /// </summary>
    public sealed class WinAdvancePresenter : MonoBehaviour
    {
        [Tooltip("Least time the finished board is left on screen. GDD 10 gives the win " +
                 "wave 0.8 to 1.2 seconds. A level whose wave runs longer than this holds " +
                 "the next one back until the wave is done.")]
        [SerializeField, Min(0f)] private float showDelay = 0.9f;

        [Tooltip("After this much of the celebration a tap moves on at once. GDD 10 asks " +
                 "for the celebration to be skippable after 0.4 seconds.")]
        [SerializeField, Min(0f)] private float skippableAfter = 0.4f;

        private float celebrationSeconds;
        private Coroutine pending;

        private void OnEnable()
        {
            GameplayEvents.OnCelebrationStarted += HandleCelebrationStarted;
            GameplayEvents.OnLevelWon += HandleLevelWon;
        }

        private void OnDisable()
        {
            GameplayEvents.OnCelebrationStarted -= HandleCelebrationStarted;
            GameplayEvents.OnLevelWon -= HandleLevelWon;

            if (pending != null)
            {
                StopCoroutine(pending);
                pending = null;
            }
        }

        /// <summary>
        /// The board says how long its wave will run before the win itself is raised, so
        /// the number is already in hand by the time the wait starts.
        /// </summary>
        private void HandleCelebrationStarted(float seconds)
        {
            celebrationSeconds = seconds;
        }

        private void HandleLevelWon(int levelNumber, string levelId)
        {
            if (pending != null)
            {
                StopCoroutine(pending);
            }

            pending = StartCoroutine(AdvanceAfterCelebration());
        }

        private IEnumerator AdvanceAfterCelebration()
        {
            float waited = 0f;
            float wait = Mathf.Max(showDelay, celebrationSeconds);
            celebrationSeconds = 0f;

            while (waited < wait)
            {
                waited += Time.deltaTime;

                if (waited >= skippableAfter && WasTapped())
                {
                    break;
                }

                yield return null;
            }

            pending = null;
            GameplayEvents.RequestNextLevel();
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
