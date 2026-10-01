using ASTeams.SingleLine.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The postcard album: every destination's card, earned or still being flown for.
    ///
    /// Its slots are built into the scene, one per destination, so opening it only
    /// refreshes them. What is earned is read from flight progress, not saved separately.
    /// </summary>
    public sealed class PostcardAlbumView : MonoBehaviour
    {
        [SerializeField] private ModalPanel modal;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text summaryLabel;
        [SerializeField] private PostcardCellView[] cells = new PostcardCellView[0];
        [SerializeField] private PostcardViewerView viewer;

        private DestinationCatalogSO catalog;
        private int nextFlight = 1;

        public void Initialize(DestinationCatalogSO destinations, int nextFlightNumber)
        {
            catalog = destinations;
            nextFlight = nextFlightNumber;
        }

        private void OnEnable()
        {
            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(Close);

            for (int i = 0; i < cells.Length; i++)
            {
                cells[i].OnOpened += viewer.Show;
            }
        }

        private void OnDisable()
        {
            openButton.onClick.RemoveListener(Open);
            closeButton.onClick.RemoveListener(Close);

            for (int i = 0; i < cells.Length; i++)
            {
                cells[i].OnOpened -= viewer.Show;
            }
        }

        private void Open()
        {
            Refresh();
            modal.Show();
        }

        private void Close()
        {
            modal.Hide();
        }

        private void Refresh()
        {
            if (catalog == null)
            {
                return;
            }

            DestinationSchedule schedule = catalog.Schedule;
            int shown = Mathf.Min(cells.Length, catalog.Count);
            int collected = 0;

            for (int i = 0; i < shown; i++)
            {
                bool isCollected = schedule.HasPostcard(i, nextFlight);
                collected += isCollected ? 1 : 0;
                cells[i].Show(catalog.Get(i), schedule.StampsCollected(i, nextFlight), schedule.FlightsPerDestination, isCollected);
            }

            summaryLabel.text = Localization.Format("album.summary", collected, shown);
        }

#if UNITY_EDITOR
        public void EditorLink(ModalPanel linkedModal, Button linkedOpen, Button linkedClose, TMP_Text linkedSummary,
            PostcardCellView[] linkedCells, PostcardViewerView linkedViewer)
        {
            viewer = linkedViewer;
            modal = linkedModal;
            openButton = linkedOpen;
            closeButton = linkedClose;
            summaryLabel = linkedSummary;
            cells = linkedCells;
        }
#endif
    }
}
