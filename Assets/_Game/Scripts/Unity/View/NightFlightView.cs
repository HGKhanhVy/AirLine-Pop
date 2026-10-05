using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The dark of a night flight. Night falls first; then a little light hops along the
    /// whole route, square by square, and only a soft glow round it shows the board as it
    /// goes. After that the only light left is each plane's own: a pool round it and a
    /// headlight beam from its nose, both fading softly into the dark and turning wherever
    /// the plane turns. The rest has to be flown from memory.
    ///
    /// The dark is one sheet drawn by the night shade shader, which works the soft light out
    /// per pixel from the lights handed to it here.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class NightFlightView : MonoBehaviour
    {
        // The shader's own count of lights: the player's plane, the wingman, the guiding light.
        private const int LightCount = 3;
        private const int GuideLight = 2;

        private static readonly int DarknessId = Shader.PropertyToID("_Darkness");
        private static readonly int LightId = Shader.PropertyToID("_NightLight");
        private static readonly int ShapeId = Shader.PropertyToID("_NightShape");

        [SerializeField] private BoardView board;
        [SerializeField] private SpriteRenderer shade;

        [Tooltip("Each plane's body, which its headlight follows: the player's, then the wingman's.")]
        [SerializeField] private SpriteRenderer[] planes = new SpriteRenderer[0];

        [Tooltip("The light that flies the route before the player starts.")]
        [SerializeField] private SpriteRenderer guide;

        [SerializeField, Range(0f, 1f)] private float darkness = 1f;

        [Header("Light, in board squares")]
        [SerializeField, Min(0.1f)] private float planePool = 0.5f;
        [SerializeField, Min(0f)] private float beamReach = 1.4f;
        [SerializeField, Min(0.1f)] private float beamWidth = 1.1f;
        [SerializeField, Min(0.1f)] private float guidePool = 0.7f;
        [SerializeField, Min(0.1f)] private float guideSize = 0.45f;

        [Header("Timing")]
        [SerializeField, Min(0.05f)] private float stepSeconds = 0.34f;
        [SerializeField, Min(0f)] private float hopHeight = 0.25f;
        [SerializeField, Min(0f)] private float holdBeforeStart = 0.3f;
        [SerializeField, Min(0.05f)] private float fadeSeconds = 0.6f;

        private readonly Vector4[] lights = new Vector4[LightCount];
        private readonly Vector4[] shapes = new Vector4[LightCount];
        private Material material;
        private Sequence preview;
        private Tween fade;
        private float shadeAmount;
        private bool isGuideLit;

        public bool IsPreviewing => preview != null && preview.IsActive();

        private void Awake()
        {
            material = shade.material;
            HideAll();
        }

        private void OnDisable()
        {
            KillTweens();
        }

        /// <summary>Hands the shader where each light is and which way it faces.</summary>
        private void LateUpdate()
        {
            if (shadeAmount <= 0f)
            {
                return;
            }

            float pitch = board.CellPitch;

            for (int i = 0; i < planes.Length && i < GuideLight; i++)
            {
                SpriteRenderer plane = planes[i];

                if (plane == null || !plane.enabled)
                {
                    shapes[i] = Vector4.zero;
                    continue;
                }

                Transform body = plane.transform;
                Vector3 nose = body.up;
                lights[i] = new Vector4(body.position.x, body.position.y, nose.x, nose.y);
                shapes[i] = new Vector4(planePool * pitch, beamReach * pitch, beamWidth * pitch, 1f);
            }

            Vector3 guidePosition = guide.transform.position;
            lights[GuideLight] = new Vector4(guidePosition.x, guidePosition.y, 0f, 1f);
            shapes[GuideLight] = isGuideLit ? new Vector4(guidePool * pitch, 0f, 0f, 1f) : Vector4.zero;

            material.SetFloat(DarknessId, shadeAmount);
            material.SetVectorArray(LightId, lights);
            material.SetVectorArray(ShapeId, shapes);
        }

        /// <summary>Clears any night left from the previous level.</summary>
        public void Prepare()
        {
            KillTweens();
            HideAll();
        }

        /// <summary>
        /// Lets night fall, then flies the light along the route through the dark.
        /// <paramref name="onDone"/> runs once the light has gone, which is when the player may start.
        /// </summary>
        public void PlayPreview(IReadOnlyList<int> route, Action onDone)
        {
            KillTweens();

            if (route.Count == 0)
            {
                FadeShade(darkness);
                onDone?.Invoke();
                return;
            }

            Transform light = guide.transform;
            light.position = board.GetCellWorldPosition(route[0]);
            light.localScale = Vector3.zero;
            guide.enabled = true;
            isGuideLit = true;

            preview = DOTween.Sequence();
            preview.Append(ShadeTween(darkness));
            preview.Append(light.DOScale(board.CellPitch * guideSize, 0.25f).SetEase(Ease.OutBack));
            preview.AppendInterval(holdBeforeStart);

            for (int i = 1; i < route.Count; i++)
            {
                int cell = route[i];
                preview.Append(light.DOJump(board.GetCellWorldPosition(cell), hopHeight, 1, stepSeconds));
                preview.AppendCallback(() => board.PreviewCell(cell));
            }

            preview.AppendInterval(holdBeforeStart);
            preview.Append(light.DOScale(0f, 0.25f).SetEase(Ease.InBack));
            preview.OnComplete(() =>
            {
                preview = null;
                guide.enabled = false;
                isGuideLit = false;
                onDone?.Invoke();
            });
        }

        /// <summary>Morning: the dark lifts, for the win.</summary>
        public void Dawn()
        {
            KillTweens();
            guide.enabled = false;
            isGuideLit = false;
            FadeShade(0f);
        }

        private void FadeShade(float target)
        {
            fade = ShadeTween(target);
        }

        private Tween ShadeTween(float target)
        {
            shade.enabled = true;
            return DOTween.To(() => shadeAmount, SetShade, target, fadeSeconds);
        }

        private void SetShade(float amount)
        {
            shadeAmount = amount;
            shade.enabled = amount > 0f;
            material.SetFloat(DarknessId, amount);
        }

        private void HideAll()
        {
            guide.enabled = false;
            isGuideLit = false;
            SetShade(0f);
        }

        private void KillTweens()
        {
            preview?.Kill();
            preview = null;
            fade?.Kill();
            fade = null;
            guide.transform.DOKill();
        }

#if UNITY_EDITOR
        public void EditorLink(BoardView linkedBoard, SpriteRenderer linkedShade, SpriteRenderer[] linkedPlanes, SpriteRenderer linkedGuide)
        {
            board = linkedBoard;
            shade = linkedShade;
            planes = linkedPlanes;
            guide = linkedGuide;
        }
#endif
    }
}
