using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The weathers night flights fly through. Each night level gets one of those its number
    /// allows, picked from the number so the same level always has the same weather.
    /// </summary>
    [CreateAssetMenu(menuName = "AirLine Pop/Night Weather", fileName = "NightWeather")]
    public sealed class NightWeatherConfigSO : ScriptableObject
    {
        [SerializeField] private NightWeatherPreset[] presets = System.Array.Empty<NightWeatherPreset>();

        public bool TryPick(int levelNumber, out NightWeatherPreset weather)
        {
            weather = default;
            int allowed = 0;

            for (int i = 0; i < presets.Length; i++)
            {
                if (presets[i].fromLevel <= levelNumber)
                {
                    allowed++;
                }
            }

            if (allowed == 0)
            {
                return false;
            }

            // A cheap integer hash, so neighbouring night levels do not walk through the list in order.
            int pick = (int)(((uint)levelNumber * 2654435761u) >> 16) % allowed;

            for (int i = 0; i < presets.Length; i++)
            {
                if (presets[i].fromLevel > levelNumber)
                {
                    continue;
                }

                if (pick == 0)
                {
                    weather = presets[i];
                    return true;
                }

                pick--;
            }

            return false;
        }

#if UNITY_EDITOR
        public bool TryPickAt(int index, out NightWeatherPreset weather)
        {
            bool isValid = index >= 0 && index < presets.Length;
            weather = isValid ? presets[index] : default;
            return isValid;
        }

        public void EditorSetPresets(NightWeatherPreset[] linkedPresets)
        {
            presets = linkedPresets;
        }
#endif
    }
}
