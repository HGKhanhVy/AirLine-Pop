using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    public sealed class CellView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Tooltip("The square's 3D block, when it has one. It takes the same colour as the sprite face.")]
        [SerializeField] private MeshCellFace meshFace;

        [Tooltip("Optional. The grass and pebbles dressing the top of the 3D block.")]
        [SerializeField] private CellDecor decor;
        [SerializeField] private Transform visualSizeRoot;
        [SerializeField] private Animator blockAnimator;
        [SerializeField] private Transform animatedInner;
        [SerializeField] private SpriteRenderer startDotRenderer;
        [SerializeField] private SpriteRenderer startPulseRenderer;

        [Tooltip("The block's own goal marker, Fill/EndBlockDot: the star this level has " +
                 "to finish on. Shipped switched off.")]
        [SerializeField] private GameObject goalMarker;

        [SerializeField] private SpriteRenderer goalStarRenderer;

        [SerializeField] private SpriteRenderer goalHaloRenderer;
        [SerializeField] private bool useConfiguredSprite;
        [SerializeField, Min(0.01f)] private float sourceVisualSize = 0.9f;
        [SerializeField, Min(0.1f)] private float startPulseDuration = 1.1f;
        [SerializeField, Min(1f)] private float startPulseScale = 2.2f;
        [SerializeField, Range(0f, 1f)] private float startPulseAlpha = 0.24f;
        [SerializeField, Min(0f)] private float idleReminderDelay = 3f;
        [SerializeField, Min(0.1f)] private float idleAnimationInterval = 4f;

        [Header("Connect")]
        [Tooltip("How long the press takes. GDD 10 asks for 80 to 120 ms, which measured out " +
                 "at a single frame on the floor of the press: correct on paper and invisible " +
                 "in the hand. The press is what the player is supposed to feel, so it is " +
                 "given long enough to be seen and still clears before the next square.")]
        [SerializeField, Min(0.02f)] private float connectDuration = 0.26f;

        [Tooltip("How far the square sinks under the finger, as a share of its size.")]
        [SerializeField, Range(0f, 0.4f)] private float connectPressDepth = 0.2f;

        [Tooltip("How far it overshoots on the way back up. Kept small on purpose: a button " +
                 "gives one beat, and a taller overshoot reads as a second bounce.")]
        [SerializeField, Range(0f, 0.4f)] private float connectPopHeight = 0.05f;

        [Tooltip("Share of the move spent sinking. The rest is the way back. Down quickly, " +
                 "up slowly: that split is what separates a button from a wobble.")]
        [SerializeField, Range(0.1f, 0.6f)] private float connectPressShare = 0.3f;

        [Header("Connect flash")]
        [Tooltip("Fill/light on the block art. The reference game spreads this white sheet " +
                 "from the middle of a square out over its whole face the moment the path " +
                 "lands on it, then takes it away again.")]
        [SerializeField] private SpriteRenderer connectFlash;

        [SerializeField, Min(0.02f)] private float flashDuration = 0.28f;
        [SerializeField, Range(0f, 1f)] private float flashPeakAlpha = 0.8f;

        [Tooltip("How wide the sheet starts, as a share of the face. The reference opens at " +
                 "just under half and ends level with the block.")]
        [SerializeField, Range(0f, 1f)] private float flashStartScale = 0.4f;

        [SerializeField, Range(0.1f, 2f)] private float flashEndScale = 1f;

        [Tooltip("Share of the flash spent brightening. The rest is the fade out.")]
        [SerializeField, Range(0.05f, 0.8f)] private float flashRiseShare = 0.18f;

        private static readonly int StartIdleState = Animator.StringToHash("StartIdle");

        // Fades the goal marker in from nothing and pops it: alpha 0 to 1 in 0.08 s, scale
        // 0.38 to 0.6 and back. The reference plays it the moment a level is finished.
        private static readonly int StarState = Animator.StringToHash("TutorialFillSquareStar");

        public int CellIndex { get; private set; }

        public Color Color => faceColor;

        public Vector3 BaseLocalPosition { get; private set; }

        private Color fillFrom;
        private Color fillTo;
        private float fillDuration;
        private Color faceColor = Color.white;
        private float fillLeft;
        private float connectLeft;
        private bool isGoalRevealing;
        private StartCueEffect startCue;
        private ConnectFlashEffect connectFlashEffect;

        // Built on first use rather than only in Awake: a square placed under an inactive
        // parent is handed its level before Awake has run.
        private StartCueEffect StartCue => startCue ??= new StartCueEffect(
            startDotRenderer,
            startPulseRenderer,
            new StartCueSettings(startPulseDuration, startPulseScale, startPulseAlpha,
                idleReminderDelay, idleAnimationInterval));

        private ConnectFlashEffect ConnectFlash => connectFlashEffect ??= new ConnectFlashEffect(
            connectFlash,
            new ConnectFlashSettings(flashDuration, flashPeakAlpha, flashStartScale,
                flashEndScale, flashRiseShare));

        private void Awake()
        {
            // Record the markers' authored scales before anything animates them.
            _ = StartCue;

            // The block art numbers its own children from 0 to 10, which leaves every dot
            // under the path. The marks saying where the path starts and where it stopped
            // have to stay readable, so they move above it here rather than by editing the
            // shared art.
            if (goalMarker != null)
            {
                goalMarker.SetActive(false);
            }

            LiftAboveConnector(startDotRenderer);
            LiftAboveConnector(startPulseRenderer);
            LiftAboveConnector(goalHaloRenderer);

            // The star has to clear its own ring, not just the path: sharing one order
            // leaves which of the two wins up to draw order, and the ring buried it.
            if (goalStarRenderer != null)
            {
                goalStarRenderer.sortingOrder = BoardSortingOrder.Marker + 1;
            }

            if (blockAnimator != null)
            {
                blockAnimator.enabled = false;
            }

            // The sheet ships collapsed; nothing shows it until a path arrives.
            ConnectFlash.Hide();
        }

        private void ApplyFaceColor(Color color)
        {
            faceColor = color;
            spriteRenderer.color = color;

            if (meshFace != null)
            {
                meshFace.SetColor(color);
            }
        }

        private static void LiftAboveConnector(SpriteRenderer renderer)
        {
            if (renderer != null)
            {
                renderer.sortingOrder = BoardSortingOrder.Marker;
            }
        }

        public void Bind(SpriteRenderer renderer)
        {
            spriteRenderer = renderer;
        }

        public void Place(int cellIndex, Vector3 localPosition, float size, Sprite sprite, Color color)
        {
            CellIndex = cellIndex;
            BaseLocalPosition = localPosition;
            transform.localPosition = localPosition;
            transform.localScale = Vector3.one;
            fillLeft = 0f;
            connectLeft = 0f;
            ConnectFlash.Hide();
            ClearStartMarker();
            HideGoalMarker();
            ResetAnimatedVisual();

            if (useConfiguredSprite && sprite != null)
            {
                spriteRenderer.sprite = sprite;
            }

            ApplyFaceColor(color);

            if (decor != null)
            {
                decor.Apply(cellIndex);
            }

            if (visualSizeRoot != null)
            {
                float scale = size / sourceVisualSize;
                visualSizeRoot.localScale = new Vector3(scale, scale, 1f);
            }
            else
            {
                spriteRenderer.drawMode = SpriteDrawMode.Sliced;
                spriteRenderer.size = new Vector2(size, size);
            }
        }

        public void SetColor(Color color)
        {
            fillLeft = 0f;
            ApplyFaceColor(color);
        }

        /// <summary>A square the route runs across is cleared of its dressing, like ground levelled for a runway.</summary>
        public void SetVisited(bool visited)
        {
            if (decor != null)
            {
                decor.SetCleared(visited);
            }
        }

        public void FillTo(Color color, float duration)
        {
            if (duration <= 0f)
            {
                SetColor(color);
                return;
            }

            fillFrom = faceColor;
            fillTo = color;
            fillDuration = duration;
            fillLeft = duration;
        }

        /// <summary>
        /// The press and bounce a square makes when the path reaches it.
        ///
        /// The shape is taken from the reference game's own selection clip, which sinks
        /// the block and then lets it overshoot its size before settling. The clip itself
        /// cannot be played here: it stores absolute scales belonging to the source
        /// prefab's hierarchy, while these squares are resized at runtime to fit the
        /// board, and at 0.62 s it would still be moving two cells after the finger.
        ///
        /// One beat, not a bounce: the square goes down under the finger and comes back,
        /// the way a button does. The white sheet washing over its face is the same clip's
        /// other half, and it rides along here rather than being a second call, because
        /// press and flare are one event to the player.
        /// </summary>
        public void PlayConnect()
        {
            if (connectDuration <= 0f)
            {
                return;
            }

            connectLeft = connectDuration;
            ConnectFlash.Play();
        }

        /// <summary>
        /// Shows the white dot and the ring that swells out of it. Colours come from the
        /// theme rather than from the prefab, so one asset still decides how the board
        /// reads and a second theme needs no second prefab.
        /// </summary>
        public void ShowStartCue(Color dotColor, Color haloColor)
        {
            if (!StartCue.Show(dotColor, haloColor))
            {
                return;
            }

            transform.localPosition = BaseLocalPosition;

            if (blockAnimator != null)
            {
                blockAnimator.enabled = false;
            }
        }

        /// <summary>
        /// Stops the waiting cue but leaves the dot where it is. In the reference game the
        /// start square wears its dot for the whole level; only the ring and the reminder
        /// shake belong to the moment before the player has touched anything.
        /// </summary>
        public void HideStartCue()
        {
            StartCue.Hide();
            transform.localPosition = BaseLocalPosition;

            if (blockAnimator != null)
            {
                blockAnimator.enabled = false;
            }

            if (animatedInner != null)
            {
                animatedInner.localRotation = Quaternion.identity;
            }
        }

        /// <summary>Takes the dot away too, for a square going back to the pool.</summary>
        public void ClearStartMarker()
        {
            HideStartCue();
            StartCue.Clear();
        }

        public bool Advance(float deltaTime)
        {
            bool running = StartCue.IsVisible;

            if (fillLeft > 0f)
            {
                fillLeft -= deltaTime;

                if (fillLeft <= 0f)
                {
                    ApplyFaceColor(fillTo);
                }
                else
                {
                    float t = 1f - (fillLeft / fillDuration);
                    ApplyFaceColor(Color.Lerp(fillFrom, fillTo, t * t * (3f - 2f * t)));
                    running = true;
                }
            }

            if (connectLeft > 0f)
            {
                connectLeft -= deltaTime;

                if (connectLeft <= 0f)
                {
                    transform.localScale = Vector3.one;
                }
                else
                {
                    float t = 1f - (connectLeft / connectDuration);
                    transform.localScale = Vector3.one * ConnectScale(t);
                    running = true;
                }
            }

            if (ConnectFlash.Advance(deltaTime))
            {
                running = true;
            }

            if (StartCue.IsVisible && StartCue.Advance(deltaTime))
            {
                PlayStartReminder();
            }

            return running;
        }

        /// <summary>
        /// The nudge a square gives when the player has stopped on it: the same idle the
        /// start square plays, run once.
        ///
        /// It used to be the selection clip played back to back, which read as a square
        /// bouncing on the spot and flaring its light over and over. One idle now and
        /// again is what the reference does, and it says "your turn" without shouting.
        /// </summary>
        public void PlayIdleNudge()
        {
            if (blockAnimator == null || isGoalRevealing)
            {
                return;
            }

            blockAnimator.enabled = true;
            blockAnimator.Play(StartIdleState, 0, 0f);
        }

        /// <summary>
        /// Reveals the star on the square that finished the level. The marker and the clip
        /// that brings it in are both the reference block's own, left switched off until
        /// the path is actually complete.
        /// </summary>
        public void PlayGoalReveal()
        {
            if (goalMarker == null || blockAnimator == null)
            {
                return;
            }

            goalMarker.SetActive(true);
            blockAnimator.enabled = true;
            blockAnimator.Play(StarState, 0, 0f);
            isGoalRevealing = true;
        }

        public void HideGoalMarker()
        {
            isGoalRevealing = false;

            if (goalMarker != null)
            {
                goalMarker.SetActive(false);
            }

            if (goalHaloRenderer != null)
            {
                goalHaloRenderer.enabled = true;
            }
        }

        /// <summary>Length of the idle clip, so the board knows when it may nudge again.</summary>
        public const float IdleNudgeLength = 4f;

        public void StopHeadCue()
        {
            // The winning square starts its star on the same frame the path stops being
            // drawn. Resetting the animator here would cut the pop off before its first
            // frame, so the square that just won keeps its clip.
            if (isGoalRevealing)
            {
                return;
            }

            ResetAnimatedVisual();
        }

        /// <summary>
        /// Puts back whatever a half finished clip left behind. A square can be released to
        /// the pool mid flare, and the next level would then open with a lit square in a
        /// place nothing is happening.
        ///
        /// The resting pose is the idle clip's first frame, not the flare's last one. Both
        /// look the same on the parts the flare animates, but the states write defaults, so
        /// sampling the idle clip also restores what it does *not* touch: the faint light
        /// under the face, which the flare ends on zero. Sampling the flare instead left
        /// every square unlit until its first idle beat three seconds later, and the square
        /// then appeared to switch on by itself.
        /// </summary>
        private void ResetAnimatedVisual()
        {
            if (blockAnimator == null)
            {
                return;
            }

            blockAnimator.enabled = true;
            blockAnimator.Play(StartIdleState, 0, 0f);
            blockAnimator.Update(0f);
            blockAnimator.enabled = false;
        }

        /// <summary>
        /// Sinks to <see cref="connectPressDepth"/> below its size, then eases back with a
        /// bump past it. Both ends land exactly on 1, so nothing is left scaled when the
        /// move finishes and a square can be released to the pool at any moment.
        /// </summary>
        private float ConnectScale(float t)
        {
            if (t < connectPressShare)
            {
                float press = t / connectPressShare;
                return 1f - connectPressDepth * Mathf.Sin(press * Mathf.PI * 0.5f);
            }

            float r = (t - connectPressShare) / (1f - connectPressShare);
            float ease = 1f - (1f - r) * (1f - r);
            float settled = Mathf.Lerp(1f - connectPressDepth, 1f, ease);
            return settled + connectPopHeight * Mathf.Sin(r * Mathf.PI);
        }

        private void PlayStartReminder()
        {
            if (blockAnimator == null)
            {
                return;
            }

            blockAnimator.enabled = true;
            blockAnimator.Play(StartIdleState, 0, 0f);
        }
    }
}
