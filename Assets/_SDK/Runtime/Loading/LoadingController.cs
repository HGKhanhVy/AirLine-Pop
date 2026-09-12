using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using ASTeams.Base.Analytics;
using ASTeams.Base.Data;
using ASTeams.Base.RemoteConfigs;
using ASTeams.Base.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.Base
{
    public class LoadingController : MonoBehaviour
    {
        [SerializeField] private Image progressBar;
        [SerializeField] private float duration = 1f;

        private void Start()
        {
            GameManager gameManager = GameManager.Instance;
            gameManager.OnInited.AddListener(OnInited);
            progressBar.fillAmount = 0;
            progressBar.DOFillAmount(1, duration);

            if (gameManager.IsInitialized)
            {
                OnInited();
            }
        }

        private void OnDestroy()
        {
            GameManager.Instance.OnInited.RemoveListener(OnInited);
        }

        private async void OnInited()
        {
            await UniTask.DelayFrame(1);
            bool firstTime = UserProfileController.Instance.GetParam<bool>("firstTime");
            int currentLevel = UserProfileController.Instance.GetParam<int>("currentLevel");
            if (!firstTime)
            {
                UserProfileController.Instance.SetParam("firstTime", true);
                UserProfileController.Instance.SetParam("currentLevel", 1);
                UserProfileController.Instance.SetParam("timeToAddLife", ConfigController.Instance.GameConfig.refillLifeTime);
                UserProfileController.Instance.SetParam("coin", ConfigController.Instance.GameConfig.startCoin);
                UserProfileController.Instance.SetParam("life", ConfigController.Instance.GameConfig.maxLife);
                UserProfileController.Instance.SetParam("adsTicket", 0);

                UserProfileController.Instance.SetParam("booster_1", 3);
                UserProfileController.Instance.SetParam("booster_2", 3);
                UserProfileController.Instance.SetParam("booster_3", 3);

                UserProfileController.Instance.SetParam("booster_1_tutorial", false);
                UserProfileController.Instance.SetParam("booster_2_tutorial", false);
                UserProfileController.Instance.SetParam("booster_3_tutorial", false);

                UserProfileController.Instance.SetParam("winStreak", 0);
                UserProfileController.Instance.SetParam("openedWinStreakCount", 0);

                await UniTask.DelayFrame(1);
            }

            GameDataHelper.PlayType = "loading";
            UISceneController.Instance.ChangeScene(SceneName.Gameplay);
        }
    }

}
