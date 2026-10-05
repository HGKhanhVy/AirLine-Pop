using UnityEditor;
using UnityEngine;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Gives the board its rule markers: one runway and a handful of wind arrows, made once
    /// in the gameplay prefab and placed by <see cref="BoardRuleMarkers"/> on each level.
    /// Safe to rerun.
    /// </summary>
    public static class BoardRuleInstaller
    {
        private const string PrefabPath = "Assets/_Game/Prefabs/SingleLineGameplay.prefab";
        private const string HolderName = "RuleMarkers";
        private const int GustCount = 4;

        [MenuItem("Tools/AirLine Pop/Install Board Rule Markers")]
        public static void InstallFromMenu()
        {
            Debug.Log(Install());
        }

        public static string Install()
        {
            Sprite runwayArt = SpriteImport.Import(RuleIntroBuilder.RunwayArt, 256f);
            Sprite windArt = SpriteImport.Import(RuleIntroBuilder.WindArt, 256f);
            GameObject contents = PrefabUtility.LoadPrefabContents(PrefabPath);

            try
            {
                var board = contents.GetComponentInChildren<BoardView>(true);

                if (board == null)
                {
                    return "No board in " + PrefabPath + "; rule markers not installed.";
                }

                Transform old = board.transform.Find(HolderName);

                if (old != null)
                {
                    Object.DestroyImmediate(old.gameObject);
                }

                var holder = new GameObject(HolderName);
                holder.transform.SetParent(board.transform, false);

                SpriteRenderer runway = Marker("Runway", holder.transform, runwayArt);
                var gusts = new SpriteRenderer[GustCount];

                for (int i = 0; i < GustCount; i++)
                {
                    gusts[i] = Marker("Wind " + (i + 1), holder.transform, windArt);
                }

                holder.AddComponent<BoardRuleMarkers>().EditorLink(board, runway, gusts);
                PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
                return "Board rule markers installed.";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static SpriteRenderer Marker(string name, Transform parent, Sprite sprite)
        {
            var marker = new GameObject(name);
            marker.transform.SetParent(parent, false);
            SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = BoardSortingOrder.RuleMarker;
            marker.SetActive(false);
            return renderer;
        }
    }
}
