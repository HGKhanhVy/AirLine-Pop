using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// One postcard slot in the album: the card itself once earned, a faded card with a
    /// lock and the stamps so far while it is still being flown for. An earned card can be
    /// tapped to look at it up close; a locked one keeps its picture a surprise.
    /// </summary>
    public sealed class PostcardCellView : MonoBehaviour
    {
        [SerializeField] private Image picture;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text progressLabel;
        [SerializeField] private GameObject lockBadge;
        [SerializeField] private Button button;

        [SerializeField] private Color lockedTint = new Color(0.55f, 0.6f, 0.68f, 0.55f);

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

        public void Show(Destination destination, int stamps, int stampsNeeded, bool isCollected)
        {
            shown = destination;
            button.interactable = isCollected;
            picture.sprite = destination.Postcard;
            picture.color = isCollected ? Color.white : lockedTint;
            nameLabel.text = destination.DisplayName;
            lockBadge.SetActive(!isCollected);
            progressLabel.gameObject.SetActive(!isCollected);

            if (!isCollected)
            {
                progressLabel.text = Localization.Format("album.stamps", stamps, stampsNeeded);
            }
        }

        private void HandleClick()
        {
            OnOpened?.Invoke(shown);
        }

#if UNITY_EDITOR
        public void EditorLink(Image linkedPicture, TMP_Text linkedName, TMP_Text linkedProgress, GameObject linkedLock, Button linkedButton)
        {
            button = linkedButton;
            picture = linkedPicture;
            nameLabel = linkedName;
            progressLabel = linkedProgress;
            lockBadge = linkedLock;
        }
#endif
    }
}
