using System;
using ASTeams.SingleLine.Core;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The flight route map tab: the whole campaign as the airline's route, gate after gate,
    /// landing at each city's airport, a chapter per country joined by international legs.
    /// Flown gates can be replayed, the current one played; the airline's plane sits at the
    /// player's gate and, after wins, flies on along the route while the map follows it.
    ///
    /// The map is far taller than the screen, so a small pool of gate, airport and chapter
    /// views is laid over whatever is in view; each gate always lands on the same pooled
    /// view, so scrolling neither instantiates nor redraws what has not changed.
    /// </summary>
    public sealed class RouteMapView : MonoBehaviour
    {
        [SerializeField] private RouteMapConfigSO config;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private RectTransform content;
        [SerializeField] private RouteLineGraphic line;
        [SerializeField] private LevelNodeView[] nodes = new LevelNodeView[0];
        [SerializeField] private HubCardView[] hubs = new HubCardView[0];
        [SerializeField] private ChapterCardView[] chapters = new ChapterCardView[0];
        [SerializeField] private RoutePlaneView plane;
        [SerializeField] private RouteMapDecor decor;
        [SerializeField] private AirlinerFlyby flyby;

        [Tooltip("How far short of the next level the plane waits, along the route.")]
        [SerializeField, Min(0f)] private float planeLead = 125f;

        [Tooltip("Where on screen, from the bottom, the next level is brought to.")]
        [SerializeField, Range(0f, 1f)] private float focusHeight = 0.4f;

        [SerializeField, Min(0f)] private float flightDelay = 0.35f;
        [SerializeField, Min(0f)] private float viewMargin = 260f;

        private const int PathCapacity = 512;

        private readonly RouteVector[] routePath = new RouteVector[PathCapacity];
        private readonly Vector2[] flightPath = new Vector2[PathCapacity];

        private DestinationCatalogSO catalog;
        private IRouteMapMemory memory;
        private RouteLayout layout;
        private Vector2 mapToLocal;
        private int nextLevel = 1;
        private bool[] nodeUsed;
        private bool[] hubUsed;

        // Space wide cards keep from the map's sides.
        private const float SideMargin = 16f;
        private int landingCity = -1;
        private bool[] chapterUsed;

        public event Action<int> OnLevelChosen;

        public void Initialize(DestinationCatalogSO destinations, int nextLevelNumber, IRouteMapMemory routeMemory)
        {
            catalog = destinations;
            memory = routeMemory;
            nextLevel = Mathf.Max(1, nextLevelNumber);
            BuildLayout();
        }

        private void OnEnable()
        {
            scroll.onValueChanged.AddListener(HandleScrolled);
            Localization.Service.OnLanguageChanged += HandleLanguageChanged;

            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i].OnTapped += HandleLevelTapped;
            }

        }

        private void OnDisable()
        {
            scroll.onValueChanged.RemoveListener(HandleScrolled);
            Localization.Service.OnLanguageChanged -= HandleLanguageChanged;
            content.DOKill();

            for (int i = 0; i < nodes.Length; i++)
            {
                nodes[i].OnTapped -= HandleLevelTapped;
            }

        }

        /// <summary>
        /// Called when the tab comes into view: brings the next level on screen and, if wins
        /// were earned since the last visit, flies the plane on from where it was.
        /// </summary>
        public void Show()
        {
            if (layout == null)
            {
                return;
            }

            int lastShown = memory.LastShownLevel;
            bool isFlying = lastShown > 0 && lastShown < nextLevel;
            int from = isFlying ? lastShown : nextLevel;

            line.SetFlown(layout.LevelDistance(Mathf.Min(from, layout.LevelCount)));
            FocusOn(PlaneDistance(from));
            Refresh();

            if (isFlying)
            {
                FlyPlane(from, nextLevel);
            }
            else
            {
                PlacePlane(nextLevel);
            }

            memory.Remember(nextLevel);

            if (flyby != null)
            {
                flyby.Play();
            }
        }

        /// <summary>Called when another tab takes over: nothing keeps animating out of sight.</summary>
        public void Hide()
        {
            if (flyby != null)
            {
                flyby.Stop();
            }
        }

        private void BuildLayout()
        {
            if (catalog == null || catalog.Count == 0)
            {
                return;
            }

            RouteRegions regions = catalog.Regions;
            var levelsPerChapter = new int[regions.PageCount];

            for (int page = 0; page < levelsPerChapter.Length; page++)
            {
                levelsPerChapter[page] = regions.DestinationCount(page) * catalog.FlightsPerDestination;
            }

            layout = new RouteLayout(config.ToSettings(), levelsPerChapter, catalog.FlightsPerDestination);
            content.sizeDelta = new Vector2(config.Width, layout.Height + config.Padding * 2f);
            mapToLocal = new Vector2(-config.Width / 2f, config.Padding);
            nodeUsed = new bool[nodes.Length];
            hubUsed = new bool[hubs.Length];
            chapterUsed = new bool[chapters.Length];

            line.SetRoute(layout, mapToLocal, 0f);

            if (decor != null)
            {
                decor.Initialize(layout, catalog.FlightsPerDestination, mapToLocal);
            }
        }

        /// <summary>Redraws what is on screen, after its look has changed.</summary>
        public void Repaint()
        {
            Refresh();
        }

        private void HandleScrolled(Vector2 _)
        {
            Refresh();
        }

        /// <summary>Lays the pooled views over whatever part of the map is on screen.</summary>
        private void Refresh()
        {
            if (layout == null)
            {
                return;
            }

            float scale = FitWidth();
            float viewportHeight = scroll.viewport.rect.height / scale;
            float low = -content.anchoredPosition.y / scale - mapToLocal.y - viewMargin;
            float high = low + viewportHeight + viewMargin * 2f;

            line.ShowRange(low, high);

            Array.Clear(nodeUsed, 0, nodeUsed.Length);
            Array.Clear(hubUsed, 0, hubUsed.Length);
            Array.Clear(chapterUsed, 0, chapterUsed.Length);

            for (int level = layout.FirstLevelAtOrAbove(low); level <= layout.LevelCount; level++)
            {
                RouteVector position = layout.LevelPosition(level);

                if (position.Y > high)
                {
                    break;
                }

                ShowStop(level, position);
            }

            ShowHubs(low, high);
            ShowChapters(low, high);
            HideUnused();

            if (decor != null)
            {
                decor.ShowRange(low, high);
            }
        }

        private void ShowStop(int level, RouteVector position)
        {
            int slot = level % nodes.Length;
            nodeUsed[slot] = true;
            Activate(nodes[slot].gameObject);
            nodes[slot].Show(level, StateOf(level), ToLocal(position));
        }

        private void ShowHubs(float low, float high)
        {
            DestinationSchedule schedule = catalog.Schedule;
            int current = schedule.DestinationOf(nextLevel);

            for (int city = layout.FirstHubAtOrAbove(low); city < layout.HubCount; city++)
            {
                RouteVector position = layout.HubPosition(city);

                if (position.Y > high)
                {
                    break;
                }

                // A city just reached waits for the plane to land before it shows its stamp.
                RouteStopState state = schedule.HasPostcard(city, nextLevel) && city != landingCity
                    ? RouteStopState.Flown
                    : city == current || city == landingCity ? RouteStopState.Current : RouteStopState.Locked;

                int slot = city % hubs.Length;
                hubUsed[slot] = true;
                Activate(hubs[slot].gameObject);
                hubs[slot].Show(city, catalog.Get(city), schedule.StampsCollected(city, nextLevel), schedule.FlightsPerDestination, state,
                    KeepInside(ToLocal(position), (RectTransform)hubs[slot].transform));
            }
        }

        private void ShowChapters(float low, float high)
        {
            for (int chapter = 0; chapter < layout.ChapterMarks.Count; chapter++)
            {
                RouteVector mark = layout.ChapterMarks[chapter];

                if (mark.Y < low || mark.Y > high)
                {
                    continue;
                }

                int slot = chapter % chapters.Length;
                chapterUsed[slot] = true;
                Activate(chapters[slot].gameObject);
                chapters[slot].Show(chapter + 1, catalog.GetPage(chapter), KeepInside(ToLocal(mark), (RectTransform)chapters[slot].transform));
            }
        }

        private void HideUnused()
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                if (!nodeUsed[i] && nodes[i].gameObject.activeSelf)
                {
                    nodes[i].gameObject.SetActive(false);
                }
            }

            for (int i = 0; i < hubs.Length; i++)
            {
                if (!hubUsed[i] && hubs[i].gameObject.activeSelf)
                {
                    hubs[i].gameObject.SetActive(false);
                }
            }

            for (int i = 0; i < chapters.Length; i++)
            {
                if (!chapterUsed[i] && chapters[i].gameObject.activeSelf)
                {
                    chapters[i].gameObject.SetActive(false);
                }
            }
        }

        private static void Activate(GameObject view)
        {
            if (!view.activeSelf)
            {
                view.SetActive(true);
            }
        }

        private RouteStopState StateOf(int level)
        {
            if (level < nextLevel)
            {
                return RouteStopState.Flown;
            }

            return level == nextLevel ? RouteStopState.Current : RouteStopState.Locked;
        }

        /// <summary>Where along the route the plane waits before a level: a little short of it.</summary>
        private float PlaneDistance(int level)
        {
            int clamped = Mathf.Min(level, layout.LevelCount);
            float distance = layout.LevelDistance(clamped);
            return level > layout.LevelCount ? distance : Mathf.Max(0f, distance - planeLead);
        }

        private void PlacePlane(int level)
        {
            float distance = PlaneDistance(level);
            plane.Place(ToLocal(layout.PointAt(distance)), Heading(distance));
        }

        private void FlyPlane(int fromLevel, int toLevel)
        {
            float from = PlaneDistance(fromLevel);
            float to = PlaneDistance(toLevel);
            int count = layout.PathBetween(from, to, routePath);

            if (count < 2)
            {
                PlacePlane(toLevel);
                return;
            }

            for (int i = 0; i < count; i++)
            {
                flightPath[i] = ToLocal(routePath[i]);
            }

            float landed = layout.LevelDistance(Mathf.Min(toLevel, layout.LevelCount));
            int flights = catalog.FlightsPerDestination;
            landingCity = (toLevel - 1) / flights > (fromLevel - 1) / flights ? (toLevel - 1) / flights - 1 : -1;
            Refresh();
            float duration = plane.Fly(flightPath, count, flightDelay, () => Land(landed));
            content.DOKill();
            content.DOAnchorPosY(FocusY(to), duration).SetDelay(flightDelay).SetEase(Ease.InOutSine).SetUpdate(true);
        }

        /// <summary>The plane is down: the route is inked up to it and the city it passed gets its stamp.</summary>
        private void Land(float landed)
        {
            line.SetFlown(landed);
            int arrived = landingCity;
            landingCity = -1;
            Refresh();

            if (arrived < 0)
            {
                return;
            }

            HubCardView hub = hubs[arrived % hubs.Length];

            if (hub.gameObject.activeSelf && hub.City == arrived)
            {
                hub.PlayArrival();
            }
        }

        private Vector2 Heading(float distance)
        {
            RouteVector ahead = layout.PointAt(distance + 6f);
            RouteVector behind = layout.PointAt(Mathf.Max(0f, distance - 6f));
            return new Vector2(ahead.X - behind.X, ahead.Y - behind.Y);
        }

        private void FocusOn(float distance)
        {
            content.DOKill();
            scroll.StopMovement();
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, FocusY(distance));
        }

        /// <summary>The content offset that puts a point of the route at the focus height.</summary>
        private float FocusY(float distance)
        {
            float scale = FitWidth();
            float viewportHeight = scroll.viewport.rect.height;
            float localY = (layout.PointAt(distance).Y + mapToLocal.y) * scale;
            float lowest = -(content.sizeDelta.y * scale - viewportHeight);
            return Mathf.Clamp(-(localY - viewportHeight * focusHeight), Mathf.Min(0f, lowest), 0f);
        }

        /// <summary>
        /// Shrinks the whole map evenly when the screen is narrower than the map was laid out
        /// for (tall phones), so no gate or airport card is cut off at the sides. Returns the scale.
        /// </summary>
        private float FitWidth()
        {
            float viewportWidth = scroll.viewport.rect.width;
            float scale = viewportWidth > 0f ? Mathf.Min(1f, viewportWidth / config.Width) : 1f;

            if (!Mathf.Approximately(content.localScale.x, scale))
            {
                content.localScale = new Vector3(scale, scale, 1f);
            }

            return scale;
        }

        /// <summary>
        /// Slides a wide card in from the map's sides when its stop lies near an edge, so the
        /// card is never cut off; it still covers the stop, which sits closer in than its half width.
        /// </summary>
        private Vector2 KeepInside(Vector2 local, RectTransform card)
        {
            float limit = config.Width / 2f - card.rect.width / 2f - SideMargin;
            local.x = limit <= 0f ? 0f : Mathf.Clamp(local.x, -limit, limit);
            return local;
        }

        private Vector2 ToLocal(RouteVector point)
        {
            return new Vector2(point.X + mapToLocal.x, point.Y + mapToLocal.y);
        }

        private void HandleLevelTapped(int level)
        {
            if (level >= 1 && level <= nextLevel)
            {
                OnLevelChosen?.Invoke(level);
            }
        }

        private void HandleLanguageChanged()
        {
            for (int i = 0; i < hubs.Length; i++)
            {
                hubs[i].Invalidate();
            }

            for (int i = 0; i < chapters.Length; i++)
            {
                chapters[i].Invalidate();
            }

            Refresh();
        }

#if UNITY_EDITOR
        public void EditorLink(RouteMapConfigSO linkedConfig, ScrollRect linkedScroll, RectTransform linkedContent,
            RouteLineGraphic linkedLine, LevelNodeView[] linkedNodes, HubCardView[] linkedHubs, ChapterCardView[] linkedChapters,
            RoutePlaneView linkedPlane, RouteMapDecor linkedDecor, AirlinerFlyby linkedFlyby)
        {
            flyby = linkedFlyby;
            config = linkedConfig;
            scroll = linkedScroll;
            content = linkedContent;
            line = linkedLine;
            nodes = linkedNodes;
            hubs = linkedHubs;
            chapters = linkedChapters;
            plane = linkedPlane;
            decor = linkedDecor;
        }
#endif
    }
}
