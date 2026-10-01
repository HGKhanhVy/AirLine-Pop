using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The airport tab's own UI (GDD 9): the big Play button with the next level number,
    /// which breathes gently so it reads as the main action.
    /// </summary>
    public sealed class AirportPanelView : MonoBehaviour
    {
        [SerializeField] private Button playButton;
        [SerializeField] private TMP_Text levelLabel;
        [SerializeField] private RectTransform pulseTarget;

        private int level = 1;
        private Destination destination;

        public event Action OnPlayRequested;

        private void OnEnable()
        {
            playButton.onClick.AddListener(HandlePlay);
            Localization.Service.OnLanguageChanged += Render;
            pulseTarget.localScale = Vector3.one;
            pulseTarget.DOScale(1.05f, 0.8f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
        }

        private void OnDisable()
        {
            playButton.onClick.RemoveListener(HandlePlay);
            Localization.Service.OnLanguageChanged -= Render;
            pulseTarget.DOKill();
        }

        public void SetLevel(int levelNumber, Destination flyingTo)
        {
            level = levelNumber;
            destination = flyingTo;
            Render();
        }

        private void Render()
        {
            levelLabel.text = destination == null
                ? Localization.Format("flight.number", level)
                : Localization.Format("flight.withDestination", level, destination.DisplayName);
        }

        public void SetInteractable(bool isInteractable)
        {
            playButton.interactable = isInteractable;
        }

        private void HandlePlay()
        {
            OnPlayRequested?.Invoke();
        }

#if UNITY_EDITOR
        public void EditorLink(Button linkedPlay, TMP_Text linkedLevel, RectTransform linkedPulse)
        {
            playButton = linkedPlay;
            levelLabel = linkedLevel;
            pulseTarget = linkedPulse;
        }
#endif
    }
}
