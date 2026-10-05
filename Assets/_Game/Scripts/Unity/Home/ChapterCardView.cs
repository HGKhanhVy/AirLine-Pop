using TMPro;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The title of a chapter on the route map: "Chapter 2 · Japan", over the start of the
    /// international leg that leads into that country.
    /// </summary>
    public sealed class ChapterCardView : MonoBehaviour
    {
        [SerializeField] private RectTransform rect;
        [SerializeField] private TMP_Text chapterLabel;
        [SerializeField] private TMP_Text countryLabel;
        [SerializeField] private TMP_Text subtitleLabel;

        public int Chapter { get; private set; }

        private void OnDisable()
        {
            Chapter = 0;
        }

        public void Show(int chapter, PassportPage page, Vector2 position)
        {
            rect.anchoredPosition = position;

            if (chapter == Chapter)
            {
                return;
            }

            Chapter = chapter;
            chapterLabel.text = Localization.Format("map.chapter", chapter);
            countryLabel.text = page != null ? page.DisplayName : string.Empty;
            subtitleLabel.gameObject.SetActive(chapter > 1);
        }

        public void Invalidate()
        {
            Chapter = 0;
        }

#if UNITY_EDITOR
        public void EditorLink(RectTransform linkedRect, TMP_Text linkedChapter, TMP_Text linkedCountry, TMP_Text linkedSubtitle)
        {
            rect = linkedRect;
            chapterLabel = linkedChapter;
            countryLabel = linkedCountry;
            subtitleLabel = linkedSubtitle;
        }
#endif
    }
}
