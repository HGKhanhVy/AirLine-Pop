namespace AirlinePop.SingleLine.Unity
{
    /// <summary>
    /// The player's three switches. Audio and haptics own their flags and save them, so
    /// everything that plays a sound or a buzz honours them without being told.
    /// </summary>
    public sealed class SettingsService : ISettingsService
    {
        private readonly GameAudio audio;
        private readonly DeviceHaptics haptics;

        public SettingsService(GameAudio audio, DeviceHaptics haptics)
        {
            this.audio = audio;
            this.haptics = haptics;
        }

        public bool IsMusicOn => audio != null && audio.IsMusicOn;

        public bool IsSoundOn => audio != null && audio.IsSoundOn;

        public bool IsHapticOn => haptics != null && haptics.IsOn;

        public void SetMusicOn(bool isOn)
        {
            audio?.SetMusicOn(isOn);
        }

        public void SetSoundOn(bool isOn)
        {
            audio?.SetSoundOn(isOn);
        }

        public void SetHapticOn(bool isOn)
        {
            haptics?.SetOn(isOn);
        }
    }
}
