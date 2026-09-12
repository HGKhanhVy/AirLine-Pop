using ASTeams.Base.Analytics;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ASTeams.Base.Data
{
    [System.Serializable]
    public class LifeData
    {
        public int liveAmount; // số mạng thông thường
        public long liveInfinityExpire; // ticks UTC, 0 = không có infinity
        public long lastLifeTime;       // ticks UTC - thời điểm hồi mạng cuối cùng

        public const int MAX_LIVES = 5;        // số mạng tối đa
        public const int REFILL_TIME = 30 * 60;  // thời gian +1 live
    }

    public enum BoosterType
    {
        Booster1,
        Booster2,
        Booster3,
        Booster4
    }

    [System.Serializable]
    public class BoosterData
    {
        public List<int> boosterAmounts;
        public List<bool> unlockeds;

        public BoosterData()
        {
            boosterAmounts = new List<int>() { 3, 3, 3, 3 };
            unlockeds = new List<bool>() { false, false, false, false };
        }
    }

    public enum RewardType
    {
        Coin = 0,
        Lives = 1,
        LiveInfinity = 2,
        NextLevel = 3,

        Booster1 = 10,
        Booster2 = 11,
        Booster3 = 12,
        Booster4 = 13,

        WinStreakItem = 20
    }

    [Serializable]
    public class RewardItemData
    {
        public RewardType rewardType;
        public int amount;
        public string source; //từ nguồn nào
        public string reason; //nguyên nhân
        public bool isClaimed; //đã nhận chưa
    }

    [Serializable]
    public class RewardData
    {
        public List<RewardItemData> rewards = new List<RewardItemData>();
    }

    [Serializable]
    public class JourneyData
    {
        public List<int> rewardClaimeds = new List<int>();
        public List<int> chapterClaimeds = new List<int>();
    }

    [System.Serializable]
    public class UserData
    {
        public string id;
        public string country;
        public long coin;
        public int level = 1;

        public int winStreak;
        public int loseStreak;

        public string data = "{}";

        public LifeData life;
        public BoosterData booster;
        public RewardData reward; //kiểm tra và nhận vào main

        //Tab
        public JourneyData journey;
    }

    public class UserProfileController : MonoSingleton<UserProfileController>
    {
        private string LOCAL_KEY = "USER_DATA";
        public UserData userData;

        [HideInInspector]
        public UnityEvent<UserData> OnUserChanged = new UnityEvent<UserData>();

        public override void Init()
        {
            base.Init();
            LoadUserProfile(null);
        }

        #region COIN

        public void AddCoin(long coin)
        {
            userData.coin += coin;
            if (userData.coin < 0) userData.coin = 0;
            SaveUser(userData);
        }

        public bool UseCoin(long coin)
        {
            if (userData.coin >= coin)
            {
                userData.coin -= coin;
                SaveUser(userData);
                return true;
            }
            return false;
        }

        #endregion

        #region LEVEL
        public int LEVEL
        {
            get { return userData.level; }
            set
            {
                userData.level = value;
                if (userData.level < 1) userData.level = 1;
                SaveUser(userData);
            }
        }
        #endregion

        #region LIFE

        /// <summary>
        /// Cập nhật live dựa trên thời gian trôi qua (hồi tự động).
        /// Gọi hàm này mỗi khi mở game hoặc định kỳ trong Update().
        /// </summary>
        public void UpdateLives()
        {
            if (HasLiveInfinity) return;

            int current = userData.life.liveAmount;
            if (current >= LifeData.MAX_LIVES) return;

            long deltaTicks = DateTime.UtcNow.Ticks - userData.life.lastLifeTime;
            int gained = (int)(TimeSpan.FromTicks(deltaTicks).TotalSeconds / LifeData.REFILL_TIME);

            if (gained > 0)
            {
                int newLives = Mathf.Min(LifeData.MAX_LIVES, current + gained);
                userData.life.liveAmount = newLives;

                // cập nhật lại lastLifeTime: lấy thời điểm cộng dồn cho chính xác
                int extraSeconds = (int)(TimeSpan.FromTicks(deltaTicks).TotalSeconds % LifeData.REFILL_TIME);
                userData.life.lastLifeTime = DateTime.UtcNow.AddSeconds(-extraSeconds).Ticks;

                SaveUser(userData);
            }
        }

        /// <summary>
        /// Cộng thêm số mạng (không vượt quá MAX).
        /// </summary>
        public void AddLife(int amount)
        {
            if (amount <= 0) return;

            UpdateLives(); // đảm bảo dữ liệu mới nhất
            userData.life.liveAmount = Mathf.Min(LifeData.MAX_LIVES, userData.life.liveAmount + amount);

            if (userData.life.liveAmount >= LifeData.MAX_LIVES)
                userData.life.lastLifeTime = DateTime.UtcNow.Ticks;

            SaveUser(userData);
        }


        /// <summary>
        /// Thời gian còn lại (giây) để hồi 1 mạng.
        /// </summary>
        public int NextRefillRemainSeconds
        {
            get
            {
                if (HasLiveInfinity) return 0;
                if (userData.life.liveAmount >= LifeData.MAX_LIVES) return 0;

                long deltaTicks = DateTime.UtcNow.Ticks - userData.life.lastLifeTime;
                int passed = (int)TimeSpan.FromTicks(deltaTicks).TotalSeconds;
                int remain = LifeData.REFILL_TIME - (passed % LifeData.REFILL_TIME);

                return Mathf.Max(0, remain);
            }
        }

        /// <summary>
        /// Có Infinity Lives không.
        /// </summary>
        public bool HasLiveInfinity
        {
            get
            {
                return userData.life.liveInfinityExpire > DateTime.UtcNow.Ticks;
            }
        }

        /// <summary>
        /// Thời gian còn lại (giây) của Infinity Lives.
        /// </summary>
        public int LiveInfinityRemainingSeconds
        {
            get
            {
                if (!HasLiveInfinity) return 0;
                return (int)TimeSpan.FromTicks(userData.life.liveInfinityExpire - DateTime.UtcNow.Ticks).TotalSeconds;
            }
        }

        /// <summary>
        /// Kích hoạt Infinity Lives trong X phút.
        /// </summary>
        public void AddLiveInfinityMinutes(int minutes)
        {
            var now = DateTime.UtcNow;
            if (HasLiveInfinity)
            {
                var expireTime = new DateTime(userData.life.liveInfinityExpire, DateTimeKind.Utc);
                expireTime = expireTime.AddMinutes(minutes);
                userData.life.liveInfinityExpire = expireTime.Ticks;
            }
            else
            {
                var expireTime = now.AddMinutes(minutes);
                userData.life.liveInfinityExpire = expireTime.Ticks;
            }

            SaveUser(userData);
        }

        /// <summary>
        /// Reset Infinity Lives.
        /// </summary>
        public void ClearLiveInfinity()
        {
            userData.life.liveInfinityExpire = 0;
            SaveUser(userData);
        }

        /// <summary>
        /// Dùng 1 mạng (nếu có).
        /// </summary>
        public bool UseLife()
        {
            if (HasLiveInfinity) return true;

            int lives = userData.life.liveAmount;
            if (lives > 0)
            {
                lives--;
                userData.life.liveAmount = lives;
                userData.life.lastLifeTime = DateTime.UtcNow.Ticks;
                SaveUser(userData);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Mua refill full mạng bằng coin.
        /// </summary>
        public void RefillLives()
        {
            userData.life.liveAmount = LifeData.MAX_LIVES;
            userData.life.lastLifeTime = DateTime.UtcNow.Ticks;
            SaveUser(userData);
        }

        #endregion

        #region BOOSTER
        public BoosterData BOOSTER_DATA
        {
            get { return userData.booster; }
        }

        public int GetBoosterAmount(BoosterType boosterType)
        {
            var index = (int)boosterType;
            return BOOSTER_DATA.boosterAmounts[index];
        }

        public bool UseBooster(BoosterType boosterType)
        {
            var index = (int)boosterType;
            var amount = BOOSTER_DATA.boosterAmounts[index];
            if (amount > 0)
            {
                amount -= 1;
                BOOSTER_DATA.boosterAmounts[index] = amount;
                SaveUser(userData);
                return true;
            }
            return false;
        }

        public int AddBoosterAmount(BoosterType boosterType, int boosterAmount)
        {
            var index = (int)boosterType;
            BOOSTER_DATA.boosterAmounts[index] += boosterAmount;
            if (BOOSTER_DATA.boosterAmounts[index] < 0)
                BOOSTER_DATA.boosterAmounts[index] = 0;

            SaveUser(userData);
            return BOOSTER_DATA.boosterAmounts[index];
        }

        public bool IsUnlockedBooster(BoosterType boosterType)
        {
            var index = (int)boosterType;
            return BOOSTER_DATA.unlockeds[index];
        }

        public void UnlockBooster(BoosterType boosterType)
        {
            var index = (int)boosterType;
            BOOSTER_DATA.unlockeds[index] = true;
            SaveUser(userData);
        }
        #endregion

        #region REWARD
        public void AddReward(RewardType rewardType, int amount)
        {
            var rewardData = userData.reward.rewards.Find(x => x.rewardType == rewardType);
            if (rewardData == null)
            {
                rewardData = new RewardItemData();
                rewardData.rewardType = rewardType;
                userData.reward.rewards.Add(rewardData);
            }
            rewardData.amount += amount;
            SaveUser(userData);
        }

        public void ClaimReward(RewardItemData rewardItem)
        {
            rewardItem.isClaimed = true;
            switch (rewardItem.rewardType)
            {
                case RewardType.Coin:
                    AddCoin(rewardItem.amount);
                    break;
                case RewardType.Lives:
                    AddLife(rewardItem.amount);
                    Debug.LogWarning("livess");
                    break;
                case RewardType.LiveInfinity:
                    AddLiveInfinityMinutes(rewardItem.amount);
                    break;
                case RewardType.NextLevel:
                    LEVEL += rewardItem.amount;
                    break;
                case RewardType.Booster1:
                    AddBoosterAmount(BoosterType.Booster1, rewardItem.amount);
                    break;
                case RewardType.Booster2:
                    AddBoosterAmount(BoosterType.Booster2, rewardItem.amount);
                    break;
                case RewardType.Booster3:
                    AddBoosterAmount(BoosterType.Booster3, rewardItem.amount);
                    break;
                case RewardType.Booster4:
                    AddBoosterAmount(BoosterType.Booster4, rewardItem.amount);
                    break;
                default:
                    break;
            }
            SaveUser(userData);
        }

        public void ClaimAllReward()
        {
            foreach (var rewardItem in userData.reward.rewards)
            {
                ClaimReward(rewardItem);
            }
            userData.reward.rewards.Clear();
            SaveUser(userData);
        }

        public void SaveReward(List<RewardItemData> rewards)
        {
            userData.reward.rewards = new List<RewardItemData>(rewards);
            SaveUser(userData);
        }

        public void ClearReward()
        {
            userData.reward.rewards.Clear();
        }

        #endregion

        #region WINSTREAK
        public void AddWinStreak(int amount = 1)
        {
            userData.winStreak += amount;
            SaveUser(userData);
        }

        public void ResetWinStreak()
        {
            userData.winStreak = 0;
            SaveUser(userData);
        }

        public void AddLoseStreak(int amount = 1)
        {
            userData.loseStreak += amount;
            SaveUser(userData);
        }

        public void ResetLoseStreak()
        {
            userData.loseStreak = 0;
            SaveUser(userData);
        }



        #endregion

        #region JOURNEY
        public void JourneyClaimReward(int levelIndex)
        {
            if (!userData.journey.rewardClaimeds.Contains(levelIndex))
            {
                userData.journey.rewardClaimeds.Add(levelIndex);
                SaveUser(userData);
            }
        }

        public void JourneyClaimChapter(int levelIndex)
        {
            if (!userData.journey.chapterClaimeds.Contains(levelIndex))
            {
                userData.journey.chapterClaimeds.Add(levelIndex);
                SaveUser(userData);
            }
        }
        #endregion

        public void LoadUserProfile(UnityAction<UserData> callback)
        {
            var userProfileJson = "";
            if (!PlayerPrefs.HasKey(LOCAL_KEY))
            {
                userData = new UserData();
                userData.id = "you";
                string countryCode = "";
                string countryName = "";
                GameUtils.GetCountryName(out countryCode, out countryName);
                userData.country = countryCode;
                userData.coin = ConfigController.Instance.GameConfig.startCoin;
                userData.level = 1;
                userData.winStreak = 0;
                userData.life = new LifeData();
                userData.booster = new BoosterData();
                userData.reward = new RewardData();
                userData.journey = new JourneyData();
                userData.data = "{}";
                SaveUser(userData);
                callback?.Invoke(userData);
                return;
            }
            else userProfileJson = PlayerPrefs.GetString(LOCAL_KEY);

            try
            {
                userData = JsonUtility.FromJson<UserData>(userProfileJson);
                callback?.Invoke(userData);
            }
            catch
            {
                callback?.Invoke(null);
            }
        }

        private void SaveUser(UserData userData)
        {
            var userProfileJson = JsonUtility.ToJson(userData);
            PlayerPrefs.SetString(LOCAL_KEY, userProfileJson);
            PlayerPrefs.Save();
            OnUserChanged?.Invoke(userData);
        }

        public T GetParam<T>(string key)
        {
            try
            {
                if (userData != null && !string.IsNullOrEmpty(userData.data))
                {
                    var jData = JObject.Parse(userData.data);
                    if (jData != null)
                        if (jData.TryGetValue(key, out JToken value))
                        {
                            return value.ToObject<T>();
                        }
                }
                return default;
            }
            catch (System.Exception ex) { return default; }
        }

        public void SetParam(string key, object value)
        {
            try
            {
                var jData = userData != null && !string.IsNullOrEmpty(userData.data) ? JObject.Parse(userData.data) : new JObject();
                Debug.Log("userData.data: " + userData.data);
                jData[key] = JToken.FromObject(value);
                userData.data = jData.ToString();
                SaveUser(userData);
            }
            catch (System.Exception ex) { }
        }


        #region TEST

#if UNITY_EDITOR

        [UnityEditor.MenuItem("Test/User Data/Coin/Add 100 Coin #1")]
        private static void Add100Coin()
        {
            UserProfileController.Instance.AddCoin(100);
        }

        [UnityEditor.MenuItem("Test/User Data/Coin/Sub 100 Coin #2")]
        private static void Sub100Coin()
        {
            UserProfileController.Instance.AddCoin(-100);
        }

        [UnityEditor.MenuItem("Test/User Data/Coin/Add 10000 Coin #3")]
        private static void Add10000Coin()
        {
            UserProfileController.Instance.AddCoin(10000);
        }

        [UnityEditor.MenuItem("Test/User Data/Coin/Sub 10000 Coin #4")]
        private static void Sub10000Coin()
        {
            UserProfileController.Instance.AddCoin(-10000);
        }

        [UnityEditor.MenuItem("Test/User Data/Life/Add Live #1")]
        private static void Add1Life()
        {
            UserProfileController.Instance.AddLife(1);
        }

        [UnityEditor.MenuItem("Test/User Data/Life/Use Live #2")]
        private static void Sub1Life()
        {
            UserProfileController.Instance.UseLife();
        }

        [UnityEditor.MenuItem("Test/User Data/Life/Add 30m Infinity #3")]
        private static void Add30mLiveInifinty()
        {
            UserProfileController.Instance.AddLiveInfinityMinutes(30);
        }

        [UnityEditor.MenuItem("Test/User Data/Life/Sub 30m Infinity #4")]
        private static void Sub30mLiveInifinty()
        {
            UserProfileController.Instance.AddLiveInfinityMinutes(-30);
        }
#endif
        #endregion
    }
}
