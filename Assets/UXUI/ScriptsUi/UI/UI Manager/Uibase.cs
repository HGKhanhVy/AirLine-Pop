using DG.Tweening;
using UnityEngine;

public abstract class Uibase : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] protected CanvasGroup canvasGroup;
    [SerializeField] protected RectTransform panel;

    [SerializeField] private float animDuration = 0.3f;

    private Sequence currentSequence;

    protected virtual void Awake()
    {
        // Tự tìm CanvasGroup trên UI
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        // Nếu không gán Panel thì dùng chính UI
        if (panel == null)
        {
            panel = GetComponent<RectTransform>();
        }
    }

    public virtual void Show()
    {
        // Hủy animation cũ nếu đang chạy
        currentSequence?.Kill();

        // Bật UI
        gameObject.SetActive(true);

        // Trạng thái ban đầu
        canvasGroup.alpha = 0f;
        panel.localScale = Vector3.one * 0.7f;

        // Tạo animation
        currentSequence = DOTween.Sequence();

        // Scale 0.7 -> 1
        currentSequence.Join(
            panel
                .DOScale(Vector3.one, animDuration)
                .SetEase(Ease.OutBack)
        );

        // Fade 0 -> 1
        currentSequence.Join(
            canvasGroup
                .DOFade(1f, animDuration)
                .SetEase(Ease.OutQuad)
        );
    }

    public virtual void Hide()
    {
        // Hủy animation cũ
        currentSequence?.Kill();

        currentSequence = DOTween.Sequence();

        // Scale 1 -> 0.7
        currentSequence.Join(
            panel
                .DOScale(Vector3.one * 0.7f, animDuration)
                .SetEase(Ease.InBack)
        );

        // Fade 1 -> 0
        currentSequence.Join(
            canvasGroup
                .DOFade(0f, animDuration * 0.7f)
        );

        currentSequence.OnComplete(() =>
        {
            gameObject.SetActive(false);

            // Reset lại trạng thái
            panel.localScale = Vector3.one;
            canvasGroup.alpha = 1f;
        });
    }

    protected virtual void OnDestroy()
    {
        currentSequence?.Kill();
    }
}