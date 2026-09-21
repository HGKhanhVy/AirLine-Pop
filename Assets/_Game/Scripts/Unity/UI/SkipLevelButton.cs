using ASTeams.Base.Ads;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Skip: watch a rewarded ad, then pass over the current level. The button is locked
    /// while the ad runs so a second tap cannot queue a second ad.
    /// </summary>
    public sealed class SkipLevelButton : MonoBehaviour
    {
        private const string Placement = "skip_level";

        [SerializeField] private Button button;

        private IRewardedAdService rewardedAds;
        private bool isWaiting;

        private void Reset()
        {
            button = GetComponent<Button>();
        }

        private void Awake()
        {
            rewardedAds = new SdkRewardedAdService(AdsController.Instance);
        }

        private void OnEnable()
        {
            button.onClick.AddListener(HandleClick);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(HandleClick);
        }

        private void HandleClick()
        {
            if (isWaiting)
            {
                return;
            }

            isWaiting = true;
            button.interactable = false;
            rewardedAds.Show(Placement, HandleAdFinished);
        }

        private void HandleAdFinished(bool isRewarded)
        {
            isWaiting = false;

            if (button != null)
            {
                button.interactable = true;
            }

            if (isRewarded)
            {
                GameplayEvents.RequestSkipLevel();
            }
        }
    }
}
