using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AirlinePop.SingleLine.Unity
{
    /// <summary>
    /// Press feedback shared by every button: shrinks on touch, springs back on release,
    /// and clicks with a light buzz only when the press lands. A main call to action can
    /// also breathe while idle to draw the eye; both live here because both drive the
    /// same scale.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class ButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private Button button;

        [Header("Scale")]
        [SerializeField, Range(0.5f, 1f)] private float pressedScale = 0.9f;
        [SerializeField, Min(0f)] private float pressDuration = 0.08f;
        [SerializeField, Min(0f)] private float releaseDuration = 0.18f;

        [Header("Idle pulse")]
        [Tooltip("Grow and shrink while nobody touches it. For the one button a screen wants pressed.")]
        [SerializeField] private bool pulsesWhenIdle;
        [SerializeField, Range(1f, 1.3f)] private float pulseScale = 1.06f;
        [Tooltip("Seconds for one grow-and-shrink cycle.")]
        [SerializeField, Min(0.2f)] private float pulsePeriod = 1.2f;

        [Header("Feedback")]
        [Tooltip("Off where the button's action already plays its own sound, so the two do not stack.")]
        [SerializeField] private bool playsClickSound = true;
        [SerializeField] private bool playsHaptic = true;

        private Vector3 restScale;
        private Tween scaleTween;
        private bool isPressed;

        private void Reset()
        {
            button = GetComponent<Button>();
        }

        private void Awake()
        {
            restScale = transform.localScale;
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

            // A panel can close under a finger; the button must not reopen shrunk.
            scaleTween?.Kill();
            isPressed = false;
            transform.localScale = restScale;
        }

        private void OnDestroy()
        {
            scaleTween?.Kill();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (button != null && !button.IsInteractable())
            {
                return;
            }

            isPressed = true;
            ScaleTo(restScale * pressedScale, pressDuration, Ease.OutQuad);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Release();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Release();
        }

        /// <summary>Turns the idle pulse on or off, for a call to action that comes and goes.</summary>
        public void SetPulsing(bool isPulsing)
        {
            if (pulsesWhenIdle == isPulsing)
            {
                return;
            }

            pulsesWhenIdle = isPulsing;

            if (isPulsing)
            {
                StartPulse();
                return;
            }

            if (!isPressed)
            {
                scaleTween?.Kill();
                transform.localScale = restScale;
            }
        }

        private void Release()
        {
            if (!isPressed)
            {
                return;
            }

            isPressed = false;
            ScaleTo(restScale, releaseDuration, Ease.OutBack);
            scaleTween.OnComplete(StartPulse);
        }

        private void StartPulse()
        {
            if (!pulsesWhenIdle || isPressed || !isActiveAndEnabled)
            {
                return;
            }

            scaleTween?.Kill();
            transform.localScale = restScale;
            scaleTween = transform
                .DOScale(restScale * pulseScale, pulsePeriod * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        private void HandleClick()
        {
            if (playsClickSound)
            {
                GameAudio.Instance?.PlayButtonClick();
            }

            if (playsHaptic)
            {
                DeviceHaptics.Instance.Play(HapticStrength.Light);
            }
        }

        private void ScaleTo(Vector3 target, float duration, Ease ease)
        {
            scaleTween?.Kill();

            // Unscaled, so buttons keep answering while the game is paused.
            scaleTween = transform.DOScale(target, duration).SetEase(ease).SetUpdate(true);
        }
    }
}
