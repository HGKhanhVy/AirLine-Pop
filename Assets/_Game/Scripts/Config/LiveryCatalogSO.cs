using System;
using System.Collections.Generic;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>Every livery the shop sells, in shelf order; the first is the one players start with.</summary>
    [CreateAssetMenu(menuName = "AirLine Pop/Livery Catalog", fileName = "LiveryCatalog")]
    public sealed class LiveryCatalogSO : ScriptableObject
    {
        [SerializeField] private LiverySO[] liveries = new LiverySO[0];

        public IReadOnlyList<LiverySO> Liveries => liveries;

        public LiverySO Default => liveries.Length > 0 ? liveries[0] : null;

        public LiverySO Find(string id)
        {
            for (int i = 0; i < liveries.Length; i++)
            {
                if (liveries[i] != null && string.Equals(liveries[i].Id, id, StringComparison.Ordinal))
                {
                    return liveries[i];
                }
            }

            return null;
        }

#if UNITY_EDITOR
        public void EditorSetLiveries(LiverySO[] linkedLiveries)
        {
            liveries = linkedLiveries;
        }
#endif
    }
}
