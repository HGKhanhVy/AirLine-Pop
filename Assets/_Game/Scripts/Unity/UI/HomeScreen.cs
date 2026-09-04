using System;
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
        private bool interactableWhenShown = true;

        /// <summary>Raised when the player asks to start.</summary>
        public event Action OnPlayRequested;

        public bool IsShown { get; private set; } = true;

        private void OnEnable()
        {
            playButton.onClick.AddListener(HandlePlay);
        }

        private void OnDisable()
        {
            playButton.onClick.RemoveListener(HandlePlay);
        }

        private void Update()
        {
            if (group == null)
            {
                return;
            }

            float speed = 1f / Mathf.Max(0.01f, fadeDuration);
            group.alpha = Mathf.MoveTowards(group.alpha, target, speed * Time.unscaledDeltaTime);

            // Blocking raycasts only while visible keeps a faded panel from swallowing
            // drags meant for the board.
            bool visible = group.alpha > 0.01f;
            group.blocksRaycasts = visible && interactableWhenShown;
            group.interactable = visible && interactableWhenShown;
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
            target = 1f;
            interactableWhenShown = true;
        }

        public void Hide()
        {
            IsShown = false;
            target = 0f;
            interactableWhenShown = false;
        }

        private void HandlePlay()
        {
            Hide();
            OnPlayRequested?.Invoke();
        }
    }
}
