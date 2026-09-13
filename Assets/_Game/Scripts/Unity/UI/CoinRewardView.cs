using TMPro;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Shows what a finished level paid: "+40" lifts off the board and fades out.
    ///
    /// It listens to the event channel and holds no reference into gameplay, so the win
    /// screen the UI owns can replace it without touching the board. Until that screen
    /// exists this is the only place a player sees the reward arrive, because the coins
    /// are written straight into the profile with nothing on the HUD bound to them.
    ///
    /// One label, reused. Nothing is created or destroyed while the game runs.
    /// </summary>
    public sealed class CoinRewardView : MonoBehaviour
    {
        [Tooltip("Raises OnCoinsAwarded once the coins are already saved.")]
        [SerializeField] private GameplayEventChannelSO eventChannel;

        [SerializeField] private TMP_Text label;

        [Tooltip("The board camera, so the number can be parked above the board whatever " +
                 "size the board is. A fixed offset would sit on top of a tall board and " +
                 "float off the top of a short one.")]
        [SerializeField] private Camera boardCamera;

        [Tooltip("Height up the screen the number starts at, 0 bottom and 1 top.")]
        [SerializeField, Range(0.5f, 0.98f)] private float viewportHeight = 0.8f;

        [Tooltip("How far the number travels up, in board units.")]
        [SerializeField, Min(0f)] private float riseDistance = 0.9f;

        [SerializeField, Min(0.1f)] private float duration = 1.2f;

        [Tooltip("Share of the time spent fading. The number holds still first so it can be read.")]
        [SerializeField, Range(0.1f, 1f)] private float fadeShare = 0.55f;

        private Vector3 restPosition;
        private const float CameraDistance = 9f;
        private float elapsed;
        private bool isPlaying;
        private Color baseColour;

        private void Awake()
        {
            if (label == null)
            {
                return;
            }

            baseColour = label.color;
            label.enabled = false;
        }

        private void OnEnable()
        {
            if (eventChannel != null)
            {
                eventChannel.OnCoinsAwarded += HandleCoinsAwarded;
            }
        }

        private void OnDisable()
        {
            if (eventChannel != null)
            {
                eventChannel.OnCoinsAwarded -= HandleCoinsAwarded;
            }
        }

        private void HandleCoinsAwarded(int amount, long balance)
        {
            if (label == null || amount <= 0)
            {
                return;
            }

            // SetText with a format argument keeps the number off the managed heap, which
            // a plain "+" + amount would not.
            label.SetText("+{0}", amount);
            restPosition = RestPosition();
            label.transform.position = restPosition;
            label.color = baseColour;
            label.enabled = true;
            elapsed = 0f;
            isPlaying = true;
        }

        private void Update()
        {
            if (!isPlaying)
            {
                return;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            label.transform.position = restPosition + Vector3.up * (riseDistance * EaseOut(t));

            float fadeStart = 1f - fadeShare;
            Color colour = baseColour;
            colour.a = t <= fadeStart ? baseColour.a : Mathf.Lerp(baseColour.a, 0f, (t - fadeStart) / fadeShare);
            label.color = colour;

            if (t < 1f)
            {
                return;
            }

            isPlaying = false;
            label.enabled = false;
        }

        /// <summary>
        /// Above the board, over the background rather than over the squares: the number
        /// has to stay readable whatever hue the level is painted in.
        /// </summary>
        private Vector3 RestPosition()
        {
            if (boardCamera == null)
            {
                return label.transform.position;
            }

            return boardCamera.ViewportToWorldPoint(new Vector3(0.5f, viewportHeight, CameraDistance));
        }

        private static float EaseOut(float t)
        {
            float inverse = 1f - t;
            return 1f - inverse * inverse;
        }
    }
}
