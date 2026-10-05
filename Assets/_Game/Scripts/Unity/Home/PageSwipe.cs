using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>Turns a horizontal swipe across the book into a page turn: right to left turns on.</summary>
    public sealed class PageSwipe : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField, Min(10f)] private float minimumDistance = 80f;

        private Vector2 start;

        public event Action<int> OnSwiped;

        public void OnBeginDrag(PointerEventData eventData)
        {
            start = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Vector2 delta = eventData.position - start;

            if (Mathf.Abs(delta.x) >= minimumDistance && Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                OnSwiped?.Invoke(delta.x < 0f ? 1 : -1);
            }
        }
    }
}
