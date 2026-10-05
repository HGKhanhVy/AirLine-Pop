using ASTeams.SingleLine.Core;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The stamp book: one country to a page (a long country runs over several), turned with
    /// the arrows at its foot or a swipe; pages slide across as they turn. It opens on the
    /// page of the country being flown.
    /// A city's stamp is collected with its last flight there, and opens its postcard.
    ///
    /// Every page and stamp is built into the scene, so opening and turning only refresh
    /// them. What is collected is read from flight progress, never saved separately.
    /// </summary>
    public sealed class StampAlbumView : MonoBehaviour
    {
        [SerializeField] private ModalPanel modal;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text summaryLabel;
        [SerializeField] private TMP_Text pageLabel;
        [SerializeField] private PageSwipe swipe;

        [SerializeField] private RectTransform[] pages = new RectTransform[0];

        [SerializeField] private TMP_Text[] headings = new TMP_Text[0];

        [Tooltip("The country (route region page) each book page belongs to.")]
        [SerializeField] private int[] pageCountry = new int[0];

        [SerializeField] private StampCellView[] cells = new StampCellView[0];
        [SerializeField] private PostcardViewerView viewer;
        [SerializeField, Min(0.01f)] private float turnSeconds = 0.3f;

        private DestinationCatalogSO catalog;
        private IVipFlightStore vipFlights;
        private int nextFlight = 1;
        private int shownPage;
        private Sequence turn;

        public void Initialize(DestinationCatalogSO destinations, int nextFlightNumber, IVipFlightStore vipFlightStore)
        {
            catalog = destinations;
            vipFlights = vipFlightStore;
            nextFlight = nextFlightNumber;
        }

        private void OnEnable()
        {
            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(Close);
            previousButton.onClick.AddListener(TurnBack);
            nextButton.onClick.AddListener(TurnOn);
            swipe.OnSwiped += HandleSwiped;

            for (int i = 0; i < cells.Length; i++)
            {
                cells[i].OnOpened += viewer.Show;
            }
        }

        private void OnDisable()
        {
            openButton.onClick.RemoveListener(Open);
            closeButton.onClick.RemoveListener(Close);
            previousButton.onClick.RemoveListener(TurnBack);
            nextButton.onClick.RemoveListener(TurnOn);
            swipe.OnSwiped -= HandleSwiped;
            FinishTurn();

            for (int i = 0; i < cells.Length; i++)
            {
                cells[i].OnOpened -= viewer.Show;
            }
        }

        private void Open()
        {
            if (catalog == null || pages.Length == 0)
            {
                return;
            }

            Refresh();
            FinishTurn();
            ShowPage(FirstPageOf(catalog.Regions.PageOf(catalog.Schedule.DestinationOf(nextFlight))));
            modal.Show();
        }

        private void Close()
        {
            modal.Hide();
        }

        private void TurnBack()
        {
            Turn(shownPage - 1);
        }

        private void TurnOn()
        {
            Turn(shownPage + 1);
        }

        private void HandleSwiped(int direction)
        {
            Turn(shownPage + direction);
        }

        /// <summary>
        /// The shown page slides out to one side as the new one slides in from the other:
        /// turning on, everything moves left; turning back, right.
        /// </summary>
        private void Turn(int page)
        {
            if (page < 0 || page >= pages.Length || page == shownPage || (turn != null && turn.IsActive()))
            {
                return;
            }

            RectTransform leaving = pages[shownPage];
            RectTransform coming = pages[page];
            float width = leaving.rect.width;
            float direction = page > shownPage ? 1f : -1f;

            coming.gameObject.SetActive(true);
            coming.anchoredPosition = new Vector2(direction * width, 0f);
            ShowLabels(page);

            turn = DOTween.Sequence().SetUpdate(true)
                .Append(leaving.DOAnchorPosX(-direction * width, turnSeconds).SetEase(Ease.OutCubic))
                .Join(coming.DOAnchorPosX(0f, turnSeconds).SetEase(Ease.OutCubic))
                .OnComplete(() => ShowPage(page));
        }

        private void FinishTurn()
        {
            turn?.Kill();
            turn = null;
        }

        private void ShowPage(int page)
        {
            shownPage = Mathf.Clamp(page, 0, pages.Length - 1);

            for (int i = 0; i < pages.Length; i++)
            {
                pages[i].anchoredPosition = Vector2.zero;
                pages[i].gameObject.SetActive(i == shownPage);
            }

            ShowLabels(shownPage);
        }

        private void ShowLabels(int page)
        {
            shownPage = page;

            previousButton.interactable = shownPage > 0;
            nextButton.interactable = shownPage < pages.Length - 1;
            pageLabel.text = Localization.Format("album.page", shownPage + 1, pages.Length);
        }

        private int FirstPageOf(int country)
        {
            for (int i = 0; i < pageCountry.Length; i++)
            {
                if (pageCountry[i] == country)
                {
                    return i;
                }
            }

            return 0;
        }

        /// <summary>True when the city's flights include a VIP flight the player has landed.</summary>
        private bool HasVipSeal(DestinationSchedule schedule, int destination)
        {
            if (vipFlights == null)
            {
                return false;
            }

            for (int flight = schedule.FirstFlight(destination); flight <= schedule.LastFlight(destination); flight++)
            {
                if (vipFlights.HasFlown(flight))
                {
                    return true;
                }
            }

            return false;
        }

        private void Refresh()
        {
            DestinationSchedule schedule = catalog.Schedule;
            RouteRegions regions = catalog.Regions;
            int shown = Mathf.Min(cells.Length, catalog.Count);
            int collected = 0;

            for (int i = 0; i < shown; i++)
            {
                bool isCollected = schedule.HasPostcard(i, nextFlight);
                collected += isCollected ? 1 : 0;
                cells[i].Show(catalog.Get(i), schedule.StampsCollected(i, nextFlight), schedule.FlightsPerDestination, isCollected,
                    HasVipSeal(schedule, i));
            }

            for (int page = 0; page < headings.Length; page++)
            {
                int country = pageCountry[page];
                int first = regions.FirstDestination(country);
                int count = regions.DestinationCount(country);
                int got = 0;

                for (int i = 0; i < count; i++)
                {
                    got += schedule.HasPostcard(first + i, nextFlight) ? 1 : 0;
                }

                // Each country shows how much of it is still to collect.
                headings[page].text = Localization.Format("album.country", catalog.Get(first).Region, got, count);
            }

            summaryLabel.SetText(Localization.Get("album.summary"), collected, shown);
        }

#if UNITY_EDITOR
        public void EditorLink(ModalPanel linkedModal, Button linkedOpen, Button linkedClose, Button linkedPrevious, Button linkedNext,
            TMP_Text linkedSummary, TMP_Text linkedPage, PageSwipe linkedSwipe, RectTransform[] linkedPages,
            TMP_Text[] linkedHeadings, int[] linkedPageCountry, StampCellView[] linkedCells, PostcardViewerView linkedViewer)
        {
            modal = linkedModal;
            openButton = linkedOpen;
            closeButton = linkedClose;
            previousButton = linkedPrevious;
            nextButton = linkedNext;
            summaryLabel = linkedSummary;
            pageLabel = linkedPage;
            swipe = linkedSwipe;
            pages = linkedPages;
            headings = linkedHeadings;
            pageCountry = linkedPageCountry;
            cells = linkedCells;
            viewer = linkedViewer;
        }
#endif
    }
}
