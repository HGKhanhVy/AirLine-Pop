using ASTeams.Base;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Press feedback shared by every button: shrink on touch, spring back on release, and a
/// click sound plus a light haptic only when the click actually lands. A main call to
/// action can also breathe while idle to draw the eye.
///
/// Both behaviours live here because both drive the same scale; two components would
/// fight over it.
/// </summary>
[RequireComponent(typeof(Button))]
public class UIButtonEffect : MonoBehaviour,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerExitHandler
{
    [SerializeField] private Button button;

    [Header("Scale")]
    [SerializeField, Range(0.5f, 1f)] private float pressedScale = 0.9f;
    [SerializeField, Min(0f)] private float pressDuration = 0.08f;
    [SerializeField, Min(0f)] private float releaseDuration = 0.18f;

    [Header("Idle pulse")]
    [Tooltip("Grow and shrink continuously while nobody is touching it. Meant for the one button a screen wants pressed.")]
    [SerializeField] private bool pulsesWhenIdle;
    [SerializeField, Range(1f, 1.3f)] private float pulseScale = 1.06f;
    [Tooltip("Seconds for one grow-and-shrink cycle.")]
    [SerializeField, Min(0.2f)] private float pulsePeriod = 1.2f;

    [Header("Feedback")]
    [Tooltip("Turn off where the button's action already plays its own sound, so the two do not stack.")]
    [SerializeField] private bool playsClickSound = true;
    [SerializeField] private bool playsHaptic = true;

    private Vector3 originalScale;
    private Tween scaleTween;
    private bool isPressed;

    private void Reset()
    {
        button = GetComponent<Button>();
    }

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        if (button != null)
        {
            button.onClick.AddListener(HandleClick);
        }

        StartPulse();
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }

        // A panel can close while a finger is still down; without this the button
        // would reopen shrunk.
        scaleTween?.Kill();
        isPressed = false;
        transform.localScale = originalScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button != null && !button.IsInteractable())
        {
            return;
        }

        isPressed = true;
        AnimateTo(originalScale * pressedScale, pressDuration, Ease.OutQuad);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Release();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Release();
    }

    private void Release()
    {
        if (!isPressed)
        {
            return;
        }

        isPressed = false;
        AnimateTo(originalScale, releaseDuration, Ease.OutBack);
        scaleTween.OnComplete(StartPulse);
    }

    private void StartPulse()
    {
        if (!pulsesWhenIdle || isPressed || !isActiveAndEnabled)
        {
            return;
        }

        scaleTween?.Kill();
        transform.localScale = originalScale;

        // Half a period out, half back; Yoyo keeps it going until a press takes over.
        scaleTween = transform
            .DOScale(originalScale * pulseScale, pulsePeriod * 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }

    private void HandleClick()
    {
        if (playsClickSound)
        {
            AudioController.Instance?.PlaySound(SoundName.UI_ButtonClick);
        }

        if (playsHaptic)
        {
            VibrationController.Instance?.VibratePop();
        }
    }

    private void AnimateTo(Vector3 target, float duration, Ease ease)
    {
        scaleTween?.Kill();

        // Unscaled so buttons keep responding while the game is paused.
        scaleTween = transform
            .DOScale(target, duration)
            .SetEase(ease)
            .SetUpdate(true);
    }

    private void OnDestroy()
    {
        scaleTween?.Kill();
    }
}
