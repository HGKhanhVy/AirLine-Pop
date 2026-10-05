using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>The popup card that explains one level rule: its picture, a title, a line of text and a button.</summary>
    public sealed class RuleIntroView : MonoBehaviour
    {
        [SerializeField] private ModalPanel modal;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text body;
        [SerializeField] private Button gotIt;

        public event Action OnClosed;

        public bool IsShown { get; private set; }

        private void OnEnable()
        {
            gotIt.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            gotIt.onClick.RemoveListener(Close);
        }

        public void Show(RuleIntroEntry entry)
        {
            icon.sprite = entry.icon;
            title.text = Localization.Get(entry.titleKey);
            body.text = Localization.Get(entry.bodyKey);
            IsShown = true;
            modal.Show();
        }

        private void Close()
        {
            IsShown = false;
            modal.Hide();
            OnClosed?.Invoke();
        }

#if UNITY_EDITOR
        public void EditorLink(ModalPanel linkedModal, Image linkedIcon, TMP_Text linkedTitle, TMP_Text linkedBody, Button linkedButton)
        {
            modal = linkedModal;
            icon = linkedIcon;
            title = linkedTitle;
            body = linkedBody;
            gotIt = linkedButton;
        }
#endif
    }
}
