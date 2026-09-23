using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Sounds the popups make, kept apart from the SDK's shared sound table so they can be
    /// tuned for this game alone. Both sit well under the button click, which always plays
    /// at the same moment: the popup should read as the click's echo, not a second hit.
    /// </summary>
    [CreateAssetMenu(menuName = "Single Line/UI Sound Config", fileName = "UiSoundConfig")]
    public sealed class UiSoundConfigSO : ScriptableObject
    {
        [Tooltip("Leave empty for a silent open; the button click already marks the moment.")]
        [SerializeField] private AudioClip popupOpen;
        [SerializeField, Range(0f, 1f)] private float popupOpenVolume = 0.35f;

        [Tooltip("Leave empty for a silent close.")]
        [SerializeField] private AudioClip popupClose;
        [SerializeField, Range(0f, 1f)] private float popupCloseVolume = 0.25f;

        public AudioClip PopupOpen => popupOpen;

        public float PopupOpenVolume => popupOpenVolume;

        public AudioClip PopupClose => popupClose;

        public float PopupCloseVolume => popupCloseVolume;
    }
}
