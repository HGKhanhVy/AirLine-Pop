using ASTeams.Base;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The sound of a night flight: the click of the torches coming on, rain and wind that
    /// swell and fade with the weather drawn on screen, and thunder rolling in a moment
    /// after each flash, the way sound trails light. Only listens; the night views never
    /// know it is there.
    /// </summary>
    public sealed class NightAudio : MonoBehaviour
    {
        [SerializeField] private NightFlightView night;
        [SerializeField] private NightWeatherView weather;
        [SerializeField] private NightLightningView lightning;

        [Header("Torch")]
        [SerializeField] private AudioClip torchClip;
        [SerializeField, Range(0f, 1f)] private float torchVolume = 0.8f;

        [Header("Weather loops")]
        [Tooltip("Rain, louder the harder it rains.")]
        [SerializeField] private AudioSource rainSource;
        [SerializeField, Range(0f, 1f)] private float rainVolume = 0.8f;

        [Tooltip("Wind, louder the thicker the mist.")]
        [SerializeField] private AudioSource windSource;
        [SerializeField, Range(0f, 1f)] private float windVolume = 0.7f;

        [Header("Thunder")]
        [SerializeField] private AudioSource thunderSource;
        [SerializeField, Range(0f, 1f)] private float thunderVolume = 0.9f;
        [SerializeField, Range(0f, 1f)] private float farThunderVolume = 0.4f;

        [Tooltip("Seconds between the flash and the thunder, picked between these each time.")]
        [SerializeField] private Vector2 thunderDelay = new Vector2(0.2f, 1f);

        private AudioController audioController;
        private float rain;
        private float wind;

        private bool IsMuted => audioController != null && audioController.IsMuteSound;

        private void OnEnable()
        {
            audioController = AudioController.Instance;
            night.OnTorchesOn += HandleTorchesOn;
            weather.OnRainChanged += HandleRainChanged;
            weather.OnFogChanged += HandleFogChanged;
            lightning.OnStrike += HandleStrike;
        }

        private void OnDisable()
        {
            night.OnTorchesOn -= HandleTorchesOn;
            weather.OnRainChanged -= HandleRainChanged;
            weather.OnFogChanged -= HandleFogChanged;
            lightning.OnStrike -= HandleStrike;
            rainSource.Stop();
            windSource.Stop();
            thunderSource.Stop();
        }

        /// <summary>The sound setting can change from the pause menu mid-flight, so the loops follow it.</summary>
        private void Update()
        {
            if (rain > 0f || wind > 0f)
            {
                ApplyLoop(rainSource, rain * rainVolume);
                ApplyLoop(windSource, wind * windVolume);
            }
        }

        private void HandleTorchesOn()
        {
            if (torchClip != null)
            {
                audioController?.PlaySound(torchClip, torchVolume);
            }
        }

        private void HandleRainChanged(float amount)
        {
            rain = amount;
            ApplyLoop(rainSource, rain * rainVolume);
        }

        private void HandleFogChanged(float amount)
        {
            wind = amount;
            ApplyLoop(windSource, wind * windVolume);
        }

        private void HandleStrike(bool hasBolt)
        {
            if (IsMuted)
            {
                return;
            }

            thunderSource.volume = hasBolt ? thunderVolume : farThunderVolume;
            thunderSource.PlayDelayed(Random.Range(thunderDelay.x, thunderDelay.y));
        }

        private void ApplyLoop(AudioSource source, float volume)
        {
            bool isAudible = volume > 0.001f && !IsMuted;
            source.volume = isAudible ? volume : 0f;

            if (isAudible && !source.isPlaying)
            {
                source.Play();
            }
            else if (volume <= 0.001f && source.isPlaying)
            {
                source.Stop();
            }
        }

#if UNITY_EDITOR
        public void EditorLink(NightFlightView linkedNight, NightWeatherView linkedWeather, NightLightningView linkedLightning,
            AudioSource linkedRain, AudioSource linkedWind, AudioSource linkedThunder, AudioClip linkedTorch)
        {
            night = linkedNight;
            weather = linkedWeather;
            lightning = linkedLightning;
            rainSource = linkedRain;
            windSource = linkedWind;
            thunderSource = linkedThunder;
            torchClip = linkedTorch;
        }
#endif
    }
}
