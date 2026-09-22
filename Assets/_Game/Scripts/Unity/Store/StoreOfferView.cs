using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// One product card in the store: its buy button shows the price, pulses while the
    /// product can be bought, and turns into a still, grey "OWNED" once it has been.
    /// A new product is a new card, not a change to the store screen.
    /// </summary>
    public sealed class StoreOfferView : MonoBehaviour
    {
        [SerializeField] private StoreProduct product;
        [SerializeField] private Button buyButton;
        [SerializeField] private UIButtonEffect buyEffect;
        [SerializeField] private TMP_Text buyLabel;
        [SerializeField] private Image buyBackground;
        [SerializeField] private Color buyColor = new Color32(46, 204, 140, 255);
        [SerializeField] private Color ownedColor = new Color32(78, 90, 102, 255);

        public event Action<StoreOfferView> OnBuyClicked;

        public StoreProduct Product => product;

        private void OnEnable()
        {
            buyButton.onClick.AddListener(HandleClick);
        }

        private void OnDisable()
        {
            buyButton.onClick.RemoveListener(HandleClick);
        }

        public void Refresh(IPurchaseService purchases, bool isBusy)
        {
            bool isOwned = purchases != null && purchases.IsOwned(product);

            buyLabel.text = isOwned ? "OWNED" : (purchases == null ? string.Empty : purchases.GetPrice(product));
            buyBackground.color = isOwned ? ownedColor : buyColor;
            buyButton.interactable = !isOwned && !isBusy && purchases != null;
            buyEffect.SetPulsing(!isOwned);
        }

        private void HandleClick()
        {
            OnBuyClicked?.Invoke(this);
        }
    }
}
