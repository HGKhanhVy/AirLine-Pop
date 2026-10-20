using CandyCoded.HapticFeedback;
using UnityEngine;

namespace AirlinePop.SingleLine.Unity
{
    /// <summary>
    /// The phone's taptic engine, behind the player's vibration switch. Two buzzes closer
    /// than the floor below merge into one, so a fast drag cannot rattle the phone.
    ///
    /// Nothing happens in the Editor; haptics can only be judged on a device.
    /// </summary>
    public sealed class DeviceHaptics : IHapticService
    {
        // Kept from the first builds so a player who turned vibration off keeps it off.
        private const string VibrationOffKey = "settingMuteVibration";
        private const float MinSecondsBetween = 0.04f;

        private static DeviceHaptics instance;

        private float lastPlayedAt = float.NegativeInfinity;

        private DeviceHaptics()
        {
            IsOn = !PlayerProfile.Instance.Get<bool>(VibrationOffKey);
        }

        public static DeviceHaptics Instance => instance ??= new DeviceHaptics();

        public bool IsOn { get; private set; }

        public void SetOn(bool isOn)
        {
            IsOn = isOn;
            PlayerProfile.Instance.Set(VibrationOffKey, !isOn);
        }

        public void Play(HapticStrength strength)
        {
            if (!IsOn || Time.unscaledTime - lastPlayedAt < MinSecondsBetween)
            {
                return;
            }

            lastPlayedAt = Time.unscaledTime;

            switch (strength)
            {
                case HapticStrength.Medium:
                    HapticFeedback.MediumFeedback();
                    break;

                case HapticStrength.Heavy:
                    HapticFeedback.HeavyFeedback();
                    break;

                default:
                    HapticFeedback.LightFeedback();
                    break;
            }
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlaySession()
        {
            instance = null;
        }
#endif
    }
}
