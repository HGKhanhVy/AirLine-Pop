using DG.Tweening;
using TMPro;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>The coin pill: counts up or down to the balance with a small pop (GDD 6: updates at once).</summary>
    public sealed class CoinCounterView : MonoBehaviour
    {
        [SerializeField] private TMP_Text amountLabel;
        [SerializeField] private RectTransform icon;
        [SerializeField, Min(0.01f)] private float countDuration = 0.45f;

        private IWallet wallet;
        private long shown;
        private Tweener count;

        public void Initialize(IWallet coinWallet)
        {
            wallet = coinWallet;
            wallet.OnCoinsChanged += HandleCoinsChanged;
            shown = wallet.Coins;
            Render();
        }

        private void OnDestroy()
        {
            if (wallet != null)
            {
                wallet.OnCoinsChanged -= HandleCoinsChanged;
            }

            count?.Kill();
            icon.DOKill();
        }

        private void HandleCoinsChanged(long balance)
        {
            count?.Kill();
            count = DOTween.To(() => shown, value => { shown = value; Render(); }, balance, countDuration)
                .SetEase(Ease.OutCubic).SetUpdate(true);
            icon.DOKill(true);
            icon.DOPunchScale(Vector3.one * 0.25f, 0.3f, 6, 0.8f).SetUpdate(true);
        }

        private void Render()
        {
            amountLabel.SetText("{0}", shown);
        }

#if UNITY_EDITOR
        public void EditorLink(TMP_Text linkedLabel, RectTransform linkedIcon)
        {
            amountLabel = linkedLabel;
            icon = linkedIcon;
        }
#endif
    }
}
