using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// A pill that drops in to say a new regular has started coming to the lounge, then
    /// leaves by itself. Several arrivals at once are named together in one message.
    /// </summary>
    public sealed class ArrivalToastView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform pill;
        [SerializeField] private TMP_Text label;

        [Tooltip("{0} is the new regular's name, or several names joined with commas.")]
        [SerializeField, Min(0.5f)] private float holdSeconds = 3f;

        private Sequence motion;

        private void Awake()
        {
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }

        private void OnDestroy()
        {
            motion?.Kill();
        }

        public void Announce(IReadOnlyList<CatBreedSO> arrived)
        {
            if (arrived == null || arrived.Count == 0)
            {
                return;
            }

            label.text = Localization.Format("toast.arrived", JoinNames(arrived));

            motion?.Kill();
            Vector2 rest = pill.anchoredPosition;
            pill.anchoredPosition = rest + new Vector2(0f, 120f);
            motion = DOTween.Sequence()
                .SetDelay(0.6f)
                .Append(group.DOFade(1f, 0.25f))
                .Join(pill.DOAnchorPos(rest, 0.35f).SetEase(Ease.OutBack))
                .AppendInterval(holdSeconds)
                .Append(group.DOFade(0f, 0.3f))
                .SetUpdate(true);
        }

        private static string JoinNames(IReadOnlyList<CatBreedSO> arrived)
        {
            var names = new string[arrived.Count];

            for (int i = 0; i < arrived.Count; i++)
            {
                names[i] = arrived[i].DisplayName;
            }

            return string.Join(", ", names);
        }

#if UNITY_EDITOR
        public void EditorLink(CanvasGroup linkedGroup, RectTransform linkedPill, TMP_Text linkedLabel)
        {
            group = linkedGroup;
            pill = linkedPill;
            label = linkedLabel;
        }
#endif
    }
}
