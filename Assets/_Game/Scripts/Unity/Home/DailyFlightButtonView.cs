using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AirlinePop.SingleLine.Unity
{
    /// <summary>
    /// The daily flight's button on the airport: what today's flight pays and how many days
    /// in a row it has been flown, or, once flown, that it is done until tomorrow.
    /// </summary>
    public sealed class DailyFlightButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text rewardLabel;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private GameObject doneMark;

        public event Action OnPressed;

        private void OnEnable()
        {
            button.onClick.AddListener(HandleClick);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(HandleClick);
        }

        public void SetVisible(bool isVisible)
        {
            gameObject.SetActive(isVisible);
        }

        public void ShowAvailable(int coins, int dayStreak)
        {
            button.interactable = true;
            doneMark.SetActive(false);
            rewardLabel.gameObject.SetActive(true);
            rewardLabel.text = Localization.Format("daily.reward", coins);
            statusLabel.text = dayStreak > 0 ? Localization.Format("daily.streak", dayStreak) : Localization.Get("daily.title");
        }

        public void ShowDone(int dayStreak)
        {
            button.interactable = false;
            doneMark.SetActive(true);
            rewardLabel.gameObject.SetActive(false);
            statusLabel.text = Localization.Get("daily.done");
        }

        private void HandleClick()
        {
            OnPressed?.Invoke();
        }

#if UNITY_EDITOR
        public void EditorLink(Button linkedButton, TMP_Text linkedReward, TMP_Text linkedStatus, GameObject linkedDone)
        {
            button = linkedButton;
            rewardLabel = linkedReward;
            statusLabel = linkedStatus;
            doneMark = linkedDone;
        }
#endif
    }
}
