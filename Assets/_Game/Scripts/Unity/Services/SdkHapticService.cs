using ASTeams.Base;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Haptics through the SDK's <see cref="VibrationController"/>.
    ///
    /// The controller is handed in once and held, never looked up. Its own settings
    /// already cover what GDD 4 asks for: a 40 ms floor between two buzzes so a fast drag
    /// cannot rattle the phone, and a mute the player owns.
    ///
    /// It reports no vibrator outside Android and iOS, so this does nothing in the Editor.
    /// That is expected, not a fault: haptics can only be judged on a device.
    /// </summary>
    public sealed class SdkHapticService : IHapticService
    {
        private readonly VibrationController vibration;

        public SdkHapticService(VibrationController vibration)
        {
            this.vibration = vibration;
        }

        public void Play(HapticStrength strength)
        {
            if (vibration == null)
            {
                return;
            }

            vibration.Play(ToSdk(strength));
        }

        private static VibrationController.HapticType ToSdk(HapticStrength strength)
        {
            switch (strength)
            {
                case HapticStrength.Medium:
                    return VibrationController.HapticType.Medium;

                case HapticStrength.Heavy:
                    return VibrationController.HapticType.Heavy;

                default:
                    return VibrationController.HapticType.Light;
            }
        }
    }
}
