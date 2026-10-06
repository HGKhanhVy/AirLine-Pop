using System;
using DG.Tweening;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The weather a night flight flies through, from the night weather config: mist and
    /// rain, both drawn into the dark itself, and lightning. It
    /// comes in as night falls; at dawn the fog lifts and the storm passes, but the rain
    /// keeps falling until the next flight.
    /// </summary>
    public sealed class NightWeatherView : MonoBehaviour
    {
        [SerializeField] private NightWeatherConfigSO config;
        [SerializeField] private NightLightningView lightning;
        [SerializeField, Min(0.05f)] private float fadeSeconds = 0.8f;

        [Tooltip("Editor testing: the preset every night flight uses, by its place in the config. -1 picks by level as in the game.")]
        [SerializeField] private int forcedPreset = -1;

        private INightShade shade;
        private Tween fogTween;
        private Tween rainTween;
        private float fog;
        private float rain;

        public event Action<float> OnFogChanged;

        public event Action<float> OnRainChanged;

        public void Initialize(INightShade nightShade, BoardView board)
        {
            shade = nightShade;
            lightning.Initialize(nightShade, board);
        }

        private void OnDisable()
        {
            KillTweens();
        }

        public void Begin(int levelNumber)
        {
            if (!TryPick(levelNumber, out NightWeatherPreset weather))
            {
                Clear(0f);
                return;
            }

            FadeTo(weather.fog, weather.rain, fadeSeconds);

            if (weather.HasLightning)
            {
                lightning.Begin(weather.lightningEvery);
            }
            else
            {
                lightning.Stop();
            }
        }

        /// <summary>Morning: the fog lifts and the lightning stops, but the rain keeps falling.</summary>
        public void Brighten(float seconds)
        {
            lightning.Stop();
            FadeTo(0f, rain, seconds);
        }

        /// <summary>Clears the weather, over <paramref name="seconds"/>, or at once for 0.</summary>
        public void Clear(float seconds)
        {
            lightning.Stop();
            FadeTo(0f, 0f, seconds);
        }

        private bool TryPick(int levelNumber, out NightWeatherPreset weather)
        {
#if UNITY_EDITOR
            if (forcedPreset >= 0)
            {
                return config.TryPickAt(forcedPreset, out weather);
            }
#endif
            return config.TryPick(levelNumber, out weather);
        }

        private void FadeTo(float targetFog, float targetRain, float seconds)
        {
            KillTweens();

            if (seconds <= 0f)
            {
                SetFog(targetFog);
                SetRain(targetRain);
                return;
            }

            fogTween = DOTween.To(() => fog, SetFog, targetFog, seconds);
            rainTween = DOTween.To(() => rain, SetRain, targetRain, seconds);
        }

        private void SetFog(float amount)
        {
            fog = amount;
            shade.SetFog(amount);
            OnFogChanged?.Invoke(amount);
        }

        private void SetRain(float amount)
        {
            rain = amount;
            shade.SetRain(amount);
            OnRainChanged?.Invoke(amount);
        }

        private void KillTweens()
        {
            fogTween?.Kill();
            fogTween = null;
            rainTween?.Kill();
            rainTween = null;
        }

#if UNITY_EDITOR
        public void EditorLink(NightWeatherConfigSO linkedConfig, NightLightningView linkedLightning)
        {
            config = linkedConfig;
            lightning = linkedLightning;
        }
#endif
    }
}
