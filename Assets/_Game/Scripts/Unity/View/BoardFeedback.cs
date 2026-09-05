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
        [SerializeField] private Transform shakeRoot;

        [Header("Stuck")]
        [SerializeField, Min(0f)] private float shakeDuration = 0.32f;
        [SerializeField, Min(0f)] private float shakeStrength = 0.16f;
        [SerializeField, Min(0)] private int shakeVibrato = 14;

        [Header("Pulse")]
        [SerializeField, Min(1f)] private float pulseScale = 1.22f;
        [SerializeField, Min(0.01f)] private float pulseDuration = 0.26f;

        [Header("Hint")]
        [SerializeField, Min(1f)] private float hintScale = 1.3f;
        [SerializeField, Min(0.01f)] private float hintDuration = 0.45f;
        [SerializeField, Min(1)] private int hintLoops = 3;

        [Header("Win")]
        [SerializeField, Min(1f)] private float winScale = 1.16f;
        [SerializeField, Min(0.01f)] private float winStepDelay = 0.02f;
        [SerializeField, Min(0.01f)] private float winDuration = 0.22f;

        [Header("Confetti")]
        [SerializeField] private ConfettiView confetti;

        private Sequence running;
        private Coroutine confettiWave;
        private WaitForSeconds confettiStep;

        // The controller hands out a list it reuses, so the positions the wave needs are
        // copied here before the coroutine outlives the call that started it.
        private readonly List<Vector3> burstPositions = new List<Vector3>(96);

        private void Awake()
        {
            if (shakeRoot == null)
            {
                shakeRoot = boardView != null ? boardView.transform : transform;
            }

            confettiStep = new WaitForSeconds(winStepDelay);
        }

        private void OnDisable()
        {
            Stop();
        }

        /// <summary>
        /// Wobbles the board and pulses the cells still to cover. The list is supplied by
        /// the controller rather than read from the model here, so the view keeps knowing
        /// nothing about the rules.
        /// </summary>
        public void PlayStuck(IReadOnlyList<int> uncoveredCells)
        {
            Stop();

            running = DOTween.Sequence();
            running.Append(shakeRoot.DOShakePosition(shakeDuration, shakeStrength, shakeVibrato, 90f, false, true));

            if (uncoveredCells != null)
            {
                for (int i = 0; i < uncoveredCells.Count; i++)
                {
                    CellView cell = boardView.GetCellView(uncoveredCells[i]);

                    if (cell != null)
                    {
                        running.Join(Pulse(cell.transform, pulseScale, pulseDuration));
                    }
                }
            }

            running.SetUpdate(isIndependentUpdate: true);
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
            }

            running.SetUpdate(isIndependentUpdate: true);

            if (confetti != null && isActiveAndEnabled)
            {
                confettiWave = StartCoroutine(ThrowConfetti());
            }
        }

        /// <summary>
        /// Paper off each cell in the order the player drew it, so the celebration runs
        /// along the path with the wave rather than landing on the board all at once.
        /// </summary>
        private IEnumerator ThrowConfetti()
        {
            for (int i = 0; i < burstPositions.Count; i++)
            {
                confetti.Burst(burstPositions[i]);
                yield return confettiStep;
            }

            confettiWave = null;
        }

        /// <summary>
        /// Draws attention to one cell without playing it. A hint points, it does not
        /// move: the player still has to make the move themselves.
        /// </summary>
        public void PlayHint(int cell)
        {
            Stop();

            CellView view = boardView.GetCellView(cell);

            if (view == null)
            {
                return;
            }

            running = DOTween.Sequence();
            running.Append(view.transform
                .DOScale(hintScale, hintDuration / (hintLoops * 2f))
                .SetLoops(hintLoops * 2, LoopType.Yoyo)
                .SetEase(Ease.InOutSine));
            running.SetUpdate(isIndependentUpdate: true);
        }

        public void Stop()
        {
            if (running != null && running.IsActive())
            {
                running.Kill();
            }

            running = null;

            // Only the throwing stops. Paper already in the air is left to fall, which
            // reads better than it vanishing mid-flight and costs nothing.
            if (confettiWave != null)
            {
                StopCoroutine(confettiWave);
                confettiWave = null;
            }

            if (shakeRoot != null)
            {
                shakeRoot.localPosition = Vector3.zero;
            }

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
