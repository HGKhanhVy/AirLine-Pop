using System.Collections.Generic;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>Every cat the game knows, in shop order.</summary>
    [CreateAssetMenu(menuName = "AirLine Pop/Cat Catalog", fileName = "CatCatalog")]
    public sealed class CatCatalogSO : ScriptableObject
    {
        [SerializeField] private CatBreedSO[] breeds = new CatBreedSO[0];

        public IReadOnlyList<CatBreedSO> Breeds => breeds;

        public CatBreedSO Find(string id)
        {
            for (int i = 0; i < breeds.Length; i++)
            {
                if (breeds[i] != null && breeds[i].Id == id)
                {
                    return breeds[i];
                }
            }

            return null;
        }

#if UNITY_EDITOR
        public void EditorSetBreeds(CatBreedSO[] list)
        {
            breeds = list;
        }
#endif
    }
}
