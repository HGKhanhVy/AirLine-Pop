using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The loading spinner: a ball of yarn rolls round an oval and the cats chase it, each
    /// stepping through its walk frames, round a cat standing in the middle. Runners on the
    /// far side of the oval sit a touch smaller and are drawn behind the near ones (and
    /// behind the middle cat), and each faces the way it runs.
    /// </summary>
    public sealed class LoadingCatChase : LoadingAnimation
    {
        [Tooltip("The yarn first, then the cats; spread evenly behind it round the oval.")]
        [SerializeField] private RectTransform[] runners = new RectTransform[0];

        [Tooltip("The yarn ball, spun as it rolls.")]
        [SerializeField] private RectTransform yarn;

        [Tooltip("One image per cat, in the same order as the cats among the runners.")]
        [SerializeField] private Image[] cats = new Image[0];

        [Tooltip("Every cat's frames back to back, frameCounts[i] for cat i: a hop, a roll...")]
        [SerializeField] private Sprite[] frames = new Sprite[0];
        [SerializeField] private int[] frameCounts = new int[0];

        [Tooltip("How long each cat shows one frame.")]
        [SerializeField] private float[] frameSeconds = new float[0];

        [SerializeField] private Vector2 radius = new Vector2(350f, 68f);
        [SerializeField, Min(0.3f)] private float lapSeconds = 3f;

        [Tooltip("Gap between runners round the oval, in radians.")]
        [SerializeField] private float spacing = 0.9f;

        [SerializeField, Range(0.5f, 1f)] private float farScale = 0.82f;

        [Header("Middle")]
        [Tooltip("The cat standing in the middle of the oval, stepping through its idle frames.")]
        [SerializeField] private Image centre;
        [SerializeField] private Sprite[] centreFrames = new Sprite[0];
        [SerializeField, Min(0.02f)] private float centreFrameSeconds = 0.1f;

        private const float FullTurn = Mathf.PI * 2f;

        private Tween lap;
        private float angle;
        private int[] order;
        private int[] firstFrames;

        public override void Play()
        {
            order ??= new int[runners.Length + 1];
            firstFrames ??= FirstFrames();
            Show(0f);
            lap = DOTween.To(Angle, Show, FullTurn, lapSeconds).From(0f).SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart).SetUpdate(true);
        }

        public override void Stop()
        {
            lap?.Kill();
            lap = null;
        }

        private float Angle()
        {
            return angle;
        }

        private void Show(float value)
        {
            angle = value;
            float seconds = value / FullTurn * lapSeconds;

            for (int i = 0; i < runners.Length; i++)
            {
                Place(runners[i], value - i * spacing);
            }

            yarn.localRotation = Quaternion.Euler(0f, 0f, value * Mathf.Rad2Deg * 3f);

            for (int i = 0; i < cats.Length; i++)
            {
                // Each cat starts its cycle a little apart from the others.
                int step = (Mathf.FloorToInt(seconds / frameSeconds[i]) + i * 3) % frameCounts[i];
                cats[i].sprite = frames[firstFrames[i] + step];
            }

            if (centreFrames.Length > 0)
            {
                centre.sprite = centreFrames[Mathf.FloorToInt(seconds / centreFrameSeconds) % centreFrames.Length];
            }

            SortByDepth();
        }

        /// <summary>
        /// Puts a runner on the oval. The angle grows anticlockwise, so runners cross the near
        /// side moving right and the far side moving left; the art faces left.
        /// </summary>
        private void Place(RectTransform runner, float at)
        {
            float sin = Mathf.Sin(at);
            runner.anchoredPosition = new Vector2(radius.x * Mathf.Cos(at), radius.y * sin);
            float depth = Mathf.Lerp(1f, farScale, (sin + 1f) * 0.5f);
            float facing = sin > 0f ? 1f : -1f;
            runner.localScale = new Vector3(depth * facing, depth, 1f);
        }

        private int[] FirstFrames()
        {
            var first = new int[frameCounts.Length];

            for (int i = 1; i < first.Length; i++)
            {
                first[i] = first[i - 1] + frameCounts[i - 1];
            }

            return first;
        }

        /// <summary>The runners, then the middle cat last.</summary>
        private RectTransform Piece(int index)
        {
            return index < runners.Length ? runners[index] : centre.rectTransform;
        }

        /// <summary>Far pieces (higher up) first, so near ones draw over them; reorders only on change.</summary>
        private void SortByDepth()
        {
            for (int i = 0; i < order.Length; i++)
            {
                order[i] = i;
            }

            for (int i = 1; i < order.Length; i++)
            {
                int current = order[i];
                float y = Piece(current).anchoredPosition.y;
                int j = i - 1;

                while (j >= 0 && Piece(order[j]).anchoredPosition.y < y)
                {
                    order[j + 1] = order[j];
                    j--;
                }

                order[j + 1] = current;
            }

            for (int i = 0; i < order.Length; i++)
            {
                RectTransform piece = Piece(order[i]);

                if (piece.GetSiblingIndex() != i)
                {
                    piece.SetSiblingIndex(i);
                }
            }
        }

#if UNITY_EDITOR
        public void EditorLink(RectTransform[] linkedRunners, RectTransform linkedYarn, Image[] linkedCats, Sprite[] linkedFrames,
            int[] linkedFrameCounts, float[] linkedFrameSeconds, Image linkedCentre, Sprite[] linkedCentreFrames)
        {
            centre = linkedCentre;
            centreFrames = linkedCentreFrames;
            runners = linkedRunners;
            yarn = linkedYarn;
            cats = linkedCats;
            frames = linkedFrames;
            frameCounts = linkedFrameCounts;
            frameSeconds = linkedFrameSeconds;
        }
#endif
    }
}
