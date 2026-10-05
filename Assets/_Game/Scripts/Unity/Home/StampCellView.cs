using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// One city's stamp in the collection. Collected, it shows in full colour with the
    /// postmark across it and opens the city's postcard when tapped; still to fly for, its
    /// picture stays visible but paler, with a small padlock and the flights so far, so the
    /// player can see what there is to collect.
    /// </summary>
    public sealed class StampCellView : MonoBehaviour
    {
        [SerializeField] private Image stamp;
        [SerializeField] private GameObject postmark;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text progressLabel;
        [SerializeField] private GameObject lockBadge;
        [SerializeField] private Button button;

        [Tooltip("The gold seal pressed on the stamp once the city's VIP flight is landed.")]
        [SerializeField] private GameObject vipSeal;

        [SerializeField] private Color lockedTint = new Color(0.86f, 0.87f, 0.9f, 0.85f);

        private Destination shown;

        public event Action<Destination> OnOpened;

        private void OnEnable()
        {
            button.onClick.AddListener(HandleClick);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(HandleClick);
        }

        public void Show(Destination destination, int flown, int flightsNeeded, bool isCollected, bool hasVipSeal)
        {
            vipSeal.SetActive(hasVipSeal);
            shown = destination;
            button.interactable = isCollected;
            stamp.sprite = destination.Stamp;
            stamp.color = isCollected ? Color.white : lockedTint;
            postmark.SetActive(isCollected);
            lockBadge.SetActive(!isCollected);
            nameLabel.text = destination.DisplayName;
            progressLabel.gameObject.SetActive(!isCollected);

            if (!isCollected)
            {
                progressLabel.SetText(Localization.Get("album.stamps"), flown, flightsNeeded);
            }
        }

        private void HandleClick()
        {
            OnOpened?.Invoke(shown);
        }

#if UNITY_EDITOR
        public void EditorLink(Image linkedStamp, GameObject linkedPostmark, TMP_Text linkedName, TMP_Text linkedProgress,
            GameObject linkedLock, Button linkedButton, GameObject linkedVipSeal)
        {
            vipSeal = linkedVipSeal;
            stamp = linkedStamp;
            postmark = linkedPostmark;
            nameLabel = linkedName;
            progressLabel = linkedProgress;
            lockBadge = linkedLock;
            button = linkedButton;
        }
#endif
    }
}
