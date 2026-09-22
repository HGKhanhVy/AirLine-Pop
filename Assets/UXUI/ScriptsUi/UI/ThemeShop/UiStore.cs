using ASTeams.SingleLine.Unity;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Store popup (GDD 12.3): the product cards and Restore Purchases. Only shows and
/// forwards; buying and owning belong to the purchase service.
/// </summary>
public class UiStore : Uibase
{
    [SerializeField] private Button closeButton;
    [SerializeField] private StoreOfferView[] offers;
    [SerializeField] private Button restoreButton;

    private IPurchaseService purchases;
    private bool isBusy;

    public void Initialize(IPurchaseService purchaseService)
    {
        purchases = purchaseService;
    }

    public override void Show()
    {
        base.Show();
        Refresh();
    }

    private void OnEnable()
    {
        closeButton.onClick.AddListener(Hide);
        restoreButton.onClick.AddListener(RestorePurchases);

        for (int i = 0; i < offers.Length; i++)
        {
            offers[i].OnBuyClicked += Buy;
        }
    }

    private void OnDisable()
    {
        closeButton.onClick.RemoveListener(Hide);
        restoreButton.onClick.RemoveListener(RestorePurchases);

        for (int i = 0; i < offers.Length; i++)
        {
            offers[i].OnBuyClicked -= Buy;
        }
    }

    private void Refresh()
    {
        for (int i = 0; i < offers.Length; i++)
        {
            offers[i].Refresh(purchases, isBusy);
        }
    }

    private void Buy(StoreOfferView offer)
    {
        if (isBusy || purchases == null)
        {
            return;
        }

        // Every card is locked until the store answers, so no second purchase can start.
        isBusy = true;
        Refresh();

        StoreProduct product = offer.Product;
        purchases.Buy(product, isPurchased => HandlePurchaseFinished(product, isPurchased));
    }

    private void HandlePurchaseFinished(StoreProduct product, bool isPurchased)
    {
        isBusy = false;
        PurchaseFeedback.ShowPurchaseResult(product, isPurchased);
        Refresh();
    }

    private void RestorePurchases()
    {
        purchases?.Restore(HandleRestoreFinished);
    }

    private void HandleRestoreFinished(bool isRestored)
    {
        PurchaseFeedback.ShowRestoreResult(isRestored);
        Refresh();
    }
}
