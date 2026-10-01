using TMPro;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>A label whose fixed text follows the chosen language, such as a button's name.</summary>
    public sealed class LocalizedLabel : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private string key;

        private void OnEnable()
        {
            Localization.Service.OnLanguageChanged += Apply;
            Apply();
        }

        private void OnDisable()
        {
            Localization.Service.OnLanguageChanged -= Apply;
        }

        private void Apply()
        {
            label.text = Localization.Get(key);
        }

#if UNITY_EDITOR
        public void EditorLink(TMP_Text linkedLabel, string linkedKey)
        {
            label = linkedLabel;
            key = linkedKey;
        }
#endif
    }
}
