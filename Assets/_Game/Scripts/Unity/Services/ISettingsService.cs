namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The player's own switches from the settings screen (GDD 9.1, 14). Every change is
    /// saved as it happens, so closing the app mid-screen loses nothing.
    /// </summary>
    public interface ISettingsService
    {
        bool IsMusicOn { get; }

        bool IsSoundOn { get; }

        bool IsHapticOn { get; }

        void SetMusicOn(bool isOn);

        void SetSoundOn(bool isOn);

        void SetHapticOn(bool isOn);
    }
}
