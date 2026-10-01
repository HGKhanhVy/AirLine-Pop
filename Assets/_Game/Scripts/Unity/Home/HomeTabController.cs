using System;
using DG.Tweening;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Switches Home between its tabs: glides the camera to the tab's view of the one 3D
    /// world and cross-fades the tab's panel. Airport and cat room live in the same scene,
    /// so switching never loads anything (GDD 9: Home, room and shop always lead back).
    /// </summary>
    public sealed class HomeTabController : MonoBehaviour
    {
        [SerializeField] private Transform cameraRig;
        [SerializeField] private BottomNavView nav;
        [SerializeField] private HomeTabStage[] stages = new HomeTabStage[0];
        [SerializeField, Min(0.01f)] private float moveDuration = 0.6f;
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.2f;

        public HomeTab Current { get; private set; }

        public event Action<HomeTab> OnTabChanged;

        private void OnEnable()
        {
            nav.OnTabSelected += Show;
        }

        private void OnDisable()
        {
            nav.OnTabSelected -= Show;
        }

        private void OnDestroy()
        {
            cameraRig.DOKill();

            for (int i = 0; i < stages.Length; i++)
            {
                stages[i].Panel.DOKill();
            }
        }

        public void ShowInstant(HomeTab tab)
        {
            Apply(tab, true);
        }

        public void Show(HomeTab tab)
        {
            if (tab == Current)
            {
                return;
            }

            Apply(tab, false);
        }

        private void Apply(HomeTab tab, bool isInstant)
        {
            Current = tab;
            nav.Show(tab, isInstant);

            for (int i = 0; i < stages.Length; i++)
            {
                bool isTarget = stages[i].Tab == tab;
                FadePanel(stages[i].Panel, isTarget, isInstant);

                if (isTarget && stages[i].CameraAnchor != null)
                {
                    MoveCamera(stages[i].CameraAnchor, isInstant);
                }
            }

            OnTabChanged?.Invoke(tab);
        }

        private void MoveCamera(Transform anchor, bool isInstant)
        {
            cameraRig.DOKill();

            if (isInstant)
            {
                cameraRig.SetPositionAndRotation(anchor.position, anchor.rotation);
                return;
            }

            cameraRig.DOMove(anchor.position, moveDuration).SetEase(Ease.InOutCubic);
            cameraRig.DORotateQuaternion(anchor.rotation, moveDuration).SetEase(Ease.InOutCubic);
        }

        private void FadePanel(CanvasGroup panel, bool isVisible, bool isInstant)
        {
            panel.DOKill();
            panel.interactable = isVisible;
            panel.blocksRaycasts = isVisible;
            float alpha = isVisible ? 1f : 0f;

            if (isInstant)
            {
                panel.alpha = alpha;
                return;
            }

            panel.DOFade(alpha, fadeDuration).SetUpdate(true);
        }

#if UNITY_EDITOR
        public void EditorLink(Transform linkedRig, BottomNavView linkedNav, HomeTabStage[] linkedStages)
        {
            cameraRig = linkedRig;
            nav = linkedNav;
            stages = linkedStages;
        }
#endif
    }
}
