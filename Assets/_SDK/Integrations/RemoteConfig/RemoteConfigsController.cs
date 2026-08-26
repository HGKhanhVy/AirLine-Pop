#if REMOTE_CONFIGS
using Firebase;
using Firebase.Extensions;
using Firebase.RemoteConfig;
#endif

using Newtonsoft.Json.Linq;
using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace ASTeams.Base.RemoteConfigs
{
    public class RemoteConfigsController : MonoSingleton<RemoteConfigsController>
    {
        // ===========================================================
        // CONFIG VALUES (stored directly in this class)
        // ===========================================================
        public static int ads_interval = 300;               // Thời gian delay inter (default 300s)
        public static int level_show_banner = 10;            // Level bắt đầu show banner
        public static int level_show_inter = 5;             // Level bắt đầu show inter
        public static bool show_banner_gameplay = false;     // Có show BN trong gameplay không
        public static bool hien_qc = true;                 // Bật tắt ads (bật sau review store)
        public static bool rating_popup = true;             // Bật tắt popup rating

        //LEVEL
        public static float level_fail_difficult_multiplier = 1.2f; //thời gian tăng thêm sau mỗi lần thua: sau lần 1 => thời gian *= 1.2f
        public static string level_difficult_multiplier = "{}"; //thay đổi độ khó động. ví dụ: {level:3, difficult_multiplier: 1.2} => level 3 thời gian bắt đầu *= 1.2f
        private static JObject _levelDifficultyJson;
        private static bool _levelDifficultyParsed = false;

        //CURRENCY
        public static int coin_start = 300; //số tiền coin bắt đầu
        public static int coin_watch_ads = 100; //số tiền mỗi lần watch ads free trong shop
        public static int coin_revive_price = 300; //số tiền coin revive
        public static int coin_win_level = 40; //số coin khi win level

        //BOOSTER PRICES (JSON format: {"pack": 100, "freeze": 100, "shuffle": 100})
        public static string booster_prices = "{\"pack\": 100, \"freeze\": 100, \"shuffle\": 100}";
        private static JObject _boosterPricesJson;
        private static bool _boosterPricesParsed = false;


#if REMOTE_CONFIGS

        private bool isFirebaseInitialized = false;
        public UnityEvent<bool> onLoadRemoteConfigs = new UnityEvent<bool>();

        // ===========================================================
        // INITIALIZE FIREBASE
        // ===========================================================
        public Task InitializeFirebase()
        {
            return FirebaseApp.CheckAndFixDependenciesAsync()
                .ContinueWithOnMainThread(task =>
                {
                    FirebaseApp app = FirebaseApp.DefaultInstance;
                    isFirebaseInitialized = true;

                    Debug.Log("[REMOTE] Firebase initialized.");
                    FetchRemoteConfigDataAsync();
                });
        }

        // ===========================================================
        // FETCH REMOTE CONFIGS
        // ===========================================================
        public Task FetchRemoteConfigDataAsync()
        {
            if (!isFirebaseInitialized)
            {
                Debug.LogWarning("[REMOTE] Firebase not initialized.");
                onLoadRemoteConfigs?.Invoke(false);
                return Task.FromResult(0);
            }

            var fetchTask = FirebaseRemoteConfig.DefaultInstance.FetchAsync(TimeSpan.Zero);

            return fetchTask.ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && !task.IsFaulted && !task.IsCanceled)
                {
                    FirebaseRemoteConfig.DefaultInstance.ActivateAsync()
                        .ContinueWithOnMainThread(activeTask =>
                        {
                            if (activeTask.IsCompleted)
                            {
                                Debug.Log("[REMOTE] Remote config activated.");

                                LoadRemoteConfigs();

                                onLoadRemoteConfigs?.Invoke(true);
                            }
                            else
                            {
                                Debug.LogError("[REMOTE] Failed to activate.");
                                onLoadRemoteConfigs?.Invoke(false);
                            }
                        });
                }
                else
                {
                    Debug.LogError("[REMOTE] Fetch failed.");
                    onLoadRemoteConfigs?.Invoke(false);
                }
            });
        }

        // ===========================================================
        // LOAD VALUES DIRECTLY INTO THIS CONTROLLER
        // ===========================================================
        public void LoadRemoteConfigs()
        {
            Debug.Log("[REMOTE] Loading config values...");

            // ================= ADS =================
            ads_interval = GetValue<int>("ads_interval", ads_interval);
            level_show_banner = GetValue<int>("level_show_banner", level_show_banner);
            level_show_inter = GetValue<int>("level_show_inter", level_show_inter);
            show_banner_gameplay = GetValue<bool>("show_banner_gameplay", show_banner_gameplay);
            hien_qc = GetValue<bool>("hien_qc", hien_qc);
            rating_popup = GetValue<bool>("rating_popup", rating_popup);

            // ================= LEVEL =================
            level_fail_difficult_multiplier =
                GetValue<float>("level_fail_difficult_multiplier", level_fail_difficult_multiplier);

            level_difficult_multiplier =
                GetValue<string>("level_difficult_multiplier", "{}");

            // Parse JSON ONE TIME
            try
            {
                _levelDifficultyJson = JObject.Parse(level_difficult_multiplier);
                _levelDifficultyParsed = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[REMOTE] Parse level_difficult_multiplier failed: " + e.Message);
                _levelDifficultyJson = null;
                _levelDifficultyParsed = false;
            }

            // ================= CURRENCY =================
            coin_start = GetValue<int>("coin_start", coin_start);
            coin_watch_ads = GetValue<int>("coin_watch_ads", coin_watch_ads);
            coin_revive_price = GetValue<int>("coin_revive_price", coin_revive_price);
            coin_win_level = GetValue<int>("coin_win_level", coin_win_level);

            // ================= BOOSTER PRICES =================
            booster_prices = GetValue<string>("booster_prices", booster_prices);

            // Parse JSON ONE TIME
            try
            {
                _boosterPricesJson = JObject.Parse(booster_prices);
                _boosterPricesParsed = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[REMOTE] Parse booster_prices failed: " + e.Message);
                _boosterPricesJson = null;
                _boosterPricesParsed = false;
            }

            // ================= LOG =================
            Debug.Log(
                $"[REMOTE] LOADED CONFIGS:\n" +
                $"- ads_interval = {ads_interval}\n" +
                $"- level_show_banner = {level_show_banner}\n" +
                $"- level_show_inter = {level_show_inter}\n" +
                $"- show_banner_gameplay = {show_banner_gameplay}\n" +
                $"- hien_qc = {hien_qc}\n" +
                $"- rating_popup = {rating_popup}\n" +
                $"- level_fail_difficult_multiplier = {level_fail_difficult_multiplier}\n" +
                $"- level_difficult_multiplier = {level_difficult_multiplier}\n" +
                $"- coin_start = {coin_start}\n" +
                $"- coin_watch_ads = {coin_watch_ads}\n" +
                $"- coin_revive_price = {coin_revive_price}\n" +
                $"- coin_win_level = {coin_win_level}\n" +
                $"- booster_prices = {booster_prices}\n"
            );
        }

        // ===========================================================
        // GENERIC GETTERS
        // ===========================================================
        public T GetValue<T>(string key, T defaultValue = default)
        {
            try
            {
                var v = FirebaseRemoteConfig.DefaultInstance.GetValue(key);

                if (typeof(T) == typeof(string))
                    return (T)(object)v.StringValue;

                if (typeof(T) == typeof(int))
                    return (T)(object)(int)v.LongValue;

                if (typeof(T) == typeof(long))
                    return (T)(object)v.LongValue;

                if (typeof(T) == typeof(bool))
                    return (T)(object)v.BooleanValue;

                if (typeof(T) == typeof(float))
                    return (T)(object)(float)v.DoubleValue;

                if (typeof(T) == typeof(double))
                    return (T)(object)v.DoubleValue;

                if (typeof(T) == typeof(JObject))
                    return (T)(object)JObject.Parse(v.StringValue);

                Debug.LogWarning($"[REMOTE] Unsupported type for '{key}'");
                return defaultValue;
            }
            catch
            {
                Debug.LogWarning($"[REMOTE] Failed to load key '{key}', using default.");
                return defaultValue;
            }
        }

        public bool TryGetValue<T>(string key, out T value)
        {
            try
            {
                value = GetValue<T>(key);
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }

        public string GetJson(string key)
        {
            try
            {
                return FirebaseRemoteConfig.DefaultInstance.GetValue(key).StringValue;
            }
            catch
            {
                return "{}";
            }
        }

        public static float GetLevelDifficultyMultiplier(int level)
        {
            if (!_levelDifficultyParsed || _levelDifficultyJson == null)
                return 1f;

            var token = _levelDifficultyJson[level.ToString()];
            if (token == null)
                return 1f;

            return token.Value<float>();
        }

        // ===========================================================
        // BOOSTER PRICE GETTER
        // ===========================================================
        /// <summary>
        /// Lấy giá booster từ Remote Config dựa trên loại booster
        /// </summary>
        /// <param name="boosterType">Loại booster (Pack, Freeze, Shuffle)</param>
        /// <param name="defaultPrice">Giá mặc định nếu không tìm thấy trong Remote Config</param>
        /// <returns>Giá booster từ Remote Config hoặc giá mặc định</returns>
        public static int GetBoosterPrice(eBooster boosterType, int defaultPrice = 100)
        {
            if (!_boosterPricesParsed || _boosterPricesJson == null)
                return defaultPrice;

            // Map booster type to JSON key
            string key = boosterType switch
            {
                eBooster.Pack => "pack",
                eBooster.Freeze => "freeze",
                eBooster.Shuffle => "shuffle",
                _ => null
            };

            if (key == null)
                return defaultPrice;

            var token = _boosterPricesJson[key];
            if (token == null)
                return defaultPrice;

            return token.Value<int>();
        }

#endif
    }
}
