using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Little gift boxes bobbing over the regulars that have a present for the player today.
    /// The boxes are made once and follow their cats as they wander.
    /// </summary>
    public sealed class CatGiftMarkersView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] boxes = new SpriteRenderer[0];
        [SerializeField] private Transform viewer;

        [Tooltip("How far above the top of the cat's body the box floats, in world units.")]
        [SerializeField] private float lift = 0.12f;
        [SerializeField] private float bobHeight = 0.08f;
        [SerializeField, Min(0.2f)] private float bobSeconds = 0.8f;

        private readonly List<Collider> targets = new List<Collider>();
        private float[] bobs;

        private void Awake()
        {
            bobs = new float[boxes.Length];

            for (int i = 0; i < boxes.Length; i++)
            {
                boxes[i].enabled = false;
            }
        }

        /// <summary>Shows a box over each of these cats, by the shape of their bodies, and hides the rest.</summary>
        public void Show(IReadOnlyList<Collider> cats)
        {
            targets.Clear();

            for (int i = 0; i < cats.Count && i < boxes.Length; i++)
            {
                targets.Add(cats[i]);
            }

            for (int i = 0; i < boxes.Length; i++)
            {
                bool isUsed = i < targets.Count;

                if (isUsed && !boxes[i].enabled)
                {
                    // A little pop as a box appears, then a gentle bob, out of step with the others.
                    boxes[i].transform.localScale = Vector3.zero;
                    boxes[i].transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
                    int slot = i;
                    DOTween.To(() => bobs[slot], v => bobs[slot] = v, bobHeight, bobSeconds).SetEase(Ease.InOutSine)
                        .SetLoops(-1, LoopType.Yoyo).SetTarget(boxes[i]).Goto(bobSeconds * 0.4f * i, true);
                }
                else if (!isUsed && boxes[i].enabled)
                {
                    DOTween.Kill(boxes[i]);
                    boxes[i].transform.DOKill();
                }

                boxes[i].enabled = isUsed;
            }

            Follow();
        }

        private void LateUpdate()
        {
            if (targets.Count > 0)
            {
                Follow();
            }
        }

        private void Follow()
        {
            for (int i = 0; i < targets.Count; i++)
            {
                Bounds body = targets[i].bounds;
                Transform box = boxes[i].transform;
                box.position = new Vector3(body.center.x, body.max.y + lift + bobs[i], body.center.z);
                box.rotation = viewer.rotation;
            }
        }

#if UNITY_EDITOR
        public void EditorLink(SpriteRenderer[] linkedBoxes, Transform linkedViewer)
        {
            boxes = linkedBoxes;
            viewer = linkedViewer;
        }
#endif
    }
}
