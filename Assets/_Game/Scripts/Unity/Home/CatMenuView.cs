using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The radial menu around a regular: the middle of the cat's body is the centre, its care
    /// actions sit round it on an arc at one distance, spread evenly and evenly balanced over
    /// its head, and its name tag (name, loyalty card, progress) closes the ring under its feet. More actions just share the
    /// same arc. The answer to the last action pops up over the arc and fades. A tap anywhere
    /// off the menu closes it. Display only; the presenter decides what the buttons do.
    /// </summary>
    public sealed class CatMenuView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform anchor;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text tierLabel;
        [SerializeField] private ProgressBarView tierBar;
        [SerializeField] private TMP_Text feedLabel;
        [SerializeField] private GameObject feedBadge;
        [SerializeField] private TMP_Text feedCount;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private Button petButton;
        [SerializeField] private Button playButton;
        [SerializeField] private Button feedButton;
        [SerializeField] private Button wardrobeButton;
        [SerializeField] private Button backdropButton;

        [Header("Arc")]
        [Tooltip("The actions' holders, in order along the arc from its start angle to its end angle.")]
        [SerializeField] private RectTransform[] actions = new RectTransform[0];
        [Tooltip("The arc's radius never goes below this, in canvas units, however small the cat looks.")]
        [SerializeField, Min(50f)] private float minRadius = 170f;

        [Tooltip("Clear space between the cat's outline and the middle of each button, in canvas units.")]
        [SerializeField, Min(0f)] private float buttonClearance = 82f;

        [Tooltip("Angles in degrees, 0 to the cat's right and 90 over its head: the arc runs from the first to the second. " +
                 "Keep them mirrored round 90 so the ring sits square on the cat.")]
        [SerializeField] private float startAngle = 168f;
        [SerializeField] private float endAngle = 12f;

        [Header("Name tag and status")]
        [SerializeField] private RectTransform tagPop;
        [Tooltip("Gap between the cat's feet and the top of its name tag, in canvas units.")]
        [SerializeField] private float tagGap = 14f;
        [SerializeField] private float tagHeight = 100f;
        [SerializeField] private RectTransform statusPop;
        [SerializeField] private float statusLift = 118f;

        [Tooltip("Screen hints that would sit under the menu, such as the lounge's \"tap a cat\" line: faded out while it is open.")]
        [SerializeField] private CanvasGroup[] hideWhileOpen = new CanvasGroup[0];

        [Tooltip("Half the width of an action with its label pill, so the outermost ones stay whole near an edge.")]
        [SerializeField] private float actionHalfWidth = 70f;

        [Tooltip("Room kept between the menu and the screen's edges, in canvas units.")]
        [SerializeField] private float edgeRoom = 16f;

        [Tooltip("Height of the screen's top bar and bottom tab bar, which the menu keeps clear of.")]
        [SerializeField] private float topBarHeight = 150f;
        [SerializeField] private float tabBarHeight = 190f;

        [SerializeField, Min(0.05f)] private float popSeconds = 0.24f;
        [SerializeField, Min(0f)] private float popStagger = 0.05f;
        [SerializeField, Min(0.3f)] private float statusSeconds = 1.6f;

        private Vector2[] actionRests;
        private Vector2 tagRest;
        private Sequence motion;
        private Sequence statusMotion;
        private string lastStatus = string.Empty;

        public event Action OnPet;
        public event Action OnPlay;
        public event Action OnFeed;
        public event Action OnWardrobe;

        /// <summary>Raised whenever the menu folds away, however it was closed.</summary>
        public event Action OnHidden;
        public event Action OnClose;

        public bool IsOpen { get; private set; }

        /// <summary>The part of the screen the menu and its panels stay inside, in the anchor's parent space.</summary>
        public Rect FreeArea
        {
            get
            {
                Rect area = ((RectTransform)anchor.parent).rect;
                area.yMin += tabBarHeight;
                area.yMax -= topBarHeight;
                return area;
            }
        }

        private void Awake()
        {
            actionRests = new Vector2[actions.Length];
            HideInstant();
        }

        private void OnEnable()
        {
            petButton.onClick.AddListener(HandlePet);
            playButton.onClick.AddListener(HandlePlay);
            feedButton.onClick.AddListener(HandleFeed);
            wardrobeButton.onClick.AddListener(HandleWardrobe);
            backdropButton.onClick.AddListener(HandleClose);
        }

        private void OnDisable()
        {
            petButton.onClick.RemoveListener(HandlePet);
            playButton.onClick.RemoveListener(HandlePlay);
            feedButton.onClick.RemoveListener(HandleFeed);
            wardrobeButton.onClick.RemoveListener(HandleWardrobe);
            backdropButton.onClick.RemoveListener(HandleClose);
        }

        private void OnDestroy()
        {
            motion?.Kill();
            statusMotion?.Kill();
        }

        /// <summary>
        /// Rings the menu round the cat's body: centred on its middle, the arc far enough out
        /// to clear its outline, the name tag hanging just under its feet. Near an edge the
        /// whole ring moves in just far enough for every button to stay in view.
        /// </summary>
        public void PlaceAt(Bounds catBody)
        {
            var parent = (RectTransform)anchor.parent;
            Vector2 centre = ToLocal(parent, catBody.center);
            float halfHeight = Mathf.Abs(ToLocal(parent, catBody.center + Vector3.up * catBody.extents.y).y - centre.y);
            float radius = Mathf.Max(minRadius, halfHeight + buttonClearance);
            LayOutArc(radius);
            statusPop.anchoredPosition = new Vector2(0f, radius + statusLift);
            tagRest = new Vector2(0f, -(halfHeight + tagGap));

            // The whole ring: the arc's buttons and labels, the answer over them, the tag under the feet.
            float halfWidth = Mathf.Max(radius + actionHalfWidth, statusPop.rect.width * 0.5f);
            var below = new Vector2(-halfWidth, tagRest.y - tagHeight);
            var above = new Vector2(halfWidth, radius + statusLift + statusPop.rect.height * 0.5f);
            anchor.anchoredPosition = ScreenEdgeClamp.Inside(centre, below, above, FreeArea, edgeRoom);
        }

        /// <summary>Spreads the actions evenly along the arc, all at the same distance from the cat.</summary>
        private void LayOutArc(float radius)
        {
            for (int i = 0; i < actions.Length; i++)
            {
                float t = actions.Length == 1 ? 0.5f : i / (float)(actions.Length - 1);
                float angle = Mathf.Lerp(startAngle, endAngle, t) * Mathf.Deg2Rad;
                actionRests[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }
        }

        public void Show(in CatMenuModel model)
        {
            nameLabel.text = model.Name;
            tierLabel.text = model.Tier;
            tierBar.SetProgress(model.TierProgress);
            feedLabel.text = model.FeedLabel;
            feedBadge.SetActive(model.FoodCount > 0);
            feedCount.SetText("{0}", model.FoodCount);
            ShowStatus(model.Status);

            if (IsOpen)
            {
                return;
            }

            IsOpen = true;
            FadeHints(0f);
            group.alpha = 1f;
            group.blocksRaycasts = true;
            group.interactable = true;
            motion?.Kill();
            motion = DOTween.Sequence().SetUpdate(true);

            // The actions spring out from the cat to their places round it, one after another;
            // the name tag drops in under its feet.
            for (int i = 0; i < actions.Length; i++)
            {
                actions[i].anchoredPosition = Vector2.zero;
                actions[i].localScale = Vector3.zero;
                float at = i * popStagger;
                motion.Insert(at, actions[i].DOAnchorPos(actionRests[i], popSeconds).SetEase(Ease.OutBack));
                motion.Insert(at, actions[i].DOScale(1f, popSeconds).SetEase(Ease.OutBack));
            }

            tagPop.anchoredPosition = tagRest + new Vector2(0f, 30f);
            tagPop.localScale = Vector3.zero;
            motion.Insert(0f, tagPop.DOAnchorPos(tagRest, popSeconds).SetEase(Ease.OutBack));
            motion.Insert(0f, tagPop.DOScale(1f, popSeconds).SetEase(Ease.OutBack));
        }

        public void Hide()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            FadeHints(1f);
            group.blocksRaycasts = false;
            group.interactable = false;
            motion?.Kill();
            motion = DOTween.Sequence().SetUpdate(true);

            for (int i = 0; i < actions.Length; i++)
            {
                motion.Join(actions[i].DOAnchorPos(Vector2.zero, popSeconds * 0.7f).SetEase(Ease.InBack));
                motion.Join(actions[i].DOScale(0f, popSeconds * 0.7f).SetEase(Ease.InBack));
            }

            motion.Join(tagPop.DOScale(0f, popSeconds * 0.7f).SetEase(Ease.InBack));
            motion.OnComplete(() => group.alpha = 0f);
            HideStatus();
            OnHidden?.Invoke();
        }

        /// <summary>
        /// Folds the actions back into the cat, or springs them out again, so another panel
        /// such as the wardrobe can take their place round the cat.
        /// </summary>
        public void SetActionsShown(bool isShown)
        {
            motion?.Kill();
            motion = DOTween.Sequence().SetUpdate(true);

            for (int i = 0; i < actions.Length; i++)
            {
                Vector2 to = isShown ? actionRests[i] : Vector2.zero;
                float scale = isShown ? 1f : 0f;
                Ease ease = isShown ? Ease.OutBack : Ease.InBack;
                motion.Insert(isShown ? i * popStagger : 0f, actions[i].DOAnchorPos(to, popSeconds).SetEase(ease));
                motion.Insert(isShown ? i * popStagger : 0f, actions[i].DOScale(scale, popSeconds).SetEase(ease));
            }
        }

        private void FadeHints(float alpha)
        {
            for (int i = 0; i < hideWhileOpen.Length; i++)
            {
                hideWhileOpen[i].DOKill();
                hideWhileOpen[i].DOFade(alpha, popSeconds).SetUpdate(true);
            }
        }

        private Vector2 ToLocal(RectTransform parent, Vector3 worldPoint)
        {
            Vector2 screen = worldCamera.WorldToScreenPoint(worldPoint);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, null, out Vector2 local);
            return local;
        }

        /// <summary>A new answer pops up over the arc and fades away; an unchanged one is left be.</summary>
        private void ShowStatus(string status)
        {
            if (string.IsNullOrEmpty(status))
            {
                lastStatus = string.Empty;
                HideStatus();
                return;
            }

            if (status == lastStatus && statusMotion != null && statusMotion.IsActive())
            {
                return;
            }

            lastStatus = status;
            statusLabel.text = status;
            statusMotion?.Kill();
            statusPop.gameObject.SetActive(true);
            statusPop.localScale = Vector3.one * 0.6f;
            statusMotion = DOTween.Sequence().SetUpdate(true)
                .Append(statusPop.DOScale(1f, 0.2f).SetEase(Ease.OutBack))
                .AppendInterval(statusSeconds)
                .Append(statusPop.DOScale(0f, 0.18f).SetEase(Ease.InBack))
                .OnComplete(HideStatus);
        }

        private void HideStatus()
        {
            statusMotion?.Kill();
            statusMotion = null;
            statusPop.gameObject.SetActive(false);
        }

        private void HideInstant()
        {
            IsOpen = false;
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            for (int i = 0; i < actions.Length; i++)
            {
                actions[i].localScale = Vector3.zero;
            }

            tagPop.localScale = Vector3.zero;
            HideStatus();
        }

        private void HandlePet()
        {
            OnPet?.Invoke();
        }

        private void HandlePlay()
        {
            OnPlay?.Invoke();
        }

        private void HandleFeed()
        {
            OnFeed?.Invoke();
        }

        private void HandleWardrobe()
        {
            OnWardrobe?.Invoke();
        }

        private void HandleClose()
        {
            OnClose?.Invoke();
        }

#if UNITY_EDITOR
        public void EditorLink(CanvasGroup linkedGroup, RectTransform linkedAnchor, TMP_Text linkedName, TMP_Text linkedTier,
            ProgressBarView linkedBar, TMP_Text linkedFeed, GameObject linkedBadge, TMP_Text linkedCount, TMP_Text linkedStatus,
            RectTransform linkedStatusPop, Button linkedPet, Button linkedPlay, Button linkedFeedButton, Button linkedWardrobe,
            Button linkedBackdrop, RectTransform[] linkedActions, RectTransform linkedTag)
        {
            wardrobeButton = linkedWardrobe;
            group = linkedGroup;
            anchor = linkedAnchor;
            nameLabel = linkedName;
            tierLabel = linkedTier;
            tierBar = linkedBar;
            feedLabel = linkedFeed;
            feedBadge = linkedBadge;
            feedCount = linkedCount;
            statusLabel = linkedStatus;
            statusPop = linkedStatusPop;
            petButton = linkedPet;
            playButton = linkedPlay;
            feedButton = linkedFeedButton;
            backdropButton = linkedBackdrop;
            actions = linkedActions;
            tagPop = linkedTag;
        }

        public void EditorLinkCamera(Camera linkedCamera)
        {
            worldCamera = linkedCamera;
        }

        public void EditorLinkHints(CanvasGroup[] linkedHints)
        {
            hideWhileOpen = linkedHints;
        }
#endif
    }
}
