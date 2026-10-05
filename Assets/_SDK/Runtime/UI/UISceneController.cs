using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ASTeams.Base.UI
{
    public class SceneName
    {
        public const string Loading = "Loading";
        public const string Home = "Home";
        public const string Gameplay = "Gameplay";
    }

    public class UISceneController : MonoSingleton<UISceneController>
    {
        [Header("Refs")]
        [SerializeField] private CanvasGroup root;   // full screen overlay
        [SerializeField] private Image bg;           // optional (chỉ để set sprite/color), không bắt buộc

        [Header("Fade")]
        [SerializeField] private float fadeInDur = 0.25f;   // overlay alpha 0 -> 1
        [SerializeField] private float fadeOutDur = 0.25f;  // overlay alpha 1 -> 0
        [SerializeField] private Ease fadeEase = Ease.Linear;

        [Header("Stability")]
        [SerializeField] private int waitFramesAfterSceneActive = 2;

        [Tooltip("Shortest time the overlay stays up, so a loading screen on it can be seen.")]
        [SerializeField, Min(0f)] private float minHoldSeconds;

        /// <summary>Raised as the overlay starts to fade in.</summary>
        public event Action TransitionStarted;

        /// <summary>Raised when the new scene is ready, just before the overlay fades out.</summary>
        public event Action SceneReady;

        /// <summary>Raised once the overlay has faded out.</summary>
        public event Action TransitionFinished;

        public string CurrentScene;

        private Coroutine _co;
        private Tween _fadeTween;
        private ThreadPriority _defaultLoadingPriority;

        public override void Init()
        {
            base.Init();
            DontDestroyOnLoad(gameObject);
            _defaultLoadingPriority = Application.backgroundLoadingPriority;

            if (root != null)
            {
                root.alpha = 0f;
                root.blocksRaycasts = false;
                root.interactable = false;
            }
        }

        public void ReloadCurrentScene()
        {
            if (!string.IsNullOrEmpty(CurrentScene))
                ChangeScene(CurrentScene);
        }

        public void ChangeScene(string sceneName)
        {
            if (_co != null) StopCoroutine(_co);
            Application.backgroundLoadingPriority = _defaultLoadingPriority;
            _co = StartCoroutine(CoLoad(sceneName));
            CurrentScene = sceneName;
            //SceneManager.LoadScene(sceneName);
        }

        private IEnumerator CoLoad(string sceneName)
        {
            KillTween();
            float startTime = Time.unscaledTime;
            TransitionStarted?.Invoke();

            // 1) Fade IN overlay
            if (root != null)
            {
                root.blocksRaycasts = true;
                root.interactable = true;

                root.alpha = 0f;
                _fadeTween = root.DOFade(1f, fadeInDur).SetEase(fadeEase).SetUpdate(true);
                yield return _fadeTween.WaitForCompletion();
            }

            // 2) Load async scene. Load gently in the background and keep the scene inactive
            // until the hold is over: activating it stalls the main thread, so it goes last,
            // where the stall cannot interrupt the loading screen's animation midway.
            Application.backgroundLoadingPriority = ThreadPriority.Low;
            var op = SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false;

            while (op.progress < 0.9f || Time.unscaledTime - startTime < minHoldSeconds) yield return null;

            Application.backgroundLoadingPriority = _defaultLoadingPriority;
            op.allowSceneActivation = true;
            while (!op.isDone) yield return null;

            while (SceneManager.GetActiveScene().name != sceneName) yield return null;
            for (int i = 0; i < waitFramesAfterSceneActive; i++) yield return null;
            yield return new WaitForEndOfFrame();

            SceneReady?.Invoke();

            // 3) Fade OUT overlay
            KillTween();
            if (root != null)
            {
                _fadeTween = root.DOFade(0f, fadeOutDur).SetEase(fadeEase).SetUpdate(true);
                yield return _fadeTween.WaitForCompletion();

                root.blocksRaycasts = false;
                root.interactable = false;
            }

            _co = null;
            TransitionFinished?.Invoke();
        }

        private void KillTween()
        {
            _fadeTween?.Kill();
            _fadeTween = null;
        }

        private void OnDisable()
        {
            KillTween();
            if (_co != null) StopCoroutine(_co);
            _co = null;
        }
    }
}