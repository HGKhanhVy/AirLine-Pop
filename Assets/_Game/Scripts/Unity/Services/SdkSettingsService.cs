using ASTeams.Base;
using ASTeams.Base.Data;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Settings kept where the SDK already keeps them: the audio and vibration
    /// controllers own the mute flags and write them into the saved profile, so
    /// everything that plays a sound or a buzz honours them without being told.
    /// </summary>
    public sealed class SdkSettingsService : ISettingsService
    {
        // Mirrors of the SDK's private save keys; the SDK is read-only here.
        private const string MuteMusicKey = "settingMuteMusic";
        private const string MuteSoundKey = "settingMuteSound";
        private const string MuteVibrationKey = "settingMuteVibration";

        private readonly AudioController audio;
        private readonly VibrationController vibration;
        private readonly UserProfileController profile;

        public SdkSettingsService(AudioController audio, VibrationController vibration, UserProfileController profile)
        {
            this.audio = audio;
            this.vibration = vibration;
            this.profile = profile;
        }

        public bool IsMusicOn => audio != null && !audio.IsMuteMusic;

        public bool IsSoundOn => audio != null && !audio.IsMuteSound;

        public bool IsHapticOn => vibration != null && !vibration.IsMuteVibration;

        public void SetMusicOn(bool isOn)
        {
            // The audio controller only exposes toggles.
            if (audio != null && IsMusicOn != isOn)
            {
                audio.ToggleMusic();
            }
        }

        public void SetSoundOn(bool isOn)
        {
            if (audio != null && IsSoundOn != isOn)
            {
                audio.ToggleSound();
            }
        }

        public void SetHapticOn(bool isOn)
        {
            if (vibration != null)
            {
                vibration.SetMuteVibration(!isOn);
            }
        }

        /// <summary>
        /// Re-reads the saved switches. The controllers read them in their own Awake, and
        /// when that runs before the profile has loaded they silently fall back to "on",
        /// so a player who muted the music hears it again on the next launch. Call once
        /// every Awake has run.
        /// </summary>
        public void SyncWithProfile()
        {
            if (profile == null)
            {
                return;
            }

            SetMusicOn(!profile.GetParam<bool>(MuteMusicKey));
            SetSoundOn(!profile.GetParam<bool>(MuteSoundKey));
            SetHapticOn(!profile.GetParam<bool>(MuteVibrationKey));
        }
    }
}
