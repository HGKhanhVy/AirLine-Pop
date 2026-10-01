using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// One cat waiting on the board for the airplane.
    ///
    /// It breathes and blinks while it waits, hops on the spot when the plane is next to
    /// it, and jumps up into the plane when the route reaches its square. After an undo it
    /// pops back onto its seat. Driven by <see cref="PassengerBoardView"/>'s single update,
    /// so a board of cats costs one Update call, not one per cat.
    /// </summary>
    public sealed class PassengerCatView : MonoBehaviour
    {
        private enum Phase
        {
            Hidden,
            Appearing,
            Waiting,
            Boarding,
        }

        [SerializeField] private SpriteRenderer body;
        [SerializeField] private SpriteRenderer shadow;

        [Tooltip("Height of the cat in world units. A board square is 1 across.")]
        [SerializeField, Min(0.1f)] private float size = 0.7f;

        [Tooltip("Where the cat's feet sit below the middle of its square.")]
        [SerializeField] private float seatDrop = 0.3f;

        [Header("Waiting")]
        [SerializeField, Min(0.1f)] private float breathPeriod = 1.7f;
        [SerializeField, Range(0f, 0.2f)] private float breathDepth = 0.04f;
        [SerializeField] private Vector2 blinkGap = new Vector2(2.2f, 4.8f);
        [SerializeField, Min(0.02f)] private float blinkLength = 0.14f;

        [Tooltip("Height of the little hops a cat makes while the plane is next to it.")]
        [SerializeField, Min(0f)] private float excitedHop = 0.1f;
        [SerializeField, Min(0.1f)] private float excitedPeriod = 0.42f;

        [Header("Appear and board")]
        [SerializeField, Min(0.05f)] private float appearDuration = 0.32f;
        [SerializeField, Min(0.05f)] private float boardDuration = 0.45f;
        [SerializeField, Min(0f)] private float boardHopHeight = 0.55f;

        private PassengerLook look;
        private Phase phase = Phase.Hidden;
        private float elapsed;
        private float delay;
        private float breathOffset;
        private float blinkAt;
        private bool isExcited;
        private float excitedTime;

        public bool IsActive => phase != Phase.Hidden;

        private void Awake()
        {
            shadow.sortingOrder = BoardSortingOrder.PassengerShadow;
            SetVisible(false);
        }

        /// <summary>Seats the cat on a square, popping in after <paramref name="appearDelay"/> seconds.</summary>
        public void Seat(Vector3 squareCentre, PassengerLook passengerLook, float appearDelay, float breathPhase)
        {
            look = passengerLook;
            transform.position = squareCentre + new Vector3(0f, -seatDrop, -0.02f);
            breathOffset = breathPhase;
            isExcited = false;
            StartAppearing(appearDelay);
        }

        /// <summary>The route reached this square: the cat jumps into the plane.</summary>
        public void Board()
        {
            if (phase == Phase.Hidden || phase == Phase.Boarding)
            {
                return;
            }

            phase = Phase.Boarding;
            elapsed = 0f;
            body.sprite = look.Happy;
            body.sortingOrder = BoardSortingOrder.PassengerBoarding;
            shadow.enabled = false;
        }

        /// <summary>The route left this square again: the cat pops back onto its seat.</summary>
        public void ReturnToSeat()
        {
            if (phase == Phase.Waiting || phase == Phase.Appearing)
            {
                return;
            }

            StartAppearing(0f);
        }

        public void Hide()
        {
            phase = Phase.Hidden;
            SetVisible(false);
        }

        public void SetExcited(bool excited)
        {
            if (excited && !isExcited)
            {
                excitedTime = 0f;
            }

            isExcited = excited;
        }

        public void Tick(float deltaTime)
        {
            switch (phase)
            {
                case Phase.Appearing:
                    TickAppearing(deltaTime);
                    break;

                case Phase.Waiting:
                    TickWaiting(deltaTime);
                    break;

                case Phase.Boarding:
                    TickBoarding(deltaTime);
                    break;
            }
        }

        private void StartAppearing(float appearDelay)
        {
            phase = Phase.Appearing;
            elapsed = 0f;
            delay = appearDelay;
            body.sprite = look.Sitting;
            body.sortingOrder = BoardSortingOrder.Passenger;
            Place(0f, 0f, 1f);
            SetVisible(false);
        }

        private void TickAppearing(float deltaTime)
        {
            if (delay > 0f)
            {
                delay -= deltaTime;
                return;
            }

            SetVisible(true);
            elapsed += deltaTime;
            float t = Mathf.Clamp01(elapsed / appearDuration);

            // Out-back: overshoots a little and settles, so the cat lands with a bounce.
            const float overshoot = 1.7f;
            float u = t - 1f;
            float grow = 1f + (overshoot + 1f) * u * u * u + overshoot * u * u;
            Place(0f, grow, 1f);
            shadow.transform.localScale = Vector3.one * size * Mathf.Clamp01(t * 1.5f);

            if (t >= 1f)
            {
                phase = Phase.Waiting;
                elapsed = 0f;
                blinkAt = Random.Range(blinkGap.x, blinkGap.y);
            }
        }

        private void TickWaiting(float deltaTime)
        {
            elapsed += deltaTime;

            float breath = Mathf.Sin((elapsed / breathPeriod + breathOffset) * Mathf.PI * 2f) * 0.5f + 0.5f;
            float lift = 0f;
            float squash = 1f;

            if (isExcited)
            {
                excitedTime += deltaTime;
                float hop = Mathf.Abs(Mathf.Sin(excitedTime / excitedPeriod * Mathf.PI));
                lift = hop * excitedHop;
                squash = 1f - (1f - hop) * 0.08f;
            }

            Place(lift, 1f, squash * (1f + breath * breathDepth));

            // Blink: a short swap to the eyes-shut sprite, then a fresh random gap. An
            // excited cat keeps its happy squint for as long as the plane is beside it.
            if (elapsed >= blinkAt + blinkLength)
            {
                blinkAt = elapsed + Random.Range(blinkGap.x, blinkGap.y);
            }

            bool eyesShut = isExcited || elapsed >= blinkAt;
            body.sprite = eyesShut ? look.Happy : look.Sitting;
        }

        private void TickBoarding(float deltaTime)
        {
            elapsed += deltaTime;
            float t = Mathf.Clamp01(elapsed / boardDuration);
            float arc = Mathf.Sin(t * Mathf.PI) * boardHopHeight;

            // Full size for most of the jump, then shrinks away into the cabin.
            float shrink = t < 0.55f ? 1f + Mathf.Sin(t / 0.55f * Mathf.PI) * 0.15f : Mathf.Lerp(1f, 0f, (t - 0.55f) / 0.45f);
            Place(arc, shrink, 1f);

            if (t >= 1f)
            {
                phase = Phase.Hidden;
                SetVisible(false);
                shadow.enabled = false;
            }
        }

        /// <summary>Moves only the body; the seat and its shadow stay put on the square.</summary>
        private void Place(float lift, float scale, float stretch)
        {
            body.transform.localPosition = new Vector3(0f, lift, 0f);
            body.transform.localScale = new Vector3(size * scale, size * scale * stretch, 1f);
        }

        private void SetVisible(bool isVisible)
        {
            body.enabled = isVisible;
            shadow.enabled = isVisible && phase != Phase.Boarding;
        }

#if UNITY_EDITOR
        public void EditorLink(SpriteRenderer linkedBody, SpriteRenderer linkedShadow)
        {
            body = linkedBody;
            shadow = linkedShadow;
        }
#endif
    }
}
