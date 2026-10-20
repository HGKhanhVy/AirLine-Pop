using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AirlinePop.SingleLine.Unity
{
    /// <summary>
    /// Changes scene behind a full-screen cover: fades the cover in, loads the next scene
    /// in the background, activates it only once the cover has been up long enough to be
    /// read, then fades the cover out. The player never sees a frozen or blank frame.
    /// </summary>
    public sealed class SceneTransition : MonoBehaviour
    {
        [Tooltip("The full-screen cover, with the loading screen on it.")]
        [SerializeField] private CanvasGroup root;

        [SerializeField, Min(0f)] private float fadeInDur = 0.25f;
        [SerializeField, Min(0f)] private float fadeOutDur = 0.25f;

        [Tooltip("Frames to let the new scene settle before the cover lifts.")]
        [SerializeField, Min(0)] private int waitFramesAfterSceneActive = 2;

        [Tooltip("Shortest time the cover stays up, so the loading screen on it can be seen.")]
        [SerializeField, Min(0f)] private float minHoldSeconds = 1f;

        private Coroutine running;
        private Tween fade;

        public static SceneTransition Instance { get; private set; }

        /// <summary>Raised as the cover starts to fade in.</summary>
        public event Action TransitionStarted;

        /// <summary>Raised when the new scene is ready, just before the cover fades out.</summary>
        public event Action SceneReady;

        /// <summary>Raised once the cover has faded out.</summary>
        public event Action TransitionFinished;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                enabled = false;
                return;
            }

            Instance = this;
            SetCover(0f, false);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void ChangeScene(string sceneName)
        {
            if (running != null)
            {
                StopCoroutine(running);
            }

            running = StartCoroutine(LoadRoutine(sceneName));
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            float startedAt = Time.unscaledTime;
            ThreadPriority normalPriority = Application.backgroundLoadingPriority;
            TransitionStarted?.Invoke();

            SetCover(0f, true);
            yield return Fade(1f, fadeInDur);

            // Activation stalls the main thread, so it is held back until the hold is
            // over: the stall then falls at the end instead of freezing the animation.
            Application.backgroundLoadingPriority = ThreadPriority.Low;
            AsyncOperation loading = SceneManager.LoadSceneAsync(sceneName);
            loading.allowSceneActivation = false;

            while (loading.progress < 0.9f || Time.unscaledTime - startedAt < minHoldSeconds)
            {
                yield return null;
            }

            Application.backgroundLoadingPriority = normalPriority;
            loading.allowSceneActivation = true;

            while (!loading.isDone)
            {
                yield return null;
            }

            for (int i = 0; i < waitFramesAfterSceneActive; i++)
            {
                yield return null;
            }

            SceneReady?.Invoke();
            yield return Fade(0f, fadeOutDur);
            SetCover(0f, false);

            running = null;
            TransitionFinished?.Invoke();
        }

        private IEnumerator Fade(float alpha, float duration)
        {
            fade?.Kill();

            if (root == null)
            {
                yield break;
            }

            fade = root.DOFade(alpha, duration).SetUpdate(true);
            yield return fade.WaitForCompletion();
        }

        private void SetCover(float alpha, bool blocksInput)
        {
            if (root == null)
            {
                return;
            }

            root.alpha = alpha;
            root.blocksRaycasts = blocksInput;
            root.interactable = blocksInput;
        }

        private void OnDisable()
        {
            fade?.Kill();
            fade = null;

            if (running != null)
            {
                StopCoroutine(running);
                running = null;
            }
        }
    }
}
