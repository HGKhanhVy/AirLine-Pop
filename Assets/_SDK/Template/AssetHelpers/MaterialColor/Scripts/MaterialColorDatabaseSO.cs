using System;
using System.Collections.Generic;
using UnityEngine;

namespace ASTeams.Base.Assets
{
    public enum ColorType
    {
        Blue = 0,
        Yellow = 1,
        Green = 2,
        Red = 3,
        Orange = 4,
        Purple = 5,
        Pink = 6,
        Cyan = 7,
        DarkBlue = 8,
        White = 9,
        Black = 10,
        Grey = 11
    }

    [CreateAssetMenu(fileName = "MaterialColorDatabase", menuName = "ASTeams/Materials/Material Color Database")]
    public class MaterialColorDatabaseSO : ScriptableObject
    {
        [Serializable]
        public class MaterialColorEntry
        {
            public ColorType colorType;
            public Material material;
        }

        [SerializeField] private List<MaterialColorEntry> entries = new List<MaterialColorEntry>();

        private Dictionary<ColorType, Material> _cache;

        public IReadOnlyList<MaterialColorEntry> Entries => entries;

        private void BuildCache()
        {
            if (_cache != null) return;

            _cache = new Dictionary<ColorType, Material>();

            for (int i = 0; i < entries.Count; i++)
            {
                MaterialColorEntry entry = entries[i];
                if (entry == null) continue;

                if (_cache.ContainsKey(entry.colorType))
                {
                    Debug.LogWarning($"[MaterialColorDatabaseSO] Duplicate ColorType: {entry.colorType} in {name}");
                    continue;
                }

                _cache.Add(entry.colorType, entry.material);
            }
        }

        public Material GetMaterial(ColorType colorType)
        {
            BuildCache();

            if (_cache.TryGetValue(colorType, out Material material))
                return material;

            Debug.LogWarning($"[MaterialColorDatabaseSO] Material not found for ColorType: {colorType} in {name}");
            return null;
        }

        public bool TryGetMaterial(ColorType colorType, out Material material)
        {
            BuildCache();
            return _cache.TryGetValue(colorType, out material);
        }

        public bool HasColor(ColorType colorType)
        {
            BuildCache();
            return _cache.ContainsKey(colorType);
        }

        public void ClearCache()
        {
            _cache = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _cache = null;
        }
#endif
    }
}