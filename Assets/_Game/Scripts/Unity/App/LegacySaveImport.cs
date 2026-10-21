#if UNITY_ANDROID || UNITY_EDITOR
using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace AirlinePop.SingleLine.Unity
{
    /// <summary>
    /// Carries an Android tester's save from the first builds into the current layout,
    /// once, then removes the old keys. Those builds never shipped on iOS, so this is not
    /// compiled there.
    /// </summary>
    internal static class LegacySaveImport
    {
        private const string RecordKey = "USER_DATA";
        private const string ConsentKey = "HasAcceptedConsent";

        // Old value name -> current one; every other value keeps its name.
        private static readonly (string from, string to)[] Renames =
        {
            ("settingMuteMusic", "music_off"),
            ("settingMuteSound", "sound_off"),
            ("settingMuteVibration", "vibration_off"),
            ("firstTime", "seeded"),
        };

        // Values the old builds wrote for systems the game no longer has.
        private static readonly string[] Dropped =
        {
            "currentLevel", "adsTicket", "winStreak", "openedWinStreakCount", "timeToAddLife", "coin", "life",
            "consentPersonalizedAds"
        };

        private const string DroppedPrefix = "booster_";

        /// <summary>The old save converted to the current layout, or false when there is none.</summary>
        public static bool TryRead(out string json)
        {
            json = null;

            if (!PlayerPrefs.HasKey(RecordKey))
            {
                return false;
            }

            JObject record = Parse(PlayerPrefs.GetString(RecordKey));
            JObject oldValues = Parse(record.Value<string>("data"));
            var values = new JObject();

            foreach (JProperty property in oldValues.Properties())
            {
                if (!IsDropped(property.Name))
                {
                    values[property.Name] = property.Value;
                }
            }

            TryUpgradeValues(values);

            var root = new JObject
            {
                ["coins"] = record.Value<long?>("coin") ?? 0,
                ["level"] = Math.Max(1, record.Value<int?>("level") ?? 1),
                ["values"] = values
            };

            json = root.ToString(Newtonsoft.Json.Formatting.None);
            return true;
        }

        public static bool TryUpgradeValues(JObject values)
        {
            bool hasChanges = false;

            foreach ((string from, string to) in Renames)
            {
                if (!values.TryGetValue(from, out JToken oldValue))
                {
                    continue;
                }

                // An explicit current value wins even when it is false.
                if (!values.ContainsKey(to))
                {
                    values[to] = oldValue;
                }

                values.Remove(from);
                hasChanges = true;
            }

            if (!values.ContainsKey("consent") && PlayerPrefs.GetInt(ConsentKey, 0) == 1)
            {
                values["consent"] = true;
                hasChanges = true;
            }

            return hasChanges;
        }

        /// <summary>Only called after the upgraded profile has been saved successfully.</summary>
        public static void CompleteImport()
        {
            PlayerPrefs.DeleteKey(RecordKey);
            PlayerPrefs.DeleteKey(ConsentKey);
            PlayerPrefs.Save();
        }

        private static bool IsDropped(string name)
        {
            return Array.IndexOf(Dropped, name) >= 0 || name.StartsWith(DroppedPrefix, StringComparison.Ordinal);
        }

        private static JObject Parse(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return new JObject();
            }

            try
            {
                return JObject.Parse(json);
            }
            catch (Exception)
            {
                return new JObject();
            }
        }
    }
}
#endif
