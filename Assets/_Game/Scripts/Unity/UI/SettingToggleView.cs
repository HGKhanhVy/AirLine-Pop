using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// One on/off switch on the settings screen: the knob slides across and the track
    /// changes colour. Reports the player's changes; being set from code stays silent and
    /// snaps, so the screen opens already showing the saved state.
    /// </summary>
    public sealed class SettingToggleView : MonoBehaviour
    {
        [SerializeField] private Toggle toggle;
        [SerializeField] private Image track;
        [SerializeField] private Image knob;

        [Header("Look")]
        [SerializeField] private Color trackOnColor = new Color32(46, 204, 140, 255);
        [SerializeField] private Color trackOffColor = new Color32(78, 90, 102, 255);
        [SerializeField] private Color knobOnColor = Color.white;
        [SerializeField] private Color knobOffColor = new Color32(170, 175, 180, 255);
        [SerializeField, Min(0f)] private float knobTravel = 30f;
        [SerializeField, Min(0f)] private float duration = 0.15f;

        private Sequence motion;

        public event Action<bool> OnChanged;

        private void OnEnable()
        {
            toggle.onValueChanged.AddListener(HandleValueChanged);
        }

        private void OnDisable()
        {
            toggle.onValueChanged.RemoveListener(HandleValueChanged);
            motion?.Kill(true);
        }

        public void SetIsOn(bool isOn)
        {
            toggle.SetIsOnWithoutNotify(isOn);
            motion?.Kill();
            ApplyInstant(isOn);
        }

        private void HandleValueChanged(bool isOn)
        {
            Animate(isOn);
            OnChanged?.Invoke(isOn);
        }

        private void ApplyInstant(bool isOn)
        {
            knob.rectTransform.anchoredPosition = new Vector2(KnobX(isOn), knob.rectTransform.anchoredPosition.y);
            track.color = isOn ? trackOnColor : trackOffColor;
            knob.color = isOn ? knobOnColor : knobOffColor;
        }

        private void Animate(bool isOn)
        {
            motion?.Kill();

            // Unscaled: the settings screen can be opened while the game is paused.
            motion = DOTween.Sequence()
                .Join(knob.rectTransform.DOAnchorPosX(KnobX(isOn), duration).SetEase(Ease.OutQuad))
                .Join(track.DOColor(isOn ? trackOnColor : trackOffColor, duration))
                .Join(knob.DOColor(isOn ? knobOnColor : knobOffColor, duration))
                .SetUpdate(true);
        }

        private float KnobX(bool isOn)
        {
            return isOn ? knobTravel : -knobTravel;
        }

        private void OnDestroy()
        {
            motion?.Kill();
        }
    }
}
