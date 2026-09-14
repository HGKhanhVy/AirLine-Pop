using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The board's reactions: a shake and a pulse when the player runs out of moves, and
    /// a wave along the finished path when they win.
    ///
    /// GDD 3.4 is deliberate that being stuck is not a loss. The feedback says so: the
    /// board wobbles and the cells still to cover pulse once to point at what is left,
    /// and nothing is reset or taken away.
    /// </summary>
    public sealed class BoardFeedback : MonoBehaviour
    {
        [SerializeField] private BoardView boardView;

        [Tooltip("The camera to knock when the path runs into a dead end. Knocking the " +
                 "whole view reads better than wobbling the board inside a still frame.")]
        [SerializeField] private Transform cameraShakeTarget;

        [Header("Stuck")]
        [SerializeField, Min(0f)] private float shakeDuration = 0.32f;
        [SerializeField, Min(0f)] private float shakeStrength = 0.16f;
        [SerializeField, Min(0)] private int shakeVibrato = 14;

        [Header("Hint")]
        [Tooltip("How far a hinted square swells. Large enough to find, small enough not " +
                 "to look like the board is breathing.")]
        [SerializeField, Min(1f)] private float hintScale = 1.12f;

        [Tooltip("How long the highlight stays up. GDD 7.1 asks for 2.5 s or until the player moves.")]
        [SerializeField, Min(0.1f)] private float hintHold = 2.5f;

        [Tooltip("Length of one swell and back.")]
        [SerializeField, Min(0.05f)] private float hintPulse = 0.5f;

        [Tooltip("Delay between one hinted cell and the next. Zero keeps the three on one " +
                 "rhythm; staggering them made each pulse at its own rate and read as noise.")]
        [SerializeField, Min(0f)] private float hintStagger;

        [Header("Win")]
        [SerializeField, Min(1f)] private float winScale = 1.16f;
        [Tooltip("Gap between one square swelling and the next. GDD 10 gives the whole " +
                 "wave 0.8 to 1.2 s, and the dust rides the same step so the two agree.")]
        [SerializeField, Min(0.01f)] private float winStepDelay = 0.05f;
        [SerializeField, Min(0.01f)] private float winDuration = 0.22f;

        [Header("Invalid move")]
        [Tooltip("How far the square nudges, in cells. A cell is at most 18% of the screen, " +
                 "so 0.014 peaks near the 2 to 3 px GDD 4 asks for at a 1080p reference.")]
        [SerializeField, Min(0f)] private float invalidShake = 0.014f;

        [SerializeField, Min(0.01f)] private float invalidDuration = 0.08f;

        [Tooltip("Least time between two nudges, so a finger held against a wall does not buzz.")]
        [SerializeField, Min(0f)] private float invalidCooldown = 0.18f;

        [Header("Win dust")]
        [SerializeField] private CellDustView dust;

        [Header("Particles")]
        [Tooltip("Plays the reference game's bursts. Leave empty to run without them.")]
        [SerializeField] private PooledEffectPlayer effectPlayer;

        private IEffectPlayer effects;
        private Sequence running;
        private Tween invalidTween;
        private CellView invalidCell;
        private Vector3 cameraShakeBase;
        private bool isShakingCamera;
        private float invalidAllowedAt;

        private Sequence hintSequence;
        private readonly List<CellView> hintCells = new List<CellView>(4);
        private Coroutine dustWave;
        private WaitForSeconds dustStep;

        // The controller hands out a list it reuses, so what the wave needs is copied here
        // before the coroutine outlives the call that started it. Colours are read now
        // too: by the time a cell's turn comes its square may already be mid-pulse.
        private readonly List<Vector3> burstPositions = new List<Vector3>(96);
        private readonly List<Color> burstColors = new List<Color>(96);

        private void Awake()
        {
            effects = effectPlayer;

            dustStep = new WaitForSeconds(winStepDelay);
        }

        private void OnDisable()
        {
            Stop();
        }

        /// <summary>
        /// Knocks the camera when the path walks into a dead end. Nothing on the board
        /// moves: the squares still to cover used to pulse as well, and between that and
        /// the knock the moment read as two separate alarms.
        /// </summary>
        public void PlayStuck()
        {
            Stop();

            running = DOTween.Sequence();

            if (cameraShakeTarget != null)
            {
                // The camera is framed once per level rather than every frame, so it is
                // safe to move here as long as it is put back exactly where it was.
                cameraShakeBase = cameraShakeTarget.localPosition;
                isShakingCamera = true;
                running.Append(cameraShakeTarget
                    .DOShakePosition(shakeDuration, shakeStrength, shakeVibrato, 90f, false, true)
                    .OnKill(RestoreCamera));
            }

            running.SetUpdate(isIndependentUpdate: false);
        }

        /// <summary>A short wave along the path, in the order the player drew it.</summary>
        public void PlayWin(IReadOnlyList<int> pathInOrder)
        {
            Stop();

            if (pathInOrder == null || pathInOrder.Count == 0)
            {
                return;
            }

            running = DOTween.Sequence();
            burstPositions.Clear();
            burstColors.Clear();

            for (int step = 0; step < pathInOrder.Count; step++)
            {
                int index = pathInOrder[step];
                CellView cell = boardView.GetCellView(index);

                if (cell == null)
                {
                    continue;
                }

                running.Insert(step * winStepDelay, Pulse(cell.transform, winScale, winDuration));
                burstPositions.Add(boardView.GetCellWorldPosition(index));
                burstColors.Add(cell.Color);
            }

            running.SetUpdate(isIndependentUpdate: false);

            if (burstPositions.Count > 0)
            {
                // One burst for the whole board rather than one per square: the dust wave
                // already runs along the path, and ninety confetti bursts would bury it.
                effects?.Play(GameplayEffect.PathCompleted,
                    burstPositions[burstPositions.Count - 1], burstColors[burstColors.Count - 1]);
            }

            if (dust != null && isActiveAndEnabled)
            {
                dustWave = StartCoroutine(ScatterDust());
            }
        }

        /// <summary>
        /// A puff off each cell in the order the player drew it, so the celebration runs
        /// along the path with the wave rather than covering the board all at once.
        /// </summary>
        private IEnumerator ScatterDust()
        {
            for (int i = 0; i < burstPositions.Count; i++)
            {
                dust.Burst(burstPositions[i], burstColors[i]);
                yield return dustStep;
            }

            dustWave = null;
        }

        /// <summary>
        /// A short nudge on the cell the player is standing on when a move is refused.
        ///
        /// GDD 4 puts a number on it: 2 to 3 px for 80 ms. Without it a refused move looks
        /// exactly like a dropped frame, and the player blames the game rather than
        /// reading the board. Rate limited because a finger held against a wall refuses a
        /// move every frame, and GDD 4 asks for the error not to be spammed.
        /// </summary>
        public void PlayInvalid(int cell)
        {
            if (Time.time < invalidAllowedAt)
            {
                return;
            }

            CellView view = boardView.GetCellView(cell);

            if (view == null)
            {
                return;
            }

            invalidAllowedAt = Time.time + invalidCooldown;
            StopInvalid();

            invalidCell = view;
            effects?.Play(GameplayEffect.CellRejected, boardView.GetCellWorldPosition(cell), view.Color);
            invalidTween = view.transform
                .DOShakePosition(invalidDuration, invalidShake, 18, 90f, false, true)
                .SetUpdate(isIndependentUpdate: false);
        }

        /// <summary>
        /// Puts the camera back where the shake picked it up, and only then. The base is
        /// captured when a shake starts, so restoring without one would move the camera to
        /// an unset position and take the whole board off screen with it.
        /// </summary>
        private void RestoreCamera()
        {
            if (!isShakingCamera || cameraShakeTarget == null)
            {
                return;
            }

            cameraShakeTarget.localPosition = cameraShakeBase;
            isShakingCamera = false;
        }

        private void StopInvalid()
        {
            if (invalidTween != null && invalidTween.IsActive())
            {
                invalidTween.Kill();
            }

            invalidTween = null;

            // Squares are pooled, so one left nudged aside would be handed to the next
            // board off centre.
            if (invalidCell != null)
            {
                invalidCell.transform.localPosition = invalidCell.BaseLocalPosition;
            }

            invalidCell = null;
        }

        /// <summary>
        /// Draws attention to one cell without playing it. A hint points, it does not
        /// move: the player still has to make the move themselves.
        /// </summary>
        public void PlayHint(IReadOnlyList<int> cells)
        {
            StopHint();

            if (cells == null || cells.Count == 0)
            {
                return;
            }

            // An even number of yoyo legs, so every cell ends back at its own size even
            // if the highlight runs its full course. Every cell uses the same leg length,
            // which is what keeps the group on one rhythm however it is staggered.
            int legs = Mathf.Max(1, Mathf.RoundToInt(hintHold / hintPulse)) * 2;
            float legDuration = hintPulse * 0.5f;
            hintSequence = DOTween.Sequence();

            for (int i = 0; i < cells.Count; i++)
            {
                CellView view = boardView.GetCellView(cells[i]);

                if (view == null)
                {
                    continue;
                }

                hintCells.Add(view);

                // Staggering them makes the three read as an order to walk rather than as
                // three separate suggestions.
                float delay = Mathf.Min(i * hintStagger, hintHold * 0.5f);
                hintSequence.Insert(delay, view.transform
                    .DOScale(hintScale, legDuration)
                    .SetLoops(legs, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine));
            }

            hintSequence.SetUpdate(isIndependentUpdate: false);
            hintSequence.OnComplete(StopHint);
        }

        /// <summary>
        /// Drops the highlight. GDD 7.1 ends it the moment the player acts, because a hint
        /// still pulsing over a path they have moved on from is pointing at the past.
        /// </summary>
        public void StopHint()
        {
            if (hintSequence != null && hintSequence.IsActive())
            {
                hintSequence.Kill();
            }

            hintSequence = null;

            for (int i = 0; i < hintCells.Count; i++)
            {
                if (hintCells[i] != null)
                {
                    hintCells[i].transform.localScale = Vector3.one;
                }
            }

            hintCells.Clear();
        }

        public void Stop()
        {
            if (running != null && running.IsActive())
            {
                running.Kill();
            }

            running = null;

            StopHint();
            StopInvalid();

            // Only the throwing stops. Dust already in the air is left to fade, which
            // reads better than it vanishing mid-flight and costs nothing.
            if (dustWave != null)
            {
                StopCoroutine(dustWave);
                dustWave = null;
            }

            RestoreCamera();

            ResetCellScales();
        }

        private static Tween Pulse(Transform target, float scale, float duration)
        {
            target.localScale = Vector3.one;

            return target
                .DOScale(scale, duration * 0.4f)
                .SetLoops(2, LoopType.Yoyo)
                .SetEase(Ease.OutQuad);
        }

        /// <summary>
        /// Squares are pooled, so one left mid-pulse would be handed to the next level at
        /// the wrong size.
        /// </summary>
        private void ResetCellScales()
        {
            if (boardView == null)
            {
                return;
            }

            IReadOnlyList<CellView> cells = boardView.ActiveCells;

            for (int i = 0; i < cells.Count; i++)
            {
                cells[i].transform.localScale = Vector3.one;
            }
        }
    }
}
