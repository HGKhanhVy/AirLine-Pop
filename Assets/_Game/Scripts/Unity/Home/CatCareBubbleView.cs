using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// A little speech bubble with a paw in it that pops up beside the head of a cat just
    /// touched, a beat after its reaction starts, so the reaction is seen in full before
    /// anything covers it, and bobs there gently. Pressing it opens the cat's care card;
    /// left alone it shrinks away after a few seconds.
    /// </summary>
    public sealed class CatCareBubbleView : MonoBehaviour
    {
        [SerializeField] private RectTransform bubble;
        [SerializeField] private Button button;
        [SerializeField] private Camera worldCamera;

        [Header("Facing")]
        [Tooltip("The bubble's picture and the paw on it, swapped round when the bubble has to sit on the cat's other side.")]
        [SerializeField] private Image face;
        [SerializeField] private RectTransform paw;
        [SerializeField] private RectTransform press;
        [SerializeField] private Sprite rightSprite;
        [SerializeField] private Sprite leftSprite;

        [Tooltip("Room the bubble keeps from the screen's side edges before it flips round, in canvas units.")]
        [SerializeField, Min(0f)] private float edgeMargin = 12f;

        [Tooltip("How long the button waits before popping up, so the cat's reaction plays uncovered.")]
        [SerializeField, Min(0f)] private float delay = 0.6f;

        [Tooltip("How long the button stays up when it is not pressed.")]
        [SerializeField, Min(0.5f)] private float lifetime = 4f;
        [SerializeField, Min(0.05f)] private float popSeconds = 0.25f;

        [Header("Bobbing")]
        [SerializeField] private float bobHeight = 8f;
        [SerializeField] private float bobDegrees = 4f;
        [SerializeField, Min(0.2f)] private float bobSeconds = 0.9f;

        private Sequence showing;
        private Vector2 restPosition;
        private Vector2 rightPivot;
        private Vector2 rightPawPosition;

        /// <summary>Raised when the player presses the button.</summary>
        public event Action OnPressed;

        /// <summary>Raised when the button shrinks away unpressed.</summary>
        public event Action OnExpired;

        public bool IsShown { get; private set; }

        private void Awake()
        {
            rightPivot = bubble.pivot;
            rightPawPosition = paw.anchoredPosition;
            HideInstant();
        }

        private void OnEnable()
        {
            button.onClick.AddListener(Press);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(Press);
        }

        /// <summary>
        /// Pops the bubble up beside a cat's head, on its right like a speech bubble, or on its
        /// left, tail turned round, when the right side would run it off the screen.
        /// </summary>
        public void ShowBeside(Vector3 head, Vector3 side)
        {
            showing?.Kill();
            var parent = (RectTransform)bubble.parent;
            Vector2 local = ToLocal(parent, head + side);
            bool fitsRight = local.x + bubble.rect.width * (1f - rightPivot.x) <= parent.rect.xMax - edgeMargin;

            if (!fitsRight)
            {
                local = ToLocal(parent, head - side);
            }

            Face(fitsRight);

            // A cat by the top of the screen, or too near both sides, still gets a whole bubble.
            Rect box = bubble.rect;
            local = ScreenEdgeClamp.Inside(local, box.min, box.max + new Vector2(0f, bobHeight), parent.rect, edgeMargin);
            bubble.anchoredPosition = local;
            restPosition = local;
            bubble.localRotation = Quaternion.identity;
            bubble.localScale = Vector3.zero;
            bubble.gameObject.SetActive(true);
            IsShown = true;

            showing = DOTween.Sequence()
                .AppendInterval(delay)
                .Append(bubble.DOScale(1f, popSeconds).SetEase(Ease.OutBack))
                .AppendCallback(StartBobbing)
                .AppendInterval(lifetime)
                .Append(bubble.DOScale(0f, popSeconds).SetEase(Ease.InBack))
                .OnComplete(Expire);
        }

        private Vector2 ToLocal(RectTransform parent, Vector3 worldPoint)
        {
            Vector2 screen = worldCamera.WorldToScreenPoint(worldPoint);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, null, out Vector2 local);
            return local;
        }

        /// <summary>Turns the bubble to sit on the cat's right or left: picture, tail point and paw mirror together.</summary>
        private void Face(bool right)
        {
            var pivot = right ? rightPivot : new Vector2(1f - rightPivot.x, rightPivot.y);
            bubble.pivot = pivot;
            press.pivot = pivot;
            face.sprite = right ? rightSprite : leftSprite;
            paw.anchoredPosition = right ? rightPawPosition : new Vector2(-rightPawPosition.x, rightPawPosition.y);
        }

        public void Hide()
        {
            showing?.Kill();
            showing = null;
            HideInstant();
        }

        private void Press()
        {
            Hide();
            OnPressed?.Invoke();
        }

        private void Expire()
        {
            HideInstant();
            OnExpired?.Invoke();
        }

        /// <summary>A soft float up and down with a little tilt, so the bubble feels alive while it waits.</summary>
        private void StartBobbing()
        {
            bubble.DOAnchorPosY(restPosition.y + bobHeight, bobSeconds).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
            bubble.localRotation = Quaternion.Euler(0f, 0f, -bobDegrees);
            bubble.DOLocalRotate(new Vector3(0f, 0f, bobDegrees), bobSeconds * 1.4f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
        }

        private void HideInstant()
        {
            bubble.DOKill();
            IsShown = false;
            bubble.localScale = Vector3.zero;
            bubble.gameObject.SetActive(false);
        }

#if UNITY_EDITOR
        public void EditorLink(RectTransform linkedBubble, Button linkedButton, Camera linkedCamera)
        {
            bubble = linkedBubble;
            button = linkedButton;
            worldCamera = linkedCamera;
        }

        public void EditorLinkCamera(Camera linkedCamera)
        {
            worldCamera = linkedCamera;
        }

        public void EditorLinkFacing(Image linkedFace, RectTransform linkedPaw, RectTransform linkedPress, Sprite linkedRight, Sprite linkedLeft)
        {
            face = linkedFace;
            paw = linkedPaw;
            press = linkedPress;
            rightSprite = linkedRight;
            leftSprite = linkedLeft;
        }
#endif
    }
}
