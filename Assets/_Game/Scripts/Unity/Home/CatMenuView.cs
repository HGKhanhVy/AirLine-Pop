using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The card that slides up when a regular is tapped: name, loyalty card, progress to
    /// the next card, and Greet / Play / Snack. Display only; the presenter decides what
    /// the buttons do.
    /// </summary>
    public sealed class CatMenuView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform card;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text tierLabel;
        [SerializeField] private ProgressBarView tierBar;
        [SerializeField] private TMP_Text feedLabel;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private Button petButton;
        [SerializeField] private Button playButton;
        [SerializeField] private Button feedButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button backdropButton;

        [SerializeField, Min(0.05f)] private float slideDuration = 0.25f;
        [SerializeField] private float slideDistance = 360f;

        private Vector2 restPosition;
        private Sequence motion;

        public event Action OnPet;
        public event Action OnPlay;
        public event Action OnFeed;
        public event Action OnClose;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            restPosition = card.anchoredPosition;
            HideInstant();
        }

        private void OnEnable()
        {
            petButton.onClick.AddListener(HandlePet);
            playButton.onClick.AddListener(HandlePlay);
            feedButton.onClick.AddListener(HandleFeed);
            closeButton.onClick.AddListener(HandleClose);
            backdropButton.onClick.AddListener(HandleClose);
        }

        private void OnDisable()
        {
            petButton.onClick.RemoveListener(HandlePet);
            playButton.onClick.RemoveListener(HandlePlay);
            feedButton.onClick.RemoveListener(HandleFeed);
            closeButton.onClick.RemoveListener(HandleClose);
            backdropButton.onClick.RemoveListener(HandleClose);
        }

        private void OnDestroy()
        {
            motion?.Kill();
        }

        public void Show(in CatMenuModel model)
        {
            nameLabel.text = model.Name;
            tierLabel.text = model.Tier;
            tierBar.SetProgress(model.TierProgress);
            feedLabel.text = model.FeedLabel;
            statusLabel.text = model.Status;

            if (IsOpen)
            {
                return;
            }

            IsOpen = true;
            group.blocksRaycasts = true;
            group.interactable = true;
            motion?.Kill();
            card.anchoredPosition = restPosition - new Vector2(0f, slideDistance);
            motion = DOTween.Sequence()
                .Join(group.DOFade(1f, slideDuration * 0.6f))
                .Join(card.DOAnchorPos(restPosition, slideDuration).SetEase(Ease.OutBack))
                .SetUpdate(true);
        }

        public void Hide()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            group.blocksRaycasts = false;
            group.interactable = false;
            motion?.Kill();
            motion = DOTween.Sequence()
                .Join(group.DOFade(0f, slideDuration * 0.6f))
                .Join(card.DOAnchorPos(restPosition - new Vector2(0f, slideDistance), slideDuration).SetEase(Ease.InQuad))
                .SetUpdate(true);
        }

        private void HideInstant()
        {
            IsOpen = false;
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        private void HandlePet()
        {
            OnPet?.Invoke();
        }

        private void HandlePlay()
        {
            OnPlay?.Invoke();
        }

        private void HandleFeed()
        {
            OnFeed?.Invoke();
        }

        private void HandleClose()
        {
            OnClose?.Invoke();
        }

#if UNITY_EDITOR
        public void EditorLink(CanvasGroup linkedGroup, RectTransform linkedCard, TMP_Text linkedName, TMP_Text linkedTier,
            ProgressBarView linkedBar, TMP_Text linkedFeed, TMP_Text linkedStatus, Button linkedPet, Button linkedPlay,
            Button linkedFeedButton, Button linkedClose, Button linkedBackdrop)
        {
            group = linkedGroup;
            card = linkedCard;
            nameLabel = linkedName;
            tierLabel = linkedTier;
            tierBar = linkedBar;
            feedLabel = linkedFeed;
            statusLabel = linkedStatus;
            petButton = linkedPet;
            playButton = linkedPlay;
            feedButton = linkedFeedButton;
            closeButton = linkedClose;
            backdropButton = linkedBackdrop;
        }
#endif
    }
}
