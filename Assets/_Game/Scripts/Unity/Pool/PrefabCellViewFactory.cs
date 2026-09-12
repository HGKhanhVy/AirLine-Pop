using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    public sealed class PrefabCellViewFactory : ICellViewFactory
    {
        private readonly CellView prefab;
        private readonly Transform parent;

        public PrefabCellViewFactory(CellView prefab, Transform parent)
        {
            this.prefab = prefab;
            this.parent = parent;
        }

        public CellView Create()
        {
            return Object.Instantiate(prefab, parent, false);
        }
    }
}
