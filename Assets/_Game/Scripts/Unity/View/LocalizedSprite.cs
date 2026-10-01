using ASTeams.SingleLine.Core;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// A piece of art with words painted on it, such as the departures board or a gate sign:
    /// one drawing per language, swapped when the language changes.
    /// </summary>
    public sealed class LocalizedSprite : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer target;
        [SerializeField] private Sprite english;
        [SerializeField] private Sprite vietnamese;

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
            target.sprite = Localization.Current == Language.Vietnamese && vietnamese != null ? vietnamese : english;
        }

#if UNITY_EDITOR
        public void EditorLink(SpriteRenderer linkedTarget, Sprite linkedEnglish, Sprite linkedVietnamese)
        {
            target = linkedTarget;
            english = linkedEnglish;
            vietnamese = linkedVietnamese;
        }
#endif
    }
}
