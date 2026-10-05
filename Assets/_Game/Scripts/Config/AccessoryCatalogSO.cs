using System.Collections.Generic;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>Every accessory a regular can wear, in the order the wardrobe shows them.</summary>
    [CreateAssetMenu(menuName = "AirLine Pop/Accessory Catalog", fileName = "AccessoryCatalog")]
    public sealed class AccessoryCatalogSO : ScriptableObject
    {
        [SerializeField] private AccessorySO[] accessories = new AccessorySO[0];

        public IReadOnlyList<AccessorySO> Accessories => accessories;

        public AccessorySO Find(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            for (int i = 0; i < accessories.Length; i++)
            {
                if (accessories[i] != null && accessories[i].Id == id)
                {
                    return accessories[i];
                }
            }

            return null;
        }

#if UNITY_EDITOR
        public void EditorSetAccessories(AccessorySO[] linkedAccessories)
        {
            accessories = linkedAccessories;
        }
#endif
    }
}
