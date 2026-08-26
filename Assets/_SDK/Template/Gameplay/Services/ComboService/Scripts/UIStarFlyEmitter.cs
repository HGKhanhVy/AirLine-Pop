using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ASTeams.Base.Gameplay
{
    public sealed class UIStarFlyEmitter : MonoBehaviour
    {
        [Header("Canvas")]
        [SerializeField] private RectTransform targetRoot;

        [Header("Star")]
        [SerializeField] private Image starIconPrefab;
        [SerializeField] private Image starIconTarget;

        [Header("Flight")]
        [SerializeField] private float flyDuration = 1.2f;
        [SerializeField] private float spreadRadius = 40f;
        [SerializeField] private float arcHeight = 180f;
        [SerializeField] private float arcSideOffset = 90f;
        [SerializeField] private float staggerDelay = 0.04f;
        [SerializeField] private float startPopScale = 1.25f;
        [SerializeField] private float endScale = 0.55f;
        [SerializeField] private float spinDegrees = 220f;

        [Header("Trail")]
        [SerializeField] private int trailCount = 5;
        [SerializeField] private float trailSpacing = 0.035f;
        [SerializeField] private float trailStartAlpha = 0.42f;
        [SerializeField] private float trailScaleStep = 0.12f;

        [Header("Target Punch")]
        [SerializeField] private float punchScale = 0.18f;
        [SerializeField] private float punchDuration = 0.18f;
        [SerializeField] private int punchVibrato = 8;
        [SerializeField] private float punchElasticity = 0.9f;

        private readonly Queue<Image> starPool = new Queue<Image>();
        private readonly Queue<Image> trailPool = new Queue<Image>();
        private Tween _targetPunchTween;

        public void Emit(int count, Vector3 worldFrom) => Emit(count, worldFrom, null);

        public void Emit(int count, Vector3 worldFrom, Action onArrived)
        {
            if (targetRoot == null || starIconPrefab == null || starIconTarget == null)
            {
                return;
            }

            Vector2 from = WorldToCanvas(targetRoot, worldFrom);
            Vector2 to = TargetToLocalPoint(targetRoot, starIconTarget.rectTransform);

            int arrived = 0;
            int total = Mathf.Max(1, count);

            for (int i = 0; i < total; i++)
            {
                Image star = GetItem(starPool, starIconPrefab, targetRoot);
                Vector2 offset = UnityEngine.Random.insideUnitCircle * spreadRadius;
                Vector2 startPosition = from + offset;
                Vector2 controlPosition = GetControlPosition(startPosition, to, i);
                float delay = i * staggerDelay;
                List<Image> trails = GetTrails(star, trailCount);

                PrepareStar(star, startPosition);
                PlayStarFlight(star, trails, startPosition, controlPosition, to, delay, () =>
                {
                    ReleaseTrails(trails);
                    ReleaseItem(starPool, star);
                    PunchTargetOnce();

                    arrived++;
                    if (arrived >= total)
                    {
                        onArrived?.Invoke();
                    }

                    AudioController.Instance.PlaySound(SoundName.UI_CollectGem);
                });
            }
        }

        private void PlayStarFlight(
            Image star,
            List<Image> trails,
            Vector2 startPosition,
            Vector2 controlPosition,
            Vector2 endPosition,
            float delay,
            Action onComplete)
        {
            RectTransform starRectTransform = star.rectTransform;
            float progress = 0f;

            Sequence sequence = DOTween.Sequence();
            sequence.SetTarget(star);
            sequence.AppendInterval(delay);
            sequence.Append(starRectTransform.DOScale(startPopScale, 0.16f).SetEase(Ease.OutBack));
            sequence.Append(DOTween.To(() => progress, value =>
            {
                progress = value;
                float easedProgress = DOVirtual.EasedValue(0f, 1f, progress, Ease.InOutCubic);
                Vector2 position = GetQuadraticBezierPoint(startPosition, controlPosition, endPosition, easedProgress);
                starRectTransform.anchoredPosition = position;
                UpdateTrails(trails, startPosition, controlPosition, endPosition, progress);
            }, 1f, flyDuration).SetEase(Ease.Linear));
            sequence.Join(starRectTransform.DOScale(endScale, flyDuration).SetEase(Ease.InQuad));
            sequence.Join(starRectTransform.DORotate(new Vector3(0f, 0f, spinDegrees), flyDuration, RotateMode.FastBeyond360).SetEase(Ease.OutQuad));
            sequence.OnComplete(() => onComplete?.Invoke());
        }

        private void PrepareStar(Image star, Vector2 startPosition)
        {
            RectTransform starRectTransform = star.rectTransform;
            star.gameObject.SetActive(true);
            star.color = Color.white;
            starRectTransform.SetAsLastSibling();
            starRectTransform.anchoredPosition = startPosition;
            starRectTransform.localScale = Vector3.one * 0.1f;
            starRectTransform.localRotation = Quaternion.identity;
        }

        private List<Image> GetTrails(Image source, int count)
        {
            var trails = new List<Image>(Mathf.Max(0, count));

            if (count <= 0)
            {
                return trails;
            }

            for (int i = 0; i < count; i++)
            {
                Image trail = GetItem(trailPool, source, targetRoot);
                RectTransform trailRectTransform = trail.rectTransform;
                float alpha = trailStartAlpha * (1f - (float)i / count);
                float scale = Mathf.Max(0.1f, 1f - (trailScaleStep * (i + 1)));

                trail.gameObject.SetActive(true);
                trail.color = new Color(1f, 1f, 1f, alpha);
                trailRectTransform.localScale = Vector3.one * scale;
                trailRectTransform.localRotation = Quaternion.identity;
                trailRectTransform.SetAsLastSibling();
                source.rectTransform.SetAsLastSibling();
                trails.Add(trail);
            }

            return trails;
        }

        private void UpdateTrails(List<Image> trails, Vector2 startPosition, Vector2 controlPosition, Vector2 endPosition, float progress)
        {
            for (int i = 0; i < trails.Count; i++)
            {
                float trailProgress = Mathf.Clamp01(progress - ((i + 1) * trailSpacing));
                float easedProgress = DOVirtual.EasedValue(0f, 1f, trailProgress, Ease.InOutCubic);
                Vector2 position = GetQuadraticBezierPoint(startPosition, controlPosition, endPosition, easedProgress);
                trails[i].rectTransform.anchoredPosition = position;
            }
        }

        private void ReleaseTrails(List<Image> trails)
        {
            for (int i = 0; i < trails.Count; i++)
            {
                ReleaseItem(trailPool, trails[i]);
            }
        }

        private void PunchTargetOnce()
        {
            if (starIconTarget == null)
            {
                return;
            }

            if (_targetPunchTween != null && _targetPunchTween.IsActive())
            {
                _targetPunchTween.Kill(false);
            }

            RectTransform targetTransform = starIconTarget.rectTransform;
            targetTransform.localScale = Vector3.one;

            _targetPunchTween = targetTransform.DOPunchScale(
                new Vector3(punchScale, punchScale, 0f),
                punchDuration,
                punchVibrato,
                punchElasticity
            );
        }

        private Vector2 GetControlPosition(Vector2 startPosition, Vector2 endPosition, int index)
        {
            Vector2 midpoint = (startPosition + endPosition) * 0.5f;
            Vector2 direction = (endPosition - startPosition).normalized;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            float side = index % 2 == 0 ? 1f : -1f;
            float sideOffset = UnityEngine.Random.Range(arcSideOffset * 0.35f, arcSideOffset) * side;
            float heightOffset = UnityEngine.Random.Range(arcHeight * 0.75f, arcHeight * 1.25f);

            return midpoint + (Vector2.up * heightOffset) + (perpendicular * sideOffset);
        }

        private static Vector2 GetQuadraticBezierPoint(Vector2 startPosition, Vector2 controlPosition, Vector2 endPosition, float progress)
        {
            float inverseProgress = 1f - progress;
            return (inverseProgress * inverseProgress * startPosition) +
                   (2f * inverseProgress * progress * controlPosition) +
                   (progress * progress * endPosition);
        }

        private static Image GetItem(Queue<Image> pool, Image prefab, RectTransform parent)
        {
            if (pool.Count > 0)
            {
                return pool.Dequeue();
            }

            return Instantiate(prefab, parent);
        }

        private static void ReleaseItem(Queue<Image> pool, Image item)
        {
            item.DOKill(false);
            item.gameObject.SetActive(false);
            pool.Enqueue(item);
        }

        private static Vector2 WorldToCanvas(RectTransform canvas, Vector3 worldPos)
        {
            Camera mainCamera = Camera.main;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(mainCamera, worldPos);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas, screen, null, out Vector2 localPoint);

            return localPoint;
        }

        private static Vector2 TargetToLocalPoint(RectTransform root, RectTransform target)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, target.position);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                root, screen, null, out Vector2 localPoint);

            return localPoint;
        }
    }
}
