using System;
using ASTeams.SingleLine.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// What fills the sky around the route: clouds all the way up, and between two airports
    /// a little cloud island carrying airport kit (control tower, baggage train, hangar,
    /// terminal), so the sky reads as the airline's. Every piece is placed once, from the
    /// route, on the side away from it; a small pool of views is laid over the pieces in view.
    /// Each pooled piece is a holder placed here with its picture inside. The sky stays
    /// still: nothing here moves but the map itself.
    /// </summary>
    public sealed class RouteMapDecor : MonoBehaviour
    {
        [SerializeField] private RectTransform[] cloudHolders = new RectTransform[0];
        [SerializeField] private Image[] clouds = new Image[0];
        [SerializeField] private RectTransform[] islandHolders = new RectTransform[0];
        [SerializeField] private Image[] islands = new Image[0];
        [SerializeField] private Sprite[] cloudSprites = new Sprite[0];
        [SerializeField] private Sprite[] islandSprites = new Sprite[0];

        [SerializeField, Min(50f)] private float cloudEvery = 420f;

        [Tooltip("How far into a city's gates its island floats, as a share of them.")]
        [SerializeField, Range(0f, 1f)] private float islandAlong = 0.5f;

        private float[] cloudX;
        private float[] cloudY;
        private float[] islandX;
        private float[] islandY;
        private bool[] cloudUsed;
        private bool[] islandUsed;
        private Vector2 mapToLocal;

        public void Initialize(RouteLayout layout, int levelsPerCity, Vector2 toLocal)
        {
            mapToLocal = toLocal;
            cloudUsed = new bool[clouds.Length];
            islandUsed = new bool[islands.Length];
            float width = -toLocal.x * 2f;
            PlaceClouds(layout, width);
            PlaceIslands(layout, levelsPerCity, width);
        }

        public void ShowRange(float low, float high)
        {
            if (cloudY == null)
            {
                return;
            }

            Array.Clear(cloudUsed, 0, cloudUsed.Length);
            Array.Clear(islandUsed, 0, islandUsed.Length);

            for (int i = FirstAtOrAbove(cloudY, low); i < cloudY.Length && cloudY[i] <= high; i++)
            {
                int slot = i % clouds.Length;
                cloudUsed[slot] = true;
                clouds[slot].sprite = cloudSprites[i % cloudSprites.Length];
                Place(cloudHolders[slot], cloudX[i], cloudY[i]);
            }

            for (int i = FirstAtOrAbove(islandY, low); i < islandY.Length && islandY[i] <= high; i++)
            {
                int slot = i % islands.Length;
                islandUsed[slot] = true;
                islands[slot].sprite = islandSprites[i % islandSprites.Length];
                Place(islandHolders[slot], islandX[i], islandY[i]);
            }

            HideUnused(cloudHolders, cloudUsed);
            HideUnused(islandHolders, islandUsed);
        }

        /// <summary>Tints every cloud, for the theme the player wears.</summary>
        public void TintClouds(Color tint)
        {
            for (int i = 0; i < clouds.Length; i++)
            {
                clouds[i].color = tint;
            }
        }

        private void Place(RectTransform piece, float x, float y)
        {
            piece.anchoredPosition = new Vector2(x + mapToLocal.x, y + mapToLocal.y);

            if (!piece.gameObject.activeSelf)
            {
                piece.gameObject.SetActive(true);
            }
        }

        private static void HideUnused(RectTransform[] pieces, bool[] used)
        {
            for (int i = 0; i < pieces.Length; i++)
            {
                if (!used[i] && pieces[i].gameObject.activeSelf)
                {
                    pieces[i].gameObject.SetActive(false);
                }
            }
        }

        /// <summary>The side of the map away from the route at a height.</summary>
        private static float AwayFromRoute(RouteLayout layout, float y, float width, float inset)
        {
            int level = Mathf.Clamp(layout.FirstLevelAtOrAbove(y), 1, layout.LevelCount);
            return layout.LevelPosition(level).X < width / 2f ? width - inset : inset;
        }

        private void PlaceClouds(RouteLayout layout, float width)
        {
            int count = Mathf.Max(1, Mathf.CeilToInt(layout.Height / cloudEvery) + 2);
            cloudX = new float[count];
            cloudY = new float[count];

            for (int i = 0; i < count; i++)
            {
                float y = -cloudEvery + i * cloudEvery + (i * 71 % 90);
                cloudY[i] = y;
                cloudX[i] = AwayFromRoute(layout, y, width, 130f + (i * 43 % 60));
            }
        }

        private void PlaceIslands(RouteLayout layout, int levelsPerCity, float width)
        {
            int count = layout.HubCount;
            islandX = new float[count];
            islandY = new float[count];

            for (int city = 0; city < count; city++)
            {
                int level = city * levelsPerCity + Mathf.Clamp(Mathf.RoundToInt(levelsPerCity * islandAlong), 1, levelsPerCity);
                float y = layout.LevelPosition(level).Y;
                islandY[city] = y;
                islandX[city] = AwayFromRoute(layout, y, width, 190f);
            }
        }

        private static int FirstAtOrAbove(float[] sortedY, float y)
        {
            int low = 0;
            int high = sortedY.Length;

            while (low < high)
            {
                int mid = (low + high) / 2;

                if (sortedY[mid] < y)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid;
                }
            }

            return low;
        }

#if UNITY_EDITOR
        public void EditorLink(RectTransform[] linkedCloudHolders, Image[] linkedClouds, RectTransform[] linkedIslandHolders, Image[] linkedIslands,
            Sprite[] linkedCloudSprites, Sprite[] linkedIslandSprites)
        {
            cloudHolders = linkedCloudHolders;
            clouds = linkedClouds;
            islandHolders = linkedIslandHolders;
            islands = linkedIslands;
            cloudSprites = linkedCloudSprites;
            islandSprites = linkedIslandSprites;
        }
#endif
    }
}
