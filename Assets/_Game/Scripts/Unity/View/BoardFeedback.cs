using System.Collections;
using System.Collections.Generic;
using ASTeams.SingleLine.Core;
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
        [Tooltip("Drags through the hinted squares. Leave empty to run the hint without a " +
                 "marker; the squares still press in order.")]
        [SerializeField] private HintGhostView hintGhost;

        [Tooltip("How long the hint stays up. GDD 7.1 asks for 2.5 s or until the player moves.")]
        [SerializeField, Min(0.1f)] private float hintHold = 2.5f;

        [Tooltip("How long the drag takes to cross one square. This is the speed of the " +
                 "imaginary finger, so it wants to look like someone playing deliberately " +
                 "rather than racing.")]
        [SerializeField, Min(0.05f)] private float hintStepSeconds = 0.22f;

        [Tooltip("Pause between one run and the next, so a repeat reads as the move being " +
                 "shown again rather than as a loop with no beginning.")]
        [SerializeField, Min(0f)] private float hintRunGap = 0.35f;

        [SerializeField, Min(0.05f)] private float hintFadeSeconds = 0.15f;

        [Header("Win")]
        [SerializeField, Min(1f)] private float winScale = 1.22f;

        [Tooltip("How long the whole wave should take. The squares share it out between " +
                 "them, so a short path pops unhurriedly and a long one runs quickly, and " +
                 "both finish at about the same moment.")]
        [SerializeField, Min(0.1f)] private float winWaveSeconds = 0.7f;

        [Tooltip("Shortest gap between two squares, so a very long path overruns the window " +
                 "rather than being sped up into a blur.")]
        [SerializeField, Min(0.01f)] private float winStepMin = 0.03f;

        [Tooltip("Longest gap between two squares, so a three square level does not dawdle.")]
        [SerializeField, Min(0.02f)] private float winStepMax = 0.11f;

        [Tooltip("How far into one square's pop the next one sets off. Below 1 they overlap, " +
                 "which is what makes the wave roll: at 0.25 there are four squares in the " +
                 "air at any moment, so neighbours sit at four different heights and the " +
                 "crest reads as a slope. Raising it thins the crest until, at 1, the " +
                 "squares run strictly one at a time and the wave catches between them.")]
        [SerializeField, Range(0.15f, 1f)] private float winStepShare = 0.25f;

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
        private readonly List<int> hintPath = new List<int>(4);
        private Coroutine dustWave;
        private WaitForSeconds dustStep;
        private float dustStepSeconds;

        // The controller hands out a list it reuses, so what the wave needs is copied here
        // before the coroutine outlives the call that started it. Colours are the settled
        // ones, not what the squares show: the win is raised inside the last move, before
        // that square is repainted, and the last few squares are still fading in.
        private readonly List<Vector3> burstPositions = new List<Vector3>(96);
        private readonly List<Color> burstColors = new List<Color>(96);

        private void Awake()
        {
            effects = effectPlayer;
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

        /// <summary>
        /// A wave along the path, in the order the player drew it.
        ///
        /// Two numbers, not one. The gap between squares sets the rhythm and comes out of
        /// a fixed budget, because a level here is anywhere from 3 to 69 squares and one
        /// fixed gap cannot serve both. How long a square takes to swell and settle is
        /// then a multiple of that gap rather than equal to it, and that is what makes
        /// this a wave: each square is still on its way down as its neighbour starts up,
        /// so the crest rolls along the path instead of hopping from square to square.
        ///
        /// Tying the two together is what went wrong in both directions before. Equal, and
        /// the squares pop one at a time like a queue. Gap far shorter than the pop, as it
        /// first shipped, and four squares swell at once and the board just shivers.
        /// </summary>
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

            float stepSeconds = Mathf.Clamp(
                winWaveSeconds / pathInOrder.Count, winStepMin, winStepMax);
            float pulseSeconds = stepSeconds / winStepShare;

            for (int step = 0; step < pathInOrder.Count; step++)
            {
                int index = pathInOrder[step];
                CellView cell = boardView.GetCellView(index);

                if (cell == null)
                {
                    continue;
                }

                running.Insert(step * stepSeconds, Pulse(cell.transform, winScale, pulseSeconds));
                burstPositions.Add(boardView.GetCellWorldPosition(index));
                burstColors.Add(boardView.GetSettledColor(index, step == pathInOrder.Count - 1));
            }

            running.SetUpdate(isIndependentUpdate: false);

            // The win screen has to sit out the whole wave, and only the board knows how
            // long that is: the last square sets off one gap behind each of the others and
            // then needs its own pop on top.
            GameplayEvents.RaiseCelebrationStarted(
                ((pathInOrder.Count - 1) * stepSeconds) + pulseSeconds);

            SetDustStep(stepSeconds);

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
        /// Plays the hint as somebody playing it: a light drags from square to square in
        /// the order they are to be walked, and each one presses under it as it lands.
        ///
        /// A hint points, it does not move. Nothing here is coloured in and no cell is
        /// entered; the player still has to make every move themselves. But three squares
        /// pulsing together only say "these three", leaving the player to work out which
        /// comes first, and pulsing them in turn still only says "this order". Dragging
        /// through them says the thing the player actually has to do with their finger.
        /// </summary>
        /// <param name="cells">The squares to walk, in order.</param>
        /// <param name="fromCell">
        /// Where the drag sets off, normally the square the player is standing on. Pass
        /// <see cref="LevelData.NoCell"/> when the hint asks for undos first: there is no
        /// line from the head to its first square, and drawing one would be a lie.
        /// </param>
        public void PlayHint(IReadOnlyList<int> cells, int fromCell)
        {
            StopHint();

            if (cells == null || cells.Count == 0)
            {
                return;
            }

            hintPath.Clear();

            if (fromCell != LevelData.NoCell)
            {
                hintPath.Add(fromCell);
            }

            for (int i = 0; i < cells.Count; i++)
            {
                hintPath.Add(cells[i]);

                CellView view = boardView.GetCellView(cells[i]);

                if (view != null)
                {
                    hintCells.Add(view);
                }
            }

            if (hintPath.Count < 2)
            {
                return;
            }

            bool hasGhost = hintGhost != null && hintGhost.IsReady;
            hintSequence = DOTween.Sequence();

            // Put the light back on the first square at the top of every run, so a repeat
            // starts where the last one did rather than from wherever it ended.
            Vector3 origin = boardView.GetCellWorldPosition(hintPath[0]);

            if (hasGhost)
            {
                hintSequence.AppendCallback(() => hintGhost.Show(origin, Color.white));
            }

            for (int i = 1; i < hintPath.Count; i++)
            {
                int index = hintPath[i];
                Vector3 to = boardView.GetCellWorldPosition(index);

                if (hasGhost)
                {
                    hintSequence.Append(hintGhost.MoveTo(to, hintStepSeconds));
                }
                else
                {
                    hintSequence.AppendInterval(hintStepSeconds);
                }

                hintSequence.AppendCallback(() => boardView.PreviewCell(index));
            }

            if (hasGhost)
            {
                hintSequence.Append(hintGhost.FadeOut(hintFadeSeconds));
            }

            hintSequence.AppendInterval(hintRunGap);

            // Whole runs only: cutting one halfway leaves the light stranded between
            // squares, pointing at nothing.
            float run = ((hintPath.Count - 1) * hintStepSeconds) + hintFadeSeconds + hintRunGap;
            hintSequence.SetLoops(Mathf.Max(1, Mathf.RoundToInt(hintHold / run)), LoopType.Restart);

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
            hintGhost?.Hide();

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

        /// <summary>
        /// One square swelling and settling again, in exactly <paramref name="length"/>.
        /// Both legs come out of that one number so the caller can lay pops end to end and
        /// trust that they will not run into each other.
        /// </summary>
        private static Tween Pulse(Transform target, float scale, float length)
        {
            target.localScale = Vector3.one;

            // Sine in and out, not quad out: a quad leaves a corner where it starts and
            // another where the yoyo turns, and at this speed those corners are what the
            // eye reads as the wave stuttering from square to square.
            return target
                .DOScale(scale, length * 0.5f)
                .SetLoops(2, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        /// <summary>
        /// The dust rides the same beat as the wave. A new wait is only built when the
        /// beat actually changes, so replaying a level of the same size allocates nothing.
        /// </summary>
        private void SetDustStep(float seconds)
        {
            if (dustStep != null && Mathf.Approximately(dustStepSeconds, seconds))
            {
                return;
            }

            dustStepSeconds = seconds;
            dustStep = new WaitForSeconds(seconds);
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
