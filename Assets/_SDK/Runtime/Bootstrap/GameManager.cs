using Cysharp.Threading.Tasks;
using ASTeams.Base.Ads;
using ASTeams.Base.Analytics;
using ASTeams.Base.Data;
using ASTeams.Base.RemoteConfigs;
using ASTeams.Base.UI;
using ASTeams.Base.IAP;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace ASTeams.Base
{
    public class GameManager : MonoSingleton<GameManager>
    {
        public UnityEvent OnInited = new UnityEvent();

        protected override void Awake()
        {
            base.Awake();
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep; //không tắt màn hình
        }

        private bool isInited;

        public bool IsInitialized => isInited;

        public override void Init()
        {
            base.Init();
            SyncInit().Forget();
        }

        // ===============================
        // MAIN INIT FLOW (NO FREEZE)
        // ===============================
        private async UniTask SyncInit()
        {
            await UniTask.DelayFrame(1);
            // Load local user profile
            UserProfileController.Instance.LoadUserProfile(null);


#if REMOTE_CONFIGS
            await UniTask.Delay(100);
            await InitRemoteConfigWithTimeout();
#endif

#if ADS
            await UniTask.Delay(100);
#if UMP
            await InitUMPWithTimeout();
#else
            AdsController.Instance.InitAds();
#endif

#endif

#if ANALYTICS

            // Tracking open app
            await UniTask.Delay(100);
            AnalyticsController.Instance.LogAppOpen();
#endif

#if IAP
            // IAP
            await UniTask.Delay(100);
            await InitIAPWithTimeout();
#endif

            isInited = true;
            OnInited?.Invoke();
        }

        // ===============================
        // INTERNET CHECK
        // ===============================
        private async UniTask WaitForInternet()
        {
            bool showed = false;

            while (!IsConnectedToInternet())
            {
                if (!showed)
                {
                    showed = true;
                    UIMessageController.Instance.ShowMessage(
                        "Connect to internet",
                        () => showed = false,
                        null);
                }

                await UniTask.Delay(2000);
            }

            if (showed)
                UIMessageController.Instance.HideMessage();
        }

        private bool IsConnectedToInternet()
        {
            return Application.internetReachability != NetworkReachability.NotReachable;
        }

        // ===============================
        // UMP + ADS (SAFE INIT)
        // ===============================
        private async UniTask InitUMPWithTimeout()
        {
#if UMP
            Debug.Log("Init UMP");
            UMPManager.Instance.InitUMP(
                () =>
                {
                    Time.timeScale = 1;
                    AdsController.Instance.InitAds();  
                },
                () =>
                {
                    Time.timeScale = 0;
                },
                () =>
                {
                    Time.timeScale = 1;
                }
            );
#endif
        }


        // ===============================
        // REMOTE CONFIGS (SAFE INIT)
        // ===============================
        private async UniTask InitRemoteConfigWithTimeout()
        {
#if REMOTE_CONFIGS
            Debug.Log("Init Remote Config");
            await RemoteConfigsController.Instance.InitializeFirebase();
#endif
        }

        // ===============================
        // IAP (SAFE INIT)
        // ===============================
        private async UniTask InitIAPWithTimeout()
        {
#if IAP
            Debug.Log("Init IAP");
            IAPController.Instance.InitializePurchasing();
#endif
        }

        // ===============================
        // NETWORK POPUP LOOP
        // ===============================
        private float timeCheck;
        private const float DelayCheckNetwork = 3f;
        bool isNoInternetShowed = false;

        private void Update()
        {
            if (!isInited)
                return;

            //timeCheck += Time.deltaTime;

            //if (timeCheck >= DelayCheckNetwork)
            //{
            //    timeCheck = 0;

            //    if (!IsConnectedToInternet() && !isNoInternetShowed)
            //    {
            //        isNoInternetShowed = true;
            //        UIMessageManager.Instance.ShowMessage(
            //            "Connect to internet",
            //            () =>
            //            {
            //                isNoInternetShowed = false;
            //                timeCheck = 0;
            //            },
            //            null);
            //    }
            //}
        }
    }
}
