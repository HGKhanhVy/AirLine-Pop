using ASTeams.Base.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The short notices a purchase or a restore ends with, shared by the store and the
    /// settings screen so both say the same thing.
    /// </summary>
    public static class PurchaseFeedback
    {
        private const string Location = "top";

        public static void ShowPurchaseResult(StoreProduct product, bool isPurchased)
        {
            if (!isPurchased)
            {
                Show("Purchase failed. Please try again.");
                return;
            }

            Show(product == StoreProduct.StarterPack ? "Starter Pack unlocked. Enjoy!" : "Ads removed. Thank you!");
        }

        public static void ShowRestoreResult(bool isRestored)
        {
            Show(isRestored ? "Purchases restored." : "No purchases to restore.");
        }

        private static void Show(string text)
        {
            UIMessageController messages = UIMessageController.Instance;

            if (messages != null)
            {
                messages.ShowNoti(text, Location);
            }
        }
    }
}
