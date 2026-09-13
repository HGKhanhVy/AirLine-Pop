using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class UIButtonEffect : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerExitHandler
{
    [SerializeField] private float pressedScale = 0.9f;
    [SerializeField] private float duration = 0.08f;

    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        transform.DOKill();

        transform
            .DOScale(originalScale * pressedScale, duration)
            .SetEase(Ease.OutQuad);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.DOKill();

        transform
            .DOScale(originalScale, duration)
            .SetEase(Ease.OutBack);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.DOKill();

        transform
            .DOScale(originalScale, duration)
            .SetEase(Ease.OutBack);
    }
}
