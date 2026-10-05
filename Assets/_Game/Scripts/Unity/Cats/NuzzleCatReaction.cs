using DG.Tweening;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The cat rubs against the hand that touched it: a few soft squashes and sways from
    /// side to side, done with its scale and position so the billboard keeps its turn.
    /// </summary>
    public sealed class NuzzleCatReaction : ICatReaction
    {
        private readonly float squash;
        private readonly float sway;
        private readonly int rubs;
        private readonly float seconds;

        public NuzzleCatReaction(float squash, float sway, int rubs, float seconds)
        {
            this.squash = squash;
            this.sway = sway;
            this.rubs = Mathf.Max(1, rubs);
            this.seconds = seconds;
        }

        public void Play(CatView cat)
        {
            Transform body = cat.Root;
            body.DOKill(true);

            Vector3 rest = body.localScale;
            Vector3 home = body.position;
            var rubbing = new Vector3(rest.x * (1f + squash), rest.y * (1f - squash), rest.z);
            float beat = seconds / (rubs * 2f);
            Sequence nuzzle = DOTween.Sequence().SetTarget(body);

            for (int i = 0; i < rubs; i++)
            {
                float side = i % 2 == 0 ? sway : -sway;
                nuzzle.Append(body.DOScale(rubbing, beat).SetEase(Ease.InOutSine));
                nuzzle.Join(body.DOMove(home + body.right * side, beat).SetEase(Ease.InOutSine));
                nuzzle.Append(body.DOScale(rest, beat).SetEase(Ease.InOutSine));
                nuzzle.Join(body.DOMove(home, beat).SetEase(Ease.InOutSine));
            }
        }
    }
}
