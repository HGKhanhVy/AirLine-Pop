using UnityEditor;
using UnityEngine;

namespace ASTeams.SingleLine.Unity.EditorTools
{
    /// <summary>
    /// Gives the board its cloud, beacon and overpass markers: a handful made once in the
    /// gameplay prefab and placed by <see cref="CellMarkMarkers"/> on each level. Safe to rerun.
    /// </summary>
    public static class CellMarkInstaller
    {
        public const string CloudArt = "Assets/_Game/Art/Flat/mark_cloud.png";
        public const string BeaconArt = "Assets/_Game/Art/Flat/mark_beacon.png";
        public const string BridgeArt = "Assets/_Game/Art/Flat/mark_bridge.png";

        private const string PrefabPath = "Assets/_Game/Prefabs/SingleLineGameplay.prefab";
        private const string HolderName = "CellMarks";

        // A storm mixing clouds and beacons on a big board, with an overpass or two, stays under this.
        private const int MarkerCount = 12;

        [MenuItem("Tools/AirLine Pop/Install Cell Mark Markers")]
        public static void InstallFromMenu()
        {
            Debug.Log(Install());
        }

        public static string Install()
        {
            Sprite cloud = SpriteImport.Import(CloudArt, 256f);
            Sprite beacon = SpriteImport.Import(BeaconArt, 256f);
            Sprite bridge = SpriteImport.Import(BridgeArt, 256f);
            GameObject contents = PrefabUtility.LoadPrefabContents(PrefabPath);

            try
            {
                var board = contents.GetComponentInChildren<BoardView>(true);

                if (board == null)
                {
                    return "No board in " + PrefabPath + "; cell marks not installed.";
                }

                Transform old = board.transform.Find(HolderName);

                if (old != null)
                {
                    Object.DestroyImmediate(old.gameObject);
                }

                var holder = new GameObject(HolderName);
                holder.transform.SetParent(board.transform, false);
                var markers = new SpriteRenderer[MarkerCount];

                for (int i = 0; i < MarkerCount; i++)
                {
                    var marker = new GameObject("Mark " + (i + 1));
                    marker.transform.SetParent(holder.transform, false);
                    markers[i] = marker.AddComponent<SpriteRenderer>();
                    markers[i].sprite = cloud;
                    markers[i].sortingOrder = BoardSortingOrder.RuleMarker;
                    marker.SetActive(false);
                }

                holder.AddComponent<CellMarkMarkers>().EditorLink(board, markers, cloud, beacon, bridge);
                PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
                return "Cell mark markers installed.";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }
    }
}
