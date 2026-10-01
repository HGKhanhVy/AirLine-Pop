using ASTeams.SingleLine.Core;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The airplane is the player's pointer. At rest it stands on the runway square at the
    /// front of the route. While the finger is down it lifts off and flies under the
    /// finger, nose along the way it is moving and banking into turns; when the finger
    /// lifts it glides back and lands on the front of the route again. When the board is
    /// won it takes off and leaves the screen.
    ///
    /// It only reads the input and the path and listens for level and state changes; it
    /// never touches the session, so the route stays the single source of truth. How the
    /// pose is drawn is up to the <see cref="AirplaneRig"/>.
    /// </summary>
    public sealed class AirplaneView : MonoBehaviour
    {
        [SerializeField] private PathView path;
        [SerializeField] private BoardInput input;
        [SerializeField] private AirplaneRig rig;

        [Tooltip("Keeps the airplane over the blocks while it follows the finger. Empty lets it fly anywhere.")]
        [SerializeField] private BoardView board;

        [Header("Height")]
        [Tooltip("Height above the block tops while parked on the runway.")]
        [SerializeField, Min(0f)] private float parkedHeight = 0.12f;

        [Tooltip("Height while flying under the finger.")]
        [SerializeField, Min(0f)] private float flightHeight = 0.8f;

        [Header("Flight")]
        [Tooltip("How quickly the airplane closes on the front of the path. Higher is tighter.")]
        [SerializeField, Min(1f)] private float followSharpness = 22f;

        [SerializeField, Min(30f)] private float turnSpeed = 900f;

        [Tooltip("Roll per degree per second of turning.")]
        [SerializeField, Min(0f)] private float bankPerTurnRate = 0.06f;

        [SerializeField, Range(0f, 80f)] private float maxBank = 40f;

        [SerializeField, Min(0.1f)] private float bankSharpness = 10f;

        [Header("Steering")]
        [Tooltip("How tightly the airplane sticks to the finger. Higher is tighter.")]
        [SerializeField, Min(1f)] private float steerSharpness = 26f;

        [Tooltip("Slower than this, in world units per second, the nose keeps its heading " +
                 "so a finger resting still does not spin the airplane.")]
        [SerializeField, Min(0f)] private float minHeadingSpeed = 0.6f;

        [Tooltip("Size while flying under the finger, relative to parked. Big enough for the " +
                 "wings to show around a fingertip.")]
        [SerializeField, Min(1f)] private float flightScale = 1.2f;

        [SerializeField, Min(0.1f)] private float climbSharpness = 9f;

        [Header("Hover")]
        [Tooltip("How far the airplane bobs up and down, in world units.")]
        [SerializeField, Range(0f, 0.2f)] private float hoverHeight = 0.04f;
        [SerializeField, Min(0.1f)] private float hoverPeriod = 1.8f;

        [Header("Take-off")]
        [SerializeField, Min(0.1f)] private float takeoffDuration = 0.9f;
        [SerializeField, Min(0f)] private float takeoffDistance = 10f;
        [SerializeField, Min(1f)] private float takeoffScale = 1.8f;
        [SerializeField, Min(0f)] private float takeoffHeight = 3f;

        // Nose up on screen until the first leg says otherwise.
        private const float DefaultHeading = 0f;

        private Vector3 position;
        private float heading = DefaultHeading;
        private float bank;
        private float altitude;
        private float hoverTime;
        private bool isVisible = true;
        private bool needsSnap = true;
        private bool hasFlownAway;
        private float takeoffElapsed = -1f;

        private void Awake()
        {
            SetVisible(false);
        }

        private void OnEnable()
        {
            GameplayEvents.OnLevelLoaded += HandleLevelLoaded;
            GameplayEvents.OnStateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            GameplayEvents.OnLevelLoaded -= HandleLevelLoaded;
            GameplayEvents.OnStateChanged -= HandleStateChanged;
        }

        private void HandleLevelLoaded(int levelNumber, string levelId, int difficulty, int totalCells)
        {
            ResetFlight();
        }

        private void HandleStateChanged(PathState previous, PathState current)
        {
            if (current == PathState.Won)
            {
                takeoffElapsed = 0f;
                return;
            }

            // Leaving the won state without a new level, such as a Restart during the
            // celebration, brings the airplane back to the route.
            if (previous == PathState.Won)
            {
                ResetFlight();
            }
        }

        private void ResetFlight()
        {
            takeoffElapsed = -1f;
            hasFlownAway = false;
            needsSnap = true;
            heading = DefaultHeading;
            bank = 0f;
            altitude = 0f;
        }

        private void LateUpdate()
        {
            float deltaTime = Time.deltaTime;
            hoverTime += deltaTime;

            if (takeoffElapsed >= 0f)
            {
                AdvanceTakeoff(deltaTime);
                return;
            }

            if (hasFlownAway || !path.HasTip)
            {
                SetVisible(false);
                needsSnap = true;
                altitude = 0f;
                return;
            }

            bool isSteering = input != null && input.IsSteering;

            if (isSteering)
            {
                // The finger may leave the board; the airplane stays over the blocks.
                Vector3 pointer = input.PointerWorldPosition;
                Steer(board != null ? board.ClampToCells(pointer) : pointer, deltaTime);
            }
            else
            {
                Follow(path.TipPosition, deltaTime);
            }

            altitude = Mathf.Lerp(altitude, isSteering ? 1f : 0f, 1f - Mathf.Exp(-climbSharpness * deltaTime));
            Apply(FlightScale(), FlightHeight());
            SetVisible(true);
        }

        private float FlightScale()
        {
            return Mathf.Lerp(1f, flightScale, altitude);
        }

        private float FlightHeight()
        {
            return Mathf.Lerp(parkedHeight, flightHeight, altitude);
        }

        /// <summary>Flies under the finger, nose along the way the airplane is actually moving.</summary>
        private void Steer(Vector3 pointer, float deltaTime)
        {
            if (TrySnap(pointer, heading))
            {
                return;
            }

            Vector3 previous = position;
            position = Vector3.Lerp(position, pointer, 1f - Mathf.Exp(-steerSharpness * deltaTime));

            Vector2 moved = position - previous;
            float targetHeading = heading;

            if (deltaTime > 0f && moved.magnitude / deltaTime > minHeadingSpeed)
            {
                targetHeading = HeadingOf(moved);
            }

            Turn(targetHeading, deltaTime);
        }

        /// <summary>Parked, or gliding back to land on the front of the route.</summary>
        private void Follow(Vector3 tip, float deltaTime)
        {
            float targetHeading = heading;

            if (path.TryGetTipDirection(out Vector2 direction))
            {
                targetHeading = HeadingOf(direction);
            }

            if (TrySnap(tip, targetHeading))
            {
                return;
            }

            position = Vector3.Lerp(position, tip, 1f - Mathf.Exp(-followSharpness * deltaTime));
            Turn(targetHeading, deltaTime);
        }

        /// <summary>Places the airplane outright the first frame it appears on a level.</summary>
        private bool TrySnap(Vector3 target, float targetHeading)
        {
            if (!needsSnap)
            {
                return false;
            }

            needsSnap = false;
            position = target;
            heading = targetHeading;
            bank = 0f;
            return true;
        }

        private void Turn(float targetHeading, float deltaTime)
        {
            float nextHeading = Mathf.MoveTowardsAngle(heading, targetHeading, turnSpeed * deltaTime);
            float turnRate = deltaTime > 0f ? Mathf.DeltaAngle(heading, nextHeading) / deltaTime : 0f;
            heading = nextHeading;

            // Turning left (positive rate) lowers the left wing.
            float targetBank = Mathf.Clamp(-turnRate * bankPerTurnRate, -maxBank, maxBank);
            bank = Mathf.Lerp(bank, targetBank, 1f - Mathf.Exp(-bankSharpness * deltaTime));
        }

        /// <summary>The model's nose points along +Y, so a heading of 0 faces up the screen.</summary>
        private static float HeadingOf(Vector2 direction)
        {
            return Mathf.Atan2(-direction.x, direction.y) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Accelerates along the current heading while climbing towards the camera, which
        /// reads as the model growing and its shadow falling away.
        /// </summary>
        private void AdvanceTakeoff(float deltaTime)
        {
            if (!isVisible)
            {
                takeoffElapsed = -1f;
                hasFlownAway = true;
                return;
            }

            takeoffElapsed += deltaTime;
            float t = Mathf.Clamp01(takeoffElapsed / takeoffDuration);
            float climb = t * t;

            float radians = heading * Mathf.Deg2Rad;
            var forward = new Vector3(-Mathf.Sin(radians), Mathf.Cos(radians), 0f);
            position += forward * (takeoffDistance * 2f * t * deltaTime / takeoffDuration);
            bank = Mathf.Lerp(bank, 0f, 1f - Mathf.Exp(-bankSharpness * deltaTime));

            Apply(Mathf.Lerp(FlightScale(), takeoffScale, climb), Mathf.Lerp(FlightHeight(), takeoffHeight, climb));

            if (t >= 1f)
            {
                takeoffElapsed = -1f;
                hasFlownAway = true;
                SetVisible(false);
            }
        }

        private void Apply(float scale, float height)
        {
            float bob = hoverHeight * Mathf.Sin(hoverTime * (2f * Mathf.PI / hoverPeriod));
            rig.Pose(position, heading, bank, scale, height + bob);
        }

        private void SetVisible(bool visible)
        {
            if (isVisible == visible)
            {
                return;
            }

            isVisible = visible;
            rig.SetVisible(visible);
        }
    }
}
