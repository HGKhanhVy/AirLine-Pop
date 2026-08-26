using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ASTeams.Base.UI
{
    [RequireComponent(typeof(Button))]
    public class UIBaseButton : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        // =========================
        // EVENTS
        // =========================
        [HideInInspector] public UnityEvent onClick = new UnityEvent();
        [HideInInspector] public UnityEvent onPointerEnter = new UnityEvent();
        [HideInInspector] public UnityEvent onPointerExit = new UnityEvent();

        // =========================
        // COMPONENTS
        // =========================
        [HideInInspector] public Button button;

        // =========================
        // ANIMATION
        // =========================
        private float animationScale = 1.1f;
        private float animationDuration = 0.1f;

        private bool isPressed;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(OnButtonClick);
        }

        // =========================
        // CLICK
        // =========================
        private void OnButtonClick()
        {
            if (isPressed) return;

            isPressed = true;

            VibrationController.Instance?.VibratePop();
            AudioController.Instance?.PlaySound(SoundName.UI_ButtonClick);

            AnimateButton();
        }

        private void AnimateButton()
        {
            transform.DOScale(Vector3.one * animationScale, animationDuration)
                .SetEase(Ease.InOutSine)
                .OnComplete(() =>
                {
                    onClick?.Invoke();

                    transform.DOScale(Vector3.one, animationDuration)
                        .SetEase(Ease.InOutSine)
                        .OnComplete(() => isPressed = false);
                });
        }

        // =========================
        // POINTER EVENTS
        // =========================
        public void OnPointerEnter(PointerEventData eventData)
        {
            //Debug.Log("OnPointerEnter");
            onPointerEnter?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            //Debug.Log("OnPointerExit");
            onPointerExit?.Invoke();
        }

        // =========================
        // BUTTON
        // =========================
        public void SetInteractable(bool interactable)
        {
            button.interactable = interactable;
        }

        private void OnDestroy()
        {
            button.onClick.RemoveListener(OnButtonClick);
        }
    }
}
