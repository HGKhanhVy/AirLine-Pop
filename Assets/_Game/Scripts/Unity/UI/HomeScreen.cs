using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The screen the game opens on: a title and a way in.
    ///
    /// It is a panel over the loaded board rather than a separate scene, because GDD 15.2
    /// forbids reloading the scene between levels and there is no reason to make the entry
    /// screen the exception. The board is already built underneath, so Play is instant.
    /// </summary>
    public sealed class HomeScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Button playButton;
        [SerializeField] private TMP_Text subtitleLabel;

        [SerializeField, Min(0.01f)] private float fadeDuration = 0.22f;

        private float target = 1f;
        private Tweener fade;

        /// <summary>Raised when the player asks to start.</summary>
        public event Action OnPlayRequested;

        public bool IsShown { get; private set; } = true;

        private void EnsureFade()
        {
            if (fade != null && fade.IsActive())
            {
                return;
            }

            fade = group.DOFade(group.alpha, fadeDuration).SetEase(Ease.Linear)
                .SetUpdate(true).SetAutoKill(false).Pause().OnUpdate(UpdateInteraction);
        }

        private void OnEnable()
        {
            EnsureFade();
            playButton.onClick.AddListener(HandlePlay);
            FadeTo(target);
        }

        private void OnDisable()
        {
            playButton.onClick.RemoveListener(HandlePlay);
            fade?.Pause();
        }

        private void OnDestroy()
        {
            fade?.Kill();
        }

        private void FadeTo(float alpha)
        {
            EnsureFade();
            target = alpha;
            float duration = Mathf.Max(0.01f, Mathf.Abs(group.alpha - target) * fadeDuration);
            fade.ChangeEndValue(target, duration, true).Restart();
            UpdateInteraction();
        }

        private void UpdateInteraction()
        {
            bool isVisible = group.alpha > 0.01f;
            group.blocksRaycasts = isVisible && IsShown;
            group.interactable = isVisible && IsShown;
        }
        public void SetSubtitle(string text)
        {
            if (subtitleLabel != null)
            {
                subtitleLabel.text = text;
            }
        }

        public void Show()
        {
            IsShown = true;
            FadeTo(1f);
        }

        public void Hide()
        {
            IsShown = false;
            FadeTo(0f);
        }

        /// <summary>
        /// Hides without the fade. Used when the player already pressed Play in the home
        /// scene, where fading a panel they never saw would only delay the board.
        /// </summary>
        public void HideImmediately()
        {
            Hide();
            fade.Pause();

            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;
            }
        }

        private void HandlePlay()
        {
            Hide();
            OnPlayRequested?.Invoke();
        }
    }
}
