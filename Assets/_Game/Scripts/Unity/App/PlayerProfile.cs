using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace AirlinePop.SingleLine.Unity
{
    /// <summary>
    /// The player's save: coin balance, campaign position and a bag of named values each
    /// feature keeps its own state in. One PlayerPrefs entry holds all of it, so a coin
    /// change and the value it pays for are written in the same save.
    ///
    /// A global service rather than a scene object: every scene reads it, and a scene
    /// opened on its own in the editor still finds the real save.
    /// </summary>
    public sealed class PlayerProfile
    {
        private const string StorageKey = "airlinepop_profile";

        private const string CoinsField = "coins";
        private const string LevelField = "level";
        private const string ValuesField = "values";

        private static PlayerProfile instance;

        private long coins;
        private int levelNumber;
        private JObject values;

        private PlayerProfile()
        {
        }

        public static PlayerProfile Instance => instance ??= Load();

        /// <summary>Raised after every save.</summary>
        public event Action Changed;

        public long Coins => coins;

        public int LevelNumber
        {
            get => levelNumber;
            set
            {
                levelNumber = Math.Max(1, value);
                Save();
            }
        }

        public void AddCoins(long amount)
        {
            coins = Math.Max(0, coins + amount);
            Save();
        }

        public bool TrySpend(long amount)
        {
            if (amount < 0 || coins < amount)
            {
                return false;
            }

            coins -= amount;
            Save();
            return true;
        }

        public T Get<T>(string key)
        {
            if (!values.TryGetValue(key, out JToken token))
            {
                return default;
            }

            try
            {
                return token.ToObject<T>();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Saved value '" + key + "' could not be read: " + exception.Message);
                return default;
            }
        }

        public void Set(string key, object value)
        {
            Store(key, value);
            Save();
        }

        /// <summary>Stores a value and moves the balance in one write, so neither lands without the other.</summary>
        public void Set(string key, object value, long coinDelta)
        {
            coins = Math.Max(0, coins + coinDelta);
            Store(key, value);
            Save();
        }

        private void Store(string key, object value)
        {
            values[key] = value == null ? JValue.CreateNull() : JToken.FromObject(value);
        }

        private void Save()
        {
            var root = new JObject
            {
                [CoinsField] = coins,
                [LevelField] = levelNumber,
                [ValuesField] = values
            };

            PlayerPrefs.SetString(StorageKey, root.ToString(Newtonsoft.Json.Formatting.None));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        private static PlayerProfile Load()
        {
            var profile = new PlayerProfile();

            if (PlayerPrefs.HasKey(StorageKey))
            {
                profile.Read(PlayerPrefs.GetString(StorageKey));
#if UNITY_ANDROID || UNITY_EDITOR
                if (LegacySaveImport.TryUpgradeValues(profile.values))
                {
                    profile.Save();
                    LegacySaveImport.CompleteImport();
                }
#endif
                return profile;
            }

#if UNITY_ANDROID || UNITY_EDITOR
            if (LegacySaveImport.TryRead(out string imported))
            {
                profile.Read(imported);
                profile.Save();
                LegacySaveImport.CompleteImport();
                return profile;
            }
#endif

            profile.levelNumber = 1;
            profile.values = new JObject();
            return profile;
        }

        private void Read(string json)
        {
            JObject root = ParseOrEmpty(json);
            coins = root.Value<long?>(CoinsField) ?? 0;
            levelNumber = Math.Max(1, root.Value<int?>(LevelField) ?? 1);
            values = root[ValuesField] as JObject ?? new JObject();
        }

        private static JObject ParseOrEmpty(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return new JObject();
            }

            try
            {
                return JObject.Parse(json);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Save could not be parsed, starting a fresh one: " + exception.Message);
                return new JObject();
            }
        }

#if UNITY_EDITOR
        // Enter Play Mode runs without a domain reload here, so the cached save would
        // otherwise outlive edits made to PlayerPrefs between two play sessions.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlaySession()
        {
            instance = null;
        }

        /// <summary>Drops the cached save so editor tools read what PlayerPrefs now holds.</summary>
        public static void EditorReload()
        {
            instance = null;
        }
#endif
    }
}
