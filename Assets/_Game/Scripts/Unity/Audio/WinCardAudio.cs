using ASTeams.Base;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The sound of the win card: a reward chime as it comes in, a grander one for a flight
    /// worth stopping for, and coins ringing while the reward counts up. Only listens to
    /// the card, which knows nothing about sound.
    /// </summary>
    public sealed class WinCardAudio : MonoBehaviour
    {
        [SerializeField] private WinPanelView panel;

        [SerializeField] private AudioClip revealClip;
        [SerializeField, Range(0f, 1f)] private float revealVolume = 0.8f;

        [Tooltip("Played instead for a VIP flight, a newcomer, a postcard or a chapter's end.")]
        [SerializeField] private AudioClip milestoneClip;
        [SerializeField, Range(0f, 1f)] private float milestoneVolume = 0.8f;

        [Tooltip("One coin's ring while the reward counts up.")]
        [SerializeField] private AudioClip coinClip;
        [SerializeField, Range(0f, 1f)] private float coinVolume = 0.45f;

        [Tooltip("The count climbs by many coins a frame; this is the shortest gap between two rings.")]
        [SerializeField, Min(0.02f)] private float coinInterval = 0.07f;

        private AudioController audioController;
        private float lastCoinTime;

        private void OnEnable()
        {
            audioController = AudioController.Instance;
            panel.OnShown += HandleShown;
            panel.OnCoinsCounted += HandleCoinsCounted;
        }

        private void OnDisable()
        {
            panel.OnShown -= HandleShown;
            panel.OnCoinsCounted -= HandleCoinsCounted;
        }

        private void HandleShown(WinMilestone milestone)
        {
            bool isMilestone = milestone != WinMilestone.None && milestoneClip != null;
            Play(isMilestone ? milestoneClip : revealClip, isMilestone ? milestoneVolume : revealVolume);
        }

        private void HandleCoinsCounted()
        {
            if (Time.unscaledTime - lastCoinTime < coinInterval)
            {
                return;
            }

            lastCoinTime = Time.unscaledTime;
            Play(coinClip, coinVolume);
        }

        private void Play(AudioClip clip, float volume)
        {
            if (clip != null && audioController != null)
            {
                audioController.PlaySound(clip, volume);
            }
        }

#if UNITY_EDITOR
        public void EditorLink(WinPanelView linkedPanel, AudioClip linkedReveal, AudioClip linkedMilestone, AudioClip linkedCoin)
        {
            panel = linkedPanel;
            revealClip = linkedReveal;
            milestoneClip = linkedMilestone;
            coinClip = linkedCoin;
        }
#endif
    }
}
