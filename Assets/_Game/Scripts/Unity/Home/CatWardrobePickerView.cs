using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The row of accessories that opens over a cat from its menu's Dress up: the first slot
    /// takes the accessory off, the rest show each accessory, ringed when worn and padlocked
    /// with the loyalty card that unlocks it until then. Display only.
    /// </summary>
    public sealed class CatWardrobePickerView : MonoBehaviour
    {
        [SerializeField] private RectTransform row;
        [SerializeField] private Button[] slots = new Button[0];
        [SerializeField] private Image[] icons = new Image[0];
        [SerializeField] private TMP_Text[] labels = new TMP_Text[0];
        [SerializeField] private GameObject[] locks = new GameObject[0];
        [SerializeField] private GameObject[] wornRings = new GameObject[0];
        [SerializeField] private Button backButton;

        [SerializeField] private float lift = 210f;
        [SerializeField] private float edgeRoom = 16f;
        [SerializeField, Min(0.05f)] private float popSeconds = 0.22f;

        private Sequence motion;

        /// <summary>Raised with the slot pressed: 0 takes the accessory off, 1 and on are the accessories in order.</summary>
        public event Action<int> OnPicked;

        public event Action OnBack;

        public bool IsShown { get; private set; }

        private void Awake()
        {
            row.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                int slot = i;
                slots[i].onClick.AddListener(() => OnPicked?.Invoke(slot));
            }

            backButton.onClick.AddListener(HandleBack);
        }

        private void OnDisable()
        {
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].onClick.RemoveAllListeners();
            }

            backButton.onClick.RemoveListener(HandleBack);
        }

        /// <summary>
        /// Fills the slots, then pops the row up over the cat if it was not already showing,
        /// kept inside the area given, in the space of the menu anchor's parent.
        /// </summary>
        public void Show(WardrobeSlotModel[] models, Rect area)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                bool isUsed = i < models.Length;
                slots[i].gameObject.SetActive(isUsed);

                if (!isUsed)
                {
                    continue;
                }

                WardrobeSlotModel model = models[i];
                icons[i].enabled = model.Icon != null;
                icons[i].sprite = model.Icon;
                icons[i].color = model.IsUnlocked ? Color.white : new Color(1f, 1f, 1f, 0.35f);
                labels[i].text = model.Label;
                locks[i].SetActive(!model.IsUnlocked);
                wornRings[i].SetActive(model.IsWorn);
                slots[i].interactable = model.IsUnlocked;
            }

            if (IsShown)
            {
                return;
            }

            IsShown = true;
            motion?.Kill();
            row.gameObject.SetActive(true);
            Vector2 rest = RestInside(area);
            row.anchoredPosition = new Vector2(rest.x, rest.y * 0.6f);
            row.localScale = Vector3.zero;
            motion = DOTween.Sequence().SetUpdate(true)
                .Join(row.DOAnchorPos(rest, popSeconds).SetEase(Ease.OutBack))
                .Join(row.DOScale(1f, popSeconds).SetEase(Ease.OutBack));
        }

        /// <summary>
        /// Over the cat by default; near an edge the row slides in until all of it, the Back
        /// button sticking out of its corner included, is on screen.
        /// </summary>
        private Vector2 RestInside(Rect area)
        {
            var back = (RectTransform)backButton.transform;
            Vector2 backHalf = back.rect.size * 0.5f;
            Vector2 below = Vector2.Min(row.rect.min, back.anchoredPosition - backHalf);
            Vector2 above = Vector2.Max(row.rect.max, back.anchoredPosition + backHalf);
            Vector2 origin = row.parent.localPosition;
            Vector2 placed = ScreenEdgeClamp.Inside(origin + new Vector2(0f, lift), below, above, area, edgeRoom);
            return placed - origin;
        }

        public void Hide()
        {
            if (!IsShown)
            {
                return;
            }

            IsShown = false;
            motion?.Kill();
            motion = DOTween.Sequence().SetUpdate(true)
                .Join(row.DOScale(0f, popSeconds * 0.7f).SetEase(Ease.InBack))
                .OnComplete(() => row.gameObject.SetActive(false));
        }

        private void HandleBack()
        {
            OnBack?.Invoke();
        }

#if UNITY_EDITOR
        public void EditorLink(RectTransform linkedRow, Button[] linkedSlots, Image[] linkedIcons, TMP_Text[] linkedLabels,
            GameObject[] linkedLocks, GameObject[] linkedRings, Button linkedBack)
        {
            row = linkedRow;
            slots = linkedSlots;
            icons = linkedIcons;
            labels = linkedLabels;
            locks = linkedLocks;
            wornRings = linkedRings;
            backButton = linkedBack;
        }
#endif
    }
}
