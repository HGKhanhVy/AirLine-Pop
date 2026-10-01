using System;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>The Home bottom bar (GDD 9: Home / Cats / Shop). Shows a selection; does not act on it.</summary>
    public sealed class BottomNavView : MonoBehaviour
    {
        [SerializeField] private NavTabButton[] tabs = new NavTabButton[0];

        public event Action<HomeTab> OnTabSelected;

        private void OnEnable()
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                tabs[i].OnClicked += HandleClicked;
            }
        }

        private void OnDisable()
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                tabs[i].OnClicked -= HandleClicked;
            }
        }

        public void Show(HomeTab selected, bool isInstant)
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                tabs[i].SetSelected(tabs[i].Tab == selected, isInstant);
            }
        }

        private void HandleClicked(HomeTab tab)
        {
            OnTabSelected?.Invoke(tab);
        }

#if UNITY_EDITOR
        public void EditorLink(NavTabButton[] linkedTabs)
        {
            tabs = linkedTabs;
        }
#endif
    }
}
