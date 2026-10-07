using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Gives the flat tile its goal star, the mark that pops up on the square a level is
    /// finished on. The flat tile build calls this; the menu adds it to an existing tile
    /// without rebuilding the board. Safe to rerun.
    /// </summary>
    public static class GoalStarInstaller
    {
        private const string CellPrefabPath = "Assets/_Game/Prefabs/CellFlat.prefab";
        private const string StarPath = "Assets/_Game/Art/Flat/goal_star.png";
        private const string HolderName = "Goal Star";
        private const float TilePixelsPerUnit = 256f;

        [MenuItem("Tools/AirLine Pop/Add Goal Star To Tile")]
        public static void InstallFromMenu()
        {
            Debug.Log(Install());
        }

        public static string Install()
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(CellPrefabPath);

            try
            {
                var view = contents.GetComponent<CellView>();
                Transform sizeRoot = contents.transform.Find("VisualSizeRoot");

                if (view == null || sizeRoot == null)
                {
                    return CellPrefabPath + " has no CellView or VisualSizeRoot; goal star not added.";
                }

                AddTo(sizeRoot, view);
                PrefabUtility.SaveAsPrefabAsset(contents, CellPrefabPath);
                return "Goal star added to " + CellPrefabPath + ".";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        public static void AddTo(Transform sizeRoot, CellView view)
        {
            Sprite sprite = SpriteImport.Import(StarPath, TilePixelsPerUnit);
            Transform holder = AssetWriter.FindOrCreateChild(sizeRoot, HolderName);

            // Nudged up like the start dot, onto the middle of the face above the tile's lip.
            holder.localPosition = new Vector3(0f, 0.04f, 0f);
            Transform mark = AssetWriter.FindOrCreateChild(holder, "Star");
            var star = AssetWriter.GetOrAdd<SpriteRenderer>(mark.gameObject);
            star.sprite = sprite;
            star.sortingOrder = BoardSortingOrder.GoalStar;
            star.shadowCastingMode = ShadowCastingMode.Off;
            star.receiveShadows = false;
            mark.gameObject.SetActive(false);

            var goal = AssetWriter.GetOrAdd<GoalStarView>(holder.gameObject);
            goal.EditorLink(star);

            var so = new SerializedObject(view);
            so.FindProperty("goalStar").objectReferenceValue = goal;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
