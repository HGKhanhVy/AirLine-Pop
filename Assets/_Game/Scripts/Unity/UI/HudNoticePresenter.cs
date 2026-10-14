using AirlinePop.SingleLine.Core;
using UnityEngine;

namespace AirlinePop.SingleLine.Unity
{
    /// <summary>
    /// Decides when the HUD notice speaks: a storm flight is billed as it opens, once the card
    /// explaining storms is out of the way.
    /// </summary>
    public sealed class HudNoticePresenter : MonoBehaviour
    {
        [SerializeField] private HudNoticeView view;
        [SerializeField] private Sprite stormIcon;

        private bool isStormPending;
        private bool isRuleCardOpen;

        private void OnEnable()
        {
            GameplayEvents.OnLevelRules += HandleLevelRules;
            GameplayEvents.OnRuleIntroChanged += HandleRuleIntroChanged;
        }

        private void OnDisable()
        {
            GameplayEvents.OnLevelRules -= HandleLevelRules;
            GameplayEvents.OnRuleIntroChanged -= HandleRuleIntroChanged;
        }

        private void HandleLevelRules(LevelRule rules)
        {
            isStormPending = (rules & LevelRule.Storm) != 0;

            // The rule cards answer the same event; a card opening now says so before this
            // frame ends, so the notice waits a frame to know whether to hold back.
            if (isStormPending)
            {
                Invoke(nameof(ShowStormIfClear), 0f);
            }
        }

        private void HandleRuleIntroChanged(bool isShown)
        {
            isRuleCardOpen = isShown;

            if (!isShown)
            {
                ShowStormIfClear();
            }
        }

        private void ShowStormIfClear()
        {
            if (!isStormPending || isRuleCardOpen)
            {
                return;
            }

            isStormPending = false;
            view.Show(stormIcon, Localization.Get("storm.banner"));
        }

#if UNITY_EDITOR
        public void EditorLink(HudNoticeView linkedView, Sprite storm)
        {
            view = linkedView;
            stormIcon = storm;
        }
#endif
    }
}
