using System.Collections.Generic;
using ASTeams.SingleLine.Core;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    [CreateAssetMenu(menuName = "Single Line/Text Catalog", fileName = "TextCatalog")]
    public sealed class TextCatalogSO : ScriptableObject
    {
        [SerializeField] private LocalizedTextEntry[] entries;

        public IReadOnlyList<LocalizedTextEntry> Entries => entries;
    }
}
