using UnityEngine;

namespace AirlinePop.SingleLine.Unity
{
    /// <summary>
    /// Sounds the buttons and popups make. The popup sounds sit well under the button
    /// click, which always plays at the same moment: the popup should read as the click's
    /// echo, not a second hit.
    /// </summary>
    [CreateAssetMenu(menuName = "Single Line/UI Sound Config", fileName = "UiSoundConfig")]
    public sealed class UiSoundConfigSO : ScriptableObject
    {
        [SerializeField] private AudioClip buttonClick;
        [SerializeField, Range(0f, 1f)] private float buttonClickVolume = 1f;

        [Tooltip("Leave empty for a silent open; the button click already marks the moment.")]
        [SerializeField] private AudioClip popupOpen;
        [SerializeField, Range(0f, 1f)] private float popupOpenVolume = 0.35f;

        [Tooltip("Leave empty for a silent close.")]
        [SerializeField] private AudioClip popupClose;
        [SerializeField, Range(0f, 1f)] private float popupCloseVolume = 0.25f;

        public AudioClip ButtonClick => buttonClick;

        public float ButtonClickVolume => buttonClickVolume;

        public AudioClip PopupOpen => popupOpen;

        public float PopupOpenVolume => popupOpenVolume;

        public AudioClip PopupClose => popupClose;

        public float PopupCloseVolume => popupCloseVolume;
    }
}
