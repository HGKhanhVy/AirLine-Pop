using ASTeams.Base.Data;
using ASTeams.SingleLine.Core;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The first time a level brings in a rule the player has not met, explains it on a
    /// card before they start; a level with two new rules shows the cards one after the other.
    /// </summary>
    public sealed class RuleIntroPresenter : MonoBehaviour
    {
        [SerializeField] private RuleIntroView view;
        [SerializeField] private RuleIntroEntry[] entries = new RuleIntroEntry[0];

        private IRuleIntroStore store;
        private LevelRule pending;

        private void OnEnable()
        {
            store ??= new ProfileRuleIntroStore(UserProfileController.Instance);
            GameplayEvents.OnLevelRules += HandleLevelRules;
            view.OnClosed += ShowNext;
        }

        private void OnDisable()
        {
            GameplayEvents.OnLevelRules -= HandleLevelRules;
            view.OnClosed -= ShowNext;
        }

        private void HandleLevelRules(LevelRule rules)
        {
            pending = LevelRule.None;

            for (int i = 0; i < entries.Length; i++)
            {
                LevelRule rule = entries[i].rule;

                if ((rules & rule) != 0 && !store.HasSeen(rule))
                {
                    pending |= rule;
                }
            }

            if (!view.IsShown)
            {
                ShowNext();
            }
        }

        private void ShowNext()
        {
            for (int i = 0; i < entries.Length; i++)
            {
                RuleIntroEntry entry = entries[i];

                if ((pending & entry.rule) == 0)
                {
                    continue;
                }

                pending &= ~entry.rule;
                store.MarkSeen(entry.rule);
                view.Show(entry);
                GameplayEvents.RaiseRuleIntroChanged(true);
                return;
            }

            GameplayEvents.RaiseRuleIntroChanged(false);
        }

#if UNITY_EDITOR
        public void EditorLink(RuleIntroView linkedView, RuleIntroEntry[] linkedEntries)
        {
            view = linkedView;
            entries = linkedEntries;
        }
#endif
    }
}
