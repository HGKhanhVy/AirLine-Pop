using ASTeams.Base;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Base for every panel that pops over the game: scales and fades in on Show, reverses on
/// Hide. Input is blocked while the panel is animating so a half-open popup cannot be
/// clicked through or tapped twice.
/// </summary>
public abstract class Uibase : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] protected CanvasGroup canvasGroup;
    [SerializeField] protected RectTransform panel;

    [SerializeField, Min(0f)] private float animDuration = 0.3f;
    [SerializeField, Range(0f, 1f)] private float hiddenScale = 0.7f;
    [SerializeField] private bool playsSound = true;

    private Sequence currentSequence;

    public bool IsShown { get; private set; }

    private void Reset()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        panel = GetComponent<RectTransform>();
    }

    protected virtual void Awake()
    {
        // Fallback for panels placed before the references were serialized; runs once.
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (panel == null)
        {
            panel = GetComponent<RectTransform>();
        }
    }

    public virtual void Show()
    {
        currentSequence?.Kill();
        IsShown = true;

        gameObject.SetActive(true);
        SetInteractable(false);

        canvasGroup.alpha = 0f;
        panel.localScale = Vector3.one * hiddenScale;

        if (playsSound)
        {
            AudioController.Instance?.PlaySound(SoundName.UI_PopupOpen);
        }

        currentSequence = DOTween.Sequence()
            .Join(panel.DOScale(Vector3.one, animDuration).SetEase(Ease.OutBack))
            .Join(canvasGroup.DOFade(1f, animDuration).SetEase(Ease.OutQuad))
            .SetUpdate(true)
            .OnComplete(() => SetInteractable(true));
    }

    public virtual void Hide()
    {
        if (!IsShown && !gameObject.activeSelf)
        {
            return;
        }

        currentSequence?.Kill();
        IsShown = false;
        SetInteractable(false);

        if (playsSound)
        {
            AudioController.Instance?.PlaySound(SoundName.UI_PopupClose);
        }

        currentSequence = DOTween.Sequence()
            .Join(panel.DOScale(Vector3.one * hiddenScale, animDuration).SetEase(Ease.InBack))
            .Join(canvasGroup.DOFade(0f, animDuration * 0.7f))
            .SetUpdate(true)
            .OnComplete(OnHidden);
    }

    private void OnHidden()
    {
        gameObject.SetActive(false);
        panel.localScale = Vector3.one;
        canvasGroup.alpha = 1f;
        SetInteractable(true);
    }

    private void SetInteractable(bool isInteractable)
    {
        canvasGroup.interactable = isInteractable;
        canvasGroup.blocksRaycasts = isInteractable;
    }

    protected virtual void OnDestroy()
    {
        currentSequence?.Kill();
    }
}
