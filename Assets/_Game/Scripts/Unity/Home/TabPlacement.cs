using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Moves a shared piece of Home UI to another spot while one tab is showing, such as the
    /// Settings and passport buttons stepping down a row on the route map to make room for
    /// the airline's header.
    /// </summary>
    public sealed class TabPlacement : MonoBehaviour
    {
        [SerializeField] private HomeTabController tabs;
        [SerializeField] private RectTransform rect;
        [SerializeField] private HomeTab tab;
        [SerializeField] private Vector2 usualPosition;
        [SerializeField] private Vector2 tabPosition;

        private void OnEnable()
        {
            tabs.OnTabChanged += Apply;
            Apply(tabs.Current);
        }

        private void OnDisable()
        {
            tabs.OnTabChanged -= Apply;
        }

        private void Apply(HomeTab current)
        {
            rect.anchoredPosition = current == tab ? tabPosition : usualPosition;
        }

#if UNITY_EDITOR
        public void EditorLink(HomeTabController linkedTabs, RectTransform linkedRect, HomeTab linkedTab, Vector2 linkedTabPosition)
        {
            tabs = linkedTabs;
            rect = linkedRect;
            tab = linkedTab;
            usualPosition = linkedRect.anchoredPosition;
            tabPosition = linkedTabPosition;
        }
#endif
    }
}
