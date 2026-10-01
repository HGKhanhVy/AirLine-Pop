using ASTeams.Base;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// What the win screen sounds and feels like, kept out of the screen itself so the
    /// popup only lays things out and moves them.
    /// </summary>
    public sealed class WinPopupFeedback
    {
        private readonly AudioController audio;
        private readonly IHapticService haptics;
        private readonly AudioClip coinClip;
        private readonly AudioSource coinSource;
        private readonly float coinVolume;

        public WinPopupFeedback(AudioController audio, IHapticService haptics,
            AudioClip coinClip, AudioSource coinSource, float coinVolume)
        {
            this.audio = audio;
            this.haptics = haptics;
            this.coinClip = coinClip;
            this.coinSource = coinSource;
            this.coinVolume = coinVolume;
        }

        /// <summary>
        /// Only a buzz as the screen fades in. A fanfare here ran over the board's own
        /// completion cue on every win.
        /// </summary>
        public void PlayOpen()
        {
            Buzz(HapticStrength.Medium);
        }

        public void PlayReveal()
        {
            if (audio != null)
            {
                audio.PlaySound(SoundName.UI_Win);
            }

            Buzz(HapticStrength.Heavy);
        }

        /// <summary>No sound on the press: what the player is waiting to hear is the coins.</summary>
        public void PlayContinue()
        {
            Buzz(HapticStrength.Light);
        }

        /// <summary>
        /// Play, not PlayOneShot: coins land 30 to 90 ms apart and the chime rings for half
        /// a second, so each new coin cuts off the one before instead of stacking into a
        /// wash. The mute setting is honoured here because this source is not the audio
        /// controller's.
        /// </summary>
        public void PlayCoin()
        {
            if (coinClip == null)
            {
                return;
            }

            if (coinSource == null)
            {
                if (audio != null)
                {
                    audio.PlaySound(coinClip, coinVolume);
                }

                return;
            }

            bool isMuted = audio != null && audio.IsMuteSound;
            coinSource.clip = coinClip;
            coinSource.volume = isMuted ? 0f : coinVolume;
            coinSource.Play();
        }

        private void Buzz(HapticStrength strength)
        {
            if (haptics != null)
            {
                haptics.Play(strength);
            }
        }
    }
}
