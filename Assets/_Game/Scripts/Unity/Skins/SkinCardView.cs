using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The skin on show: a little board painted in its colours, its name, and the one
    /// button that either buys it or puts it on. The preview is the same board the home
    /// screen wears as a logo, so a skin is judged on the thing it changes.
    ///
    /// Which skin it shows is set from outside, so one card can page through them all.
    /// </summary>
    public sealed class SkinCardView : MonoBehaviour
    {
        /// <summary>The colours turn with the level, so the preview has to pick one.</summary>
        private const int PreviewLevel = 1;

        [SerializeField] private SkinSO skin;

        [Header("Preview")]
        [SerializeField] private Image background;
        [SerializeField] private Image startCell;
        [SerializeField] private Image[] visitedCells;
        [SerializeField] private Image[] emptyCells;
        [SerializeField] private Image[] pathParts;
        [SerializeField] private Image startDot;

        [Header("Labels")]
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text actionLabel;

        [Header("Action")]
        [SerializeField] private Button actionButton;
        [SerializeField] private Image actionBackground;
        [SerializeField] private UIButtonEffect actionEffect;

        [Header("Worn")]
        [Tooltip("Ring shown around the card the player is wearing.")]
        [SerializeField] private Image wornOutline;

        [SerializeField] private Color buyColor = new Color32(46, 204, 140, 255);
        [SerializeField] private Color wornColor = new Color32(78, 90, 102, 255);

        public event Action<SkinCardView> OnClicked;

        public SkinSO Skin => skin;

        /// <summary>Puts another skin on show.</summary>
        public void SetSkin(SkinSO newSkin)
        {
            skin = newSkin;
            PaintPreview();
        }

        private void OnEnable()
        {
            actionButton.onClick.AddListener(HandleClick);
            PaintPreview();
        }

        private void OnDisable()
        {
            actionButton.onClick.RemoveListener(HandleClick);
        }

        public void Refresh(ISkinService skins)
        {
            if (skin == null)
            {
                return;
            }

            PaintPreview();
            nameLabel.text = skin.DisplayName;

            bool isOwned = skins != null && skins.IsOwned(skin);
            bool isWorn = isOwned && skins.Equipped == skin;

            // A skin that is not sold cannot be reached from here; the label says where it comes from.
            bool isReachable = isOwned || skin.IsSoldInShop;

            actionLabel.text = isWorn ? "WEARING"
                : isOwned ? "WEAR"
                : skin.IsSoldInShop ? skin.Price.ToString("N0")
                : "STARTER PACK";

            actionBackground.color = isWorn || !isReachable ? wornColor : buyColor;
            actionButton.interactable = !isWorn && isReachable;
            actionEffect.SetPulsing(!isWorn && isReachable);

            if (wornOutline != null)
            {
                wornOutline.enabled = isWorn;
            }
        }

        private void PaintPreview()
        {
            if (skin == null)
            {
                return;
            }

            BoardPalette palette = skin.GetPalette(PreviewLevel);
            background.color = skin.Background;
            startCell.color = palette.Start;
            Paint(visitedCells, palette.Visited);
            Paint(emptyCells, palette.Cell);

            // The line is drawn translucent over the squares; on a flat card it reads better solid.
            Color line = palette.Path;
            line.a = 1f;
            Paint(pathParts, line);

            if (startDot != null)
            {
                startDot.color = Color.white;
            }
        }

        private static void Paint(Image[] images, Color colour)
        {
            if (images == null)
            {
                return;
            }

            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null)
                {
                    images[i].color = colour;
                }
            }
        }

        private void HandleClick()
        {
            OnClicked?.Invoke(this);
        }
    }
}
