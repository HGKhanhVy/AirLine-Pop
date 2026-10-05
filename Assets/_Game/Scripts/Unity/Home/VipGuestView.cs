using DG.Tweening;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The VIP guest who drops by the lounge after the player lands a VIP flight: a crowned
    /// cat standing in the room with a gold seal bobbing over it. It only visits,
    /// so it has no care menu of its own. Display only.
    /// </summary>
    public sealed class VipGuestView : MonoBehaviour
    {
        [SerializeField] private CatView guest;
        [SerializeField] private Transform seal;

        [Tooltip("The lounge camera, which the guest's drawing turns to face.")]
        [SerializeField] private Transform viewer;
        [SerializeField] private float bobHeight = 0.08f;
        [SerializeField, Min(0.2f)] private float bobSeconds = 0.9f;

        private Vector3 sealRest;

        private void Awake()
        {
            sealRest = seal.localPosition;
            guest.SetViewer(viewer);
        }

        private void OnDisable()
        {
            seal.DOKill();
        }

        public void SetVisiting(bool isVisiting)
        {
            gameObject.SetActive(isVisiting);
            guest.gameObject.SetActive(isVisiting);
            seal.DOKill();

            if (!isVisiting)
            {
                return;
            }

            seal.localPosition = sealRest;
            seal.DOLocalMoveY(sealRest.y + bobHeight, bobSeconds).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
        }

#if UNITY_EDITOR
        public void EditorLink(CatView linkedGuest, Transform linkedSeal, Transform linkedViewer)
        {
            viewer = linkedViewer;
            guest = linkedGuest;
            seal = linkedSeal;
        }
#endif
    }
}
