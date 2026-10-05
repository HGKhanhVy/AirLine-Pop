using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// One thing on the shop shelf: its picture, its name and one button that buys it, puts
    /// it on, or says it is already on. Knows nothing of what it sells.
    /// </summary>
    public sealed class ShopItemCardView : MonoBehaviour
    {
        [SerializeField] private Image picture;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private Button button;
        [SerializeField] private Image buttonFace;
        [SerializeField] private GameObject priceRow;
        [SerializeField] private TMP_Text priceLabel;
        [SerializeField] private TMP_Text actionLabel;

        [Header("Button faces")]
        [SerializeField] private Sprite buyFace;
        [SerializeField] private Sprite useFace;
        [SerializeField] private Sprite inUseFace;

        public event Action<ShopItemCardView> OnPressed;

        private void OnEnable()
        {
            button.onClick.AddListener(HandleClick);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(HandleClick);
        }

        public void Show(Sprite artwork, string itemName, ShopCardState state, int price, bool canAfford)
        {
            picture.sprite = artwork;
            nameLabel.text = itemName;
            priceRow.SetActive(state == ShopCardState.Buy);
            actionLabel.gameObject.SetActive(state != ShopCardState.Buy);

            switch (state)
            {
                case ShopCardState.Buy:
                    priceLabel.text = price.ToString();
                    buttonFace.sprite = buyFace;
                    button.interactable = canAfford;
                    break;

                case ShopCardState.Use:
                    actionLabel.text = Localization.Get("shop.use");
                    buttonFace.sprite = useFace;
                    button.interactable = true;
                    break;

                default:
                    actionLabel.text = Localization.Get("shop.inUse");
                    buttonFace.sprite = inUseFace;
                    button.interactable = false;
                    break;
            }
        }

        private void HandleClick()
        {
            OnPressed?.Invoke(this);
        }

#if UNITY_EDITOR
        public void EditorLink(Image linkedPicture, TMP_Text linkedName, Button linkedButton, Image linkedFace, GameObject linkedPriceRow,
            TMP_Text linkedPrice, TMP_Text linkedAction, Sprite linkedBuy, Sprite linkedUse, Sprite linkedInUse)
        {
            picture = linkedPicture;
            nameLabel = linkedName;
            button = linkedButton;
            buttonFace = linkedFace;
            priceRow = linkedPriceRow;
            priceLabel = linkedPrice;
            actionLabel = linkedAction;
            buyFace = linkedBuy;
            useFace = linkedUse;
            inUseFace = linkedInUse;
        }
#endif
    }
}
