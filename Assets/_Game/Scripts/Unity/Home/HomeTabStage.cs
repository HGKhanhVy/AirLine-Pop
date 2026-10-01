using System;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>Where the camera goes and which panel shows for one Home tab.</summary>
    [Serializable]
    public struct HomeTabStage
    {
        [SerializeField] private HomeTab tab;
        [SerializeField] private Transform cameraAnchor;
        [SerializeField] private CanvasGroup panel;

        public HomeTab Tab => tab;

        public Transform CameraAnchor => cameraAnchor;

        public CanvasGroup Panel => panel;

#if UNITY_EDITOR
        public HomeTabStage(HomeTab tab, Transform cameraAnchor, CanvasGroup panel)
        {
            this.tab = tab;
            this.cameraAnchor = cameraAnchor;
            this.panel = panel;
        }
#endif
    }
}
