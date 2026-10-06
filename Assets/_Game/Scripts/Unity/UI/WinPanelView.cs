using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The reward card after a win (GDD 3, 9): the cat that belongs to the moment, what the
    /// flight was, and the coins it earned counting up, always under the words: +0 on a
    /// replay, which says plainly beneath that the ticket was already paid. It has no
    /// buttons: the win sequence takes it away and opens the next flight a moment later.
    /// Display only.
    /// </summary>
    public sealed class WinPanelView : MonoBehaviour
    {
        [SerializeField] private ModalPanel modal;
        [SerializeField] private RawImage catPortrait;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private GameObject rewardRow;
        [SerializeField] private TMP_Text rewardLabel;
        [SerializeField] private TMP_Text replayNote;

        [Tooltip("Optional. Takes the portrait's place on the flight that completes a postcard.")]
        [SerializeField] private Image postcardImage;
        [SerializeField, Min(0.01f)] private float countDuration = 0.6f;

        private Tweener count;
        private int shown;

        /// <summary>The card has come in, for the moment it marks.</summary>
        public event Action<WinMilestone> OnShown;

        /// <summary>The reward count has climbed.</summary>
        public event Action OnCoinsCounted;

        private void Awake()
        {
            // The title can run to two lines now; it shrinks to fit rather than spill
            // onto the reward row beneath it.
            titleLabel.fontSizeMax = titleLabel.fontSize;
            titleLabel.fontSizeMin = titleLabel.fontSize * 0.6f;
            titleLabel.enableAutoSizing = true;
        }

        private void OnDisable()
        {
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
            rewardRow.SetActive(true);
            replayNote.gameObject.SetActive(!hasReward);
            modal.Show();
            OnShown?.Invoke(flight.Milestone);

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
            switch (flight.Milestone)
            {
                case WinMilestone.Vip:
                    return Localization.Format("win.vipTitle", flight.FlightNumber) + Detail(PostcardOr(flight, "win.vipDetail"));

                case WinMilestone.Arrival:
                    return Localization.Format("win.arrivalTitle", flight.ArrivalName) + Detail(PostcardOr(flight, "win.arrivalDetail"));

                case WinMilestone.ChapterEnd:
                    int chapter = (flight.FlightNumber - 1) / ASTeams.SingleLine.Data.CampaignLevelAddress.LevelsPerChapter + 1;
                    return Localization.Format("win.chapterTitle", chapter) + Detail(PostcardOr(flight, "win.chapterDetail"));
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

        /// <summary>The postcard line when this flight also earned one, which matters more; otherwise the milestone's own.</summary>
        private static string PostcardOr(in WinFlightSummary flight, string key)
        {
            return flight.EarnedPostcard != null && !string.IsNullOrEmpty(flight.Destination)
                ? Localization.Format("win.postcard", flight.Destination)
                : Localization.Get(key);
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
            OnCoinsCounted?.Invoke();
        }

#if UNITY_EDITOR
        public void EditorLink(ModalPanel linkedModal, RawImage portrait, TMP_Text title, GameObject row,
            TMP_Text reward, TMP_Text note)
        {
            modal = linkedModal;
            catPortrait = portrait;
            titleLabel = title;
            rewardRow = row;
            rewardLabel = reward;
            replayNote = note;
        }

        public void EditorLinkPostcard(Image linkedPostcard)
        {
            postcardImage = linkedPostcard;
        }
#endif
    }
}
