using ASTeams.SingleLine.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Flashes a red edge around the screen when the path walks into a dead end.
    ///
    /// The art is the reference game's own edge vignette, which is already drawn for a
    /// portrait screen and fades to nothing well before the middle, so the board stays
    /// readable underneath it.
    ///
    /// It listens to the state event rather than to the board, which is why it can live on
    /// the HUD canvas: the knock the camera takes and this flash are two answers to the
    /// same moment, each in its own layer.
    /// </summary>
    public sealed class StuckFlashView : MonoBehaviour
    {
        [SerializeField] private Image border;

        [Tooltip("How strong the edge gets at the peak of the flash.")]
        [SerializeField, Range(0f, 1f)] private float peakAlpha = 0.85f;

        [SerializeField, Min(0.01f)] private float riseDuration = 0.12f;
        [SerializeField, Min(0.01f)] private float fallDuration = 0.45f;

        private float elapsed;
        private bool isPlaying;

        private void Awake()
        {
            SetAlpha(0f);
        }

        private void OnEnable()
        {
            GameplayEvents.OnStateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            GameplayEvents.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(PathState previous, PathState current)
        {
            if (current != PathState.Stuck)
            {
                // Undoing out of the dead end clears it at once; a flash still fading over
                // a board the player has already fixed would be lying.
                isPlaying = false;
                SetAlpha(0f);
                return;
            }

            elapsed = 0f;
            isPlaying = true;
        }

        private void Update()
        {
            if (!isPlaying || border == null)
            {
                return;
            }

            elapsed += Time.deltaTime;

            if (elapsed < riseDuration)
            {
                SetAlpha(peakAlpha * (elapsed / riseDuration));
                return;
            }

            float fall = (elapsed - riseDuration) / fallDuration;

            if (fall >= 1f)
            {
                isPlaying = false;
                SetAlpha(0f);
                return;
            }

            SetAlpha(Mathf.Lerp(peakAlpha, 0f, fall));
        }

        private void SetAlpha(float alpha)
        {
            if (border == null)
            {
                return;
            }

            Color colour = border.color;
            colour.a = alpha;
            border.color = colour;
            border.enabled = alpha > 0f;
        }
    }
}
