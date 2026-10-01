using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// One on/off switch on the settings screen: the knob slides across and the track
    /// swaps to its "on" or "off" drawing (a swap rather than a tint, so the outline stays
    /// brown). Reports the player's changes; being set from code stays silent and snaps, so
    /// the screen opens already showing the saved state.
    /// </summary>
    public sealed class SettingToggleView : MonoBehaviour
    {
        [SerializeField] private Toggle toggle;
        [SerializeField] private Image track;
        [SerializeField] private Image knob;

        [Header("Look")]
        [SerializeField] private Sprite trackOn;
        [SerializeField] private Sprite trackOff;
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
            track.sprite = isOn ? trackOn : trackOff;
        }

        private void Animate(bool isOn)
        {
            motion?.Kill();
            track.sprite = isOn ? trackOn : trackOff;

            // Unscaled: the settings screen can be opened while the game is paused.
            motion = DOTween.Sequence()
                .Join(knob.rectTransform.DOAnchorPosX(KnobX(isOn), duration).SetEase(Ease.OutBack))
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
