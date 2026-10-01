using ASTeams.SingleLine.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The settings row that switches the game's language. Each language is named in its own
    /// words, so a player who cannot read the current one still finds theirs.
    /// </summary>
    public sealed class LanguageRowView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text valueLabel;

        private void OnEnable()
        {
            button.onClick.AddListener(Toggle);
            Localization.Service.OnLanguageChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(Toggle);
            Localization.Service.OnLanguageChanged -= Refresh;
        }

        private static void Toggle()
        {
            Language next = Localization.Current == Language.English ? Language.Vietnamese : Language.English;
            Localization.Service.SetLanguage(next);
        }

        private void Refresh()
        {
            valueLabel.text = Localization.Current == Language.Vietnamese ? "Tiếng Việt" : "English";
        }

#if UNITY_EDITOR
        public void EditorLink(Button linkedButton, TMP_Text linkedValue)
        {
            button = linkedButton;
            valueLabel = linkedValue;
        }
#endif
    }
}
