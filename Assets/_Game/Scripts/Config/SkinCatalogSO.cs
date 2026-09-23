using System.Collections.Generic;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Every skin the game ships with, in the order the shop shows them. The first one is
    /// what a new player wears.
    /// </summary>
    [CreateAssetMenu(menuName = "Single Line/Skin Catalog", fileName = "SkinCatalog")]
    public sealed class SkinCatalogSO : ScriptableObject
    {
        [SerializeField] private SkinSO[] skins;

        [Tooltip("Given free when the player finishes the first chapter (GDD 2).")]
        [SerializeField] private SkinSO chapterOneReward;

        [Tooltip("Level that ends the first chapter.")]
        [SerializeField, Min(1)] private int chapterOneLastLevel = 30;

        public IReadOnlyList<SkinSO> Skins => skins;

        public SkinSO Default => skins != null && skins.Length > 0 ? skins[0] : null;

        public SkinSO ChapterOneReward => chapterOneReward;

        public int ChapterOneLastLevel => chapterOneLastLevel;

        public SkinSO Find(string id)
        {
            if (skins == null || string.IsNullOrEmpty(id))
            {
                return null;
            }

            for (int i = 0; i < skins.Length; i++)
            {
                if (skins[i] != null && skins[i].Id == id)
                {
                    return skins[i];
                }
            }

            return null;
        }
    }
}
