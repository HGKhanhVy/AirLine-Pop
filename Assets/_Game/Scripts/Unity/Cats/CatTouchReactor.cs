using DG.Tweening;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Makes a lounge cat answer a touch: it rolls over, rubs against the hand, purrs or hops
    /// for joy, never the same twice running, with little hearts floating up. Poke one too
    /// many times in a row and it sees stars. Runs beside the care card, which the same tap
    /// still opens; this only plays the moment.
    /// </summary>
    public sealed class CatTouchReactor : MonoBehaviour
    {
        [SerializeField] private CatTapInput tapInput;
        [SerializeField] private Transform viewer;

        [Header("Hearts")]
        [SerializeField] private SpriteRenderer[] hearts = new SpriteRenderer[0];
        [SerializeField, Min(1)] private int heartsPerTouch = 3;
        [Tooltip("The hearts' colour: the icon itself is white, made to be tinted.")]
        [SerializeField] private Color heartTint = new Color(1f, 0.42f, 0.5f, 1f);
        [SerializeField] private float heartHeight = 1.7f;
        [SerializeField] private float heartRise = 0.7f;
        [SerializeField] private float heartSpread = 0.35f;
        [SerializeField, Min(0.1f)] private float heartSeconds = 0.9f;
        [SerializeField, Min(0.01f)] private float heartScale = 0.35f;

        [Header("Nuzzle")]
        [SerializeField, Range(0f, 0.4f)] private float nuzzleSquash = 0.12f;
        [SerializeField] private float nuzzleSway = 0.08f;
        [SerializeField, Min(1)] private int nuzzleRubs = 3;
        [SerializeField, Min(0.2f)] private float nuzzleSeconds = 1.1f;

        [Header("Too many pokes")]
        [Tooltip("This many taps on the same cat within the window make it dizzy.")]
        [SerializeField, Min(2)] private int dizzyTaps = 4;
        [SerializeField, Min(0.2f)] private float tapWindow = 2f;
        [SerializeField, Min(0.2f)] private float dizzySeconds = 1.6f;

        private ICatReaction[] reactions;
        private int lastReaction = -1;
        private int nextHeart;
        private CatBrain lastCat;
        private float lastTapTime;
        private int tapCount;

        private void Awake()
        {
            reactions = new ICatReaction[]
            {
                new AnimatorCatReaction(CatAnimatorParams.Play),
                new NuzzleCatReaction(nuzzleSquash, nuzzleSway, nuzzleRubs, nuzzleSeconds),
                new AnimatorCatReaction(CatAnimatorParams.Pet),
                new AnimatorCatReaction(CatAnimatorParams.Celebrate),
            };

            for (int i = 0; i < hearts.Length; i++)
            {
                hearts[i].enabled = false;
            }
        }

        private void OnEnable()
        {
            tapInput.OnCatTapped += React;
        }

        private void OnDisable()
        {
            tapInput.OnCatTapped -= React;
        }

        private void React(CatBrain cat)
        {
            CountTap(cat);

            if (tapCount >= dizzyTaps)
            {
                tapCount = 0;
                MakeDizzy(cat);
                return;
            }

            reactions[PickReaction()].Play(cat.View);
            FloatHearts(cat.View.Root.position);
        }

        private void CountTap(CatBrain cat)
        {
            bool isStreak = cat == lastCat && Time.unscaledTime - lastTapTime <= tapWindow;
            tapCount = isStreak ? tapCount + 1 : 1;
            lastCat = cat;
            lastTapTime = Time.unscaledTime;
        }

        /// <summary>A random reaction, never the one just played, so a few taps show off the whole set.</summary>
        private int PickReaction()
        {
            if (lastReaction < 0)
            {
                lastReaction = Random.Range(0, reactions.Length);
                return lastReaction;
            }

            int pick = Random.Range(0, reactions.Length - 1);

            if (pick >= lastReaction)
            {
                pick++;
            }

            lastReaction = pick;
            return pick;
        }

        private void MakeDizzy(CatBrain cat)
        {
            cat.View.SetPose(CatPose.Dizzy);

            // Back to looking at the player, unless the player has moved on in the meantime.
            DOVirtual.DelayedCall(dizzySeconds, () =>
            {
                if (cat.Activity == CatActivity.Selected)
                {
                    cat.View.SetPose(CatPose.Wait);
                }
            }).SetTarget(this);
        }

        /// <summary>A few hearts rise from above the cat's head, drifting apart and fading.</summary>
        private void FloatHearts(Vector3 catPosition)
        {
            Vector3 top = catPosition + Vector3.up * heartHeight;

            for (int i = 0; i < heartsPerTouch && hearts.Length > 0; i++)
            {
                SpriteRenderer heart = hearts[nextHeart];
                nextHeart = (nextHeart + 1) % hearts.Length;

                Transform mark = heart.transform;
                mark.DOKill();
                heart.DOKill();

                float offset = (i - (heartsPerTouch - 1) * 0.5f) * heartSpread;
                Vector3 start = top + viewer.right * offset * 0.4f;
                mark.position = start;
                mark.rotation = viewer.rotation;
                mark.localScale = Vector3.one * heartScale * 0.4f;
                heart.color = heartTint;
                heart.enabled = true;

                float delay = i * 0.12f;
                Vector3 end = start + Vector3.up * heartRise + viewer.right * offset;
                mark.DOMove(end, heartSeconds).SetDelay(delay).SetEase(Ease.OutSine);
                mark.DOScale(heartScale, heartSeconds * 0.35f).SetDelay(delay).SetEase(Ease.OutBack);
                heart.DOFade(0f, heartSeconds * 0.4f).SetDelay(delay + heartSeconds * 0.6f).OnComplete(() => heart.enabled = false);
            }
        }

#if UNITY_EDITOR
        public void EditorLink(CatTapInput linkedTapInput, Transform linkedViewer, SpriteRenderer[] linkedHearts, float linkedHeartScale)
        {
            tapInput = linkedTapInput;
            viewer = linkedViewer;
            hearts = linkedHearts;
            heartScale = linkedHeartScale;
        }
#endif
    }
}
