using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// One earned postcard shown large over the album; the card names its own city. A tap
    /// anywhere puts it back.
    /// </summary>
    public sealed class PostcardViewerView : MonoBehaviour
    {
        [SerializeField] private ModalPanel modal;
        [SerializeField] private Image picture;
        [SerializeField] private Button closeButton;

        private void OnEnable()
        {
            closeButton.onClick.AddListener(Hide);
        }

        private void OnDisable()
        {
            closeButton.onClick.RemoveListener(Hide);
        }

        public void Show(Destination destination)
        {
            picture.sprite = destination.Postcard;
            modal.Show();
        }

        public void Hide()
        {
            modal.Hide();
        }

#if UNITY_EDITOR
        public void EditorLink(ModalPanel linkedModal, Image linkedPicture, Button linkedClose)
        {
            modal = linkedModal;
            picture = linkedPicture;
            closeButton = linkedClose;
        }
#endif
    }
}
