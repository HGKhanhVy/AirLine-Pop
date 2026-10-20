using System.Collections.Generic;
using UnityEngine;

namespace AirlinePop.SingleLine.Unity
{
    /// <summary>
    /// The campaign in play order: entry N is flight N + 1. Written by the campaign
    /// rebuild, so the list a designer sees in the Inspector is the list the game plays.
    /// </summary>
    [CreateAssetMenu(menuName = "AirLine Pop/Level Order", fileName = "LevelOrder")]
    public sealed class LevelOrderSO : ScriptableObject
    {
        [SerializeField] private List<SingleLineLevelSO> levels = new List<SingleLineLevelSO>();

        public int LevelCount => levels.Count;

        /// <summary>The level at a zero-based position, or null past either end.</summary>
        public SingleLineLevelSO GetLevel(int index)
        {
            return index >= 0 && index < levels.Count ? levels[index] : null;
        }

#if UNITY_EDITOR
        public void EditorSetLevels(IEnumerable<SingleLineLevelSO> ordered)
        {
            levels.Clear();
            levels.AddRange(ordered);
        }
#endif
    }
}
