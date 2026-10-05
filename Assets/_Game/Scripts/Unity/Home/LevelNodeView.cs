using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// One level on the route map, drawn as an airport gate sign with the level's number:
    /// flown gates are navy with a tick, the current gate is coral and breathes, the rest
    /// are grey. Pooled: the map hands the same view a new level as the player scrolls, and
    /// it only redraws when that changes.
    /// </summary>
    public sealed class LevelNodeView : MonoBehaviour
    {
        [SerializeField] private RectTransform rect;
        [SerializeField] private RectTransform pulseTarget;
        [SerializeField] private Image face;
        [SerializeField] private TMP_Text number;
        [SerializeField] private Button button;

        [SerializeField] private Sprite flownFace;
        [SerializeField] private Sprite currentFace;
        [SerializeField] private Sprite lockedFace;
        [SerializeField] private Color flownText = new Color32(52, 64, 96, 255);
        [SerializeField] private Color currentText = new Color32(214, 84, 76, 255);
        [SerializeField] private Color lockedText = new Color32(150, 156, 172, 255);

        private RouteStopState state;

        public event Action<int> OnTapped;

        public int Level { get; private set; }

        private void OnEnable()
        {
            button.onClick.AddListener(HandleClick);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(HandleClick);
            pulseTarget.DOKill();
            pulseTarget.localScale = Vector3.one;
            Level = 0;
        }

        public void Show(int level, RouteStopState stopState, Vector2 position)
        {
            rect.anchoredPosition = position;

            if (level == Level && stopState == state)
            {
                return;
            }

            Level = level;
            state = stopState;
            number.SetText("{0}", level);
            face.sprite = stopState == RouteStopState.Flown ? flownFace : stopState == RouteStopState.Current ? currentFace : lockedFace;
            number.color = stopState == RouteStopState.Flown ? flownText : stopState == RouteStopState.Current ? currentText : lockedText;
            button.interactable = stopState != RouteStopState.Locked;

            pulseTarget.DOKill();
            pulseTarget.localScale = Vector3.one;

            if (stopState == RouteStopState.Current)
            {
                // The next level to fly stands out: larger than the rest, and breathing.
                pulseTarget.localScale = Vector3.one * 1.12f;
                pulseTarget.DOScale(1.22f, 0.7f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
            }
        }

        /// <summary>Takes the theme's gate signs; the next <see cref="Show"/> redraws with them.</summary>
        public void SetFaces(Sprite flown, Sprite current, Sprite locked)
        {
            flownFace = flown;
            currentFace = current;
            lockedFace = locked;
            Level = 0;
        }

        private void HandleClick()
        {
            OnTapped?.Invoke(Level);
        }

#if UNITY_EDITOR
        public void EditorLink(RectTransform linkedRect, RectTransform linkedPulse, Image linkedFace, TMP_Text linkedNumber,
            Button linkedButton, Sprite linkedFlown, Sprite linkedCurrent, Sprite linkedLocked)
        {
            rect = linkedRect;
            pulseTarget = linkedPulse;
            face = linkedFace;
            number = linkedNumber;
            button = linkedButton;
            flownFace = linkedFlown;
            currentFace = linkedCurrent;
            lockedFace = linkedLocked;
        }
#endif
    }
}
