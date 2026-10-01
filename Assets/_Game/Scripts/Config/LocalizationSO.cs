using System.Collections.Generic;
using ASTeams.SingleLine.Core;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Every line of player-facing text in English and Vietnamese. Lives in Resources so the
    /// one localization service can load it once, whichever scene the game starts in.
    /// Written by the editor's LocalizationInstaller from its table.
    /// </summary>
    [CreateAssetMenu(menuName = "Single Line/Localization", fileName = "Localization")]
    public sealed class LocalizationSO : ScriptableObject
    {
        public const string ResourcePath = "Localization";

        [SerializeField] private BilingualTextEntry[] entries = new BilingualTextEntry[0];

        public IReadOnlyList<BilingualTextEntry> Entries => entries;

#if UNITY_EDITOR
        public void EditorSetEntries(BilingualTextEntry[] linkedEntries)
        {
            entries = linkedEntries;
        }
#endif
    }
}
