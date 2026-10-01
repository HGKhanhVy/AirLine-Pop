using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>Squashes a button while held and springs it back on release.</summary>
    public sealed class ButtonPressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float pressedScale = 0.92f;
        [SerializeField, Min(0.01f)] private float duration = 0.08f;

        private Vector3 restScale = Vector3.one;

        private void Awake()
        {
            restScale = transform.localScale;
        }

        private void OnDisable()
        {
            transform.DOKill();
            transform.localScale = restScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            transform.DOKill();
            transform.DOScale(restScale * pressedScale, duration).SetUpdate(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            transform.DOKill();
            transform.DOScale(restScale, duration * 2.5f).SetEase(Ease.OutBack).SetUpdate(true);
        }
    }
}
