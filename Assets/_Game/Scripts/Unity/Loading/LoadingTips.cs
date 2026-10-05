using DG.Tweening;
using TMPro;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Light-hearted lines about the crew getting the flight ready, one after another,
    /// starting from a random one so each loading screen opens on something new.
    /// </summary>
    public sealed class LoadingTips : LoadingAnimation
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private string[] keys = new string[0];
        [SerializeField, Min(0.3f)] private float secondsPerTip = 1.4f;

        private Sequence cycle;
        private int index;

        public override void Play()
        {
            if (keys.Length == 0)
            {
                return;
            }

            index = Random.Range(0, keys.Length);
            ShowCurrent();
            cycle = DOTween.Sequence()
                .AppendInterval(secondsPerTip)
                .AppendCallback(ShowNext)
                .Append(label.transform.DOPunchScale(Vector3.one * 0.08f, 0.3f, 6, 0.6f))
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(true);
        }

        public override void Stop()
        {
            cycle?.Kill();
            cycle = null;
            label.transform.localScale = Vector3.one;
        }

        private void ShowNext()
        {
            index = (index + 1) % keys.Length;
            ShowCurrent();
        }

        private void ShowCurrent()
        {
            label.text = Localization.Get(keys[index]);
        }

#if UNITY_EDITOR
        public void EditorLink(TMP_Text linkedLabel, string[] linkedKeys)
        {
            label = linkedLabel;
            keys = linkedKeys;
        }
#endif
    }
}
