using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The reward card after a win (GDD 3, 9): the companion cat, the coins actually
    /// earned counting up, Continue as the main button and Back to airport as the second.
    /// A replay says plainly that there is no first-clear reward.
    /// </summary>
    public sealed class WinPanelView : MonoBehaviour
    {
        [SerializeField] private ModalPanel modal;
        [SerializeField] private RawImage catPortrait;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private GameObject rewardRow;
        [SerializeField] private TMP_Text rewardLabel;
        [SerializeField] private TMP_Text replayNote;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button homeButton;
        [Tooltip("Optional. Takes the portrait's place on the flight that completes a postcard.")]
        [SerializeField] private Image postcardImage;
        [SerializeField, Min(0.01f)] private float countDuration = 0.6f;

        private Tweener count;
        private int shown;

        public event Action OnContinue;
        public event Action OnHome;

        private void Awake()
        {
            // The title can run to two lines now; it shrinks to fit rather than spill
            // onto the reward row beneath it.
            titleLabel.fontSizeMax = titleLabel.fontSize;
            titleLabel.fontSizeMin = titleLabel.fontSize * 0.6f;
            titleLabel.enableAutoSizing = true;
        }

        private void OnEnable()
        {
            continueButton.onClick.AddListener(HandleContinue);
            homeButton.onClick.AddListener(HandleHome);
        }

        private void OnDisable()
        {
            continueButton.onClick.RemoveListener(HandleContinue);
            homeButton.onClick.RemoveListener(HandleHome);
            count?.Kill();
        }

        public void SetPortrait(Texture texture)
        {
            catPortrait.texture = texture;
            catPortrait.enabled = texture != null;
        }

        public void Show(in WinFlightSummary flight)
        {
            titleLabel.text = Title(flight);
            ShowPostcard(flight.EarnedPostcard);
            int coins = flight.Coins;
            bool hasReward = coins > 0;
            rewardRow.SetActive(hasReward);
            replayNote.gameObject.SetActive(!hasReward);
            SetButtonsInteractable(true);
            modal.Show();

            shown = 0;
            rewardLabel.SetText("+0");
            count?.Kill();

            if (hasReward)
            {
                count = DOTween.To(() => shown, SetShown, coins, countDuration)
                    .SetDelay(0.25f).SetEase(Ease.OutCubic).SetUpdate(true);
            }
        }

        public void Hide()
        {
            count?.Kill();
            modal.Hide();
        }

        private string Title(in WinFlightSummary flight)
        {
            if (flight.IsVip)
            {
                string detail = flight.EarnedPostcard != null && !string.IsNullOrEmpty(flight.Destination)
                    ? Localization.Format("win.postcard", flight.Destination)
                    : Localization.Get("win.vipDetail");
                return Localization.Format("win.vipTitle", flight.FlightNumber) + Detail(detail);
            }

            if (string.IsNullOrEmpty(flight.Destination))
            {
                return Localization.Format("win.title", flight.FlightNumber);
            }

            string title = Localization.Format("win.titleTo", flight.FlightNumber, flight.Destination);

            if (flight.EarnedPostcard != null)
            {
                return title + Detail(Localization.Format("win.postcard", flight.Destination));
            }

            return title + Detail(Localization.Format("win.detail", flight.Passengers, flight.Stamp, flight.StampsNeeded));
        }

        /// <summary>The smaller second line under the title.</summary>
        private static string Detail(string line)
        {
            return "\n<size=58%>" + line + "</size>";
        }

        /// <summary>The postcard replaces the cat on the card for the flight that earns it.</summary>
        private void ShowPostcard(Sprite postcard)
        {
            bool hasPostcard = postcard != null && postcardImage != null;

            if (postcardImage != null)
            {
                postcardImage.sprite = postcard;
                postcardImage.enabled = hasPostcard;
            }

            catPortrait.gameObject.SetActive(!hasPostcard);
        }

        private void SetShown(int value)
        {
            shown = value;
            rewardLabel.SetText("+{0}", value);
        }

        private void SetButtonsInteractable(bool isInteractable)
        {
            continueButton.interactable = isInteractable;
            homeButton.interactable = isInteractable;
        }

        private void HandleContinue()
        {
            SetButtonsInteractable(false);
            OnContinue?.Invoke();
        }

        private void HandleHome()
        {
            SetButtonsInteractable(false);
            OnHome?.Invoke();
        }

#if UNITY_EDITOR
        public void EditorLink(ModalPanel linkedModal, RawImage portrait, TMP_Text title, GameObject row,
            TMP_Text reward, TMP_Text note, Button next, Button home)
        {
            modal = linkedModal;
            catPortrait = portrait;
            titleLabel = title;
            rewardRow = row;
            rewardLabel = reward;
            replayNote = note;
            continueButton = next;
            homeButton = home;
        }

        public void EditorLinkPostcard(Image linkedPostcard)
        {
            postcardImage = linkedPostcard;
        }
#endif
    }
}
