using System;
using System.Collections.Generic;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Keeps the cats that live in one space (room or airport) on stage and runs their
    /// brains from a single update. A cat taken out of the room is parked inactive and
    /// reused when it comes back, so toggling cats never instantiates twice.
    /// </summary>
    public sealed class CatRoster : MonoBehaviour
    {
        [SerializeField] private WalkArea area;
        [SerializeField] private CatBehaviourConfigSO behaviour;
        [SerializeField] private Transform container;
        [SerializeField, Min(0.1f)] private float catScale = 1.6f;

        [Tooltip("Upper bound of cats shown here; the airport may show fewer than the room.")]
        [SerializeField, Min(1)] private int maxCats = 4;

        [Tooltip("The camera looking at this space. Flat cats turn to face it.")]
        [SerializeField] private Transform viewer;

        [Tooltip("Places cats go to on purpose here: the yarn, the carrier, the seats.")]
        [SerializeField] private CatSpot[] spots = new CatSpot[0];

        private readonly Dictionary<string, CatBrain> brains = new Dictionary<string, CatBrain>();
        private readonly List<CatBrain> active = new List<CatBrain>();
        private readonly List<CatMotor> motors = new List<CatMotor>();

        private ICatCollectionService collection;
        private CatCatalogSO catalog;

        public IReadOnlyList<CatBrain> Active => active;

        public event Action OnRosterChanged;

        public void Initialize(ICatCollectionService collectionService, CatCatalogSO catCatalog)
        {
            collection = collectionService;
            catalog = catCatalog;
            collection.OnCollectionChanged += Refresh;
            Refresh();
        }

        private void OnDestroy()
        {
            if (collection != null)
            {
                collection.OnCollectionChanged -= Refresh;
            }
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            for (int i = 0; i < active.Count; i++)
            {
                active[i].Tick(deltaTime, motors);
            }
        }

        /// <summary>The brain driving the cat that owns <paramref name="hit"/>, if any.</summary>
        public CatBrain FindByCollider(Collider hit)
        {
            for (int i = 0; i < active.Count; i++)
            {
                if (active[i].View.TapCollider == hit)
                {
                    return active[i];
                }
            }

            return null;
        }

        private void Refresh()
        {
            IReadOnlyList<string> visible = collection.VisibleCatIds;
            active.Clear();
            motors.Clear();

            foreach (KeyValuePair<string, CatBrain> pair in brains)
            {
                pair.Value.View.gameObject.SetActive(false);
            }

            for (int i = 0; i < visible.Count && active.Count < maxCats; i++)
            {
                CatBrain brain = GetOrSpawn(visible[i]);

                if (brain == null)
                {
                    continue;
                }

                brain.View.gameObject.SetActive(true);
                active.Add(brain);
                motors.Add(brain.Motor);
            }

            OnRosterChanged?.Invoke();
        }

#if UNITY_EDITOR
        public void EditorLink(WalkArea linkedArea, CatBehaviourConfigSO linkedBehaviour, Transform linkedContainer, float scale, int limit,
            Transform linkedViewer = null)
        {
            area = linkedArea;
            behaviour = linkedBehaviour;
            container = linkedContainer;
            catScale = scale;
            maxCats = limit;
            viewer = linkedViewer;
        }

        public void EditorLinkSpots(CatSpot[] linkedSpots)
        {
            spots = linkedSpots;
        }
#endif

        private CatBrain GetOrSpawn(string catId)
        {
            if (brains.TryGetValue(catId, out CatBrain existing))
            {
                return existing;
            }

            CatBreedSO breed = catalog.Find(catId);

            if (breed == null || breed.Prefab == null)
            {
                return null;
            }

            Vector3 spawn = area.RandomPoint();
            Quaternion facing = Quaternion.Euler(0f, UnityEngine.Random.Range(140f, 220f), 0f);
            CatView view = Instantiate(breed.Prefab, spawn, facing, container);
            view.transform.localScale = Vector3.one * catScale;
            view.Bind(catId);
            view.SetViewer(viewer);

            var motor = new CatMotor(view, behaviour, area);
            var brain = new CatBrain(view, motor, behaviour, area, spots);
            brains.Add(catId, brain);
            return brain;
        }
    }
}
