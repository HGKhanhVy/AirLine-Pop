using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// The dark of a night flight. Night falls first; then a little light hops along the
    /// whole route, square by square, and only a soft glow round it shows the board as it
    /// goes, the planes showing only a small glow of their own. Once the light has gone the
    /// planes switch their torches on, with the flicker of a torch catching, and from then
    /// the only light left is each plane's own: a pool round it and a
    /// torch beam opening out from its nose to the edge of the screen, both with
    /// soft edges and turning wherever the plane turns. The rest has to be flown from memory.
    ///
    /// The dark is one sheet drawn by the night shade shader, which works the soft light out
    /// per pixel from the lights handed to it here, along with whatever weather the
    /// <see cref="NightWeatherView"/> brings: fog, rain and lightning.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class NightFlightView : MonoBehaviour, INightShade
    {
        // The shader's own count of lights: the player's plane, the wingman, the guiding light.
        private const int LightCount = 3;
        private const int GuideLight = 2;

        private static readonly int DarknessId = Shader.PropertyToID("_Darkness");
        private static readonly int LightId = Shader.PropertyToID("_NightLight");
        private static readonly int ShapeId = Shader.PropertyToID("_NightShape");
        private static readonly int BeamId = Shader.PropertyToID("_NightBeam");
        private static readonly int FogId = Shader.PropertyToID("_Fog");
        private static readonly int RainId = Shader.PropertyToID("_Rain");
        private static readonly int FlashId = Shader.PropertyToID("_Flash");
        private static readonly int WeatherScaleId = Shader.PropertyToID("_WeatherScale");

        // The torch catching: beam brightness at each step of the switch-on, and how long each step takes.
        private static readonly float[] SwitchOnSteps = { 0.9f, 0.1f, 1f, 0.35f, 1f };
        private static readonly float[] SwitchOnSeconds = { 0.05f, 0.07f, 0.05f, 0.09f, 0.06f };

        [SerializeField] private BoardView board;
        [SerializeField] private SpriteRenderer shade;

        [Tooltip("Each plane's body, which its headlight follows: the player's, then the wingman's.")]
        [SerializeField] private SpriteRenderer[] planes = new SpriteRenderer[0];

        [Tooltip("The light that flies the route before the player starts.")]
        [SerializeField] private SpriteRenderer guide;

        [Tooltip("Optional. The fog, rain, clouds and lightning a night flight flies through.")]
        [SerializeField] private NightWeatherView weather;

        [SerializeField, Range(0f, 1f)] private float darkness = 1f;

        [Header("Light, in board squares")]
        [SerializeField, Min(0.1f)] private float planePool = 0.5f;
        [SerializeField, Min(0f)] private float beamReach = 60f;
        [Tooltip("Half the torch beam's opening angle, in degrees.")]
        [SerializeField, Range(0f, 45f)] private float beamSpread = 14f;
        [SerializeField, Min(0.1f)] private float guidePool = 0.7f;
        [SerializeField, Min(0.1f)] private float guideSize = 0.45f;

        [Header("Timing")]
        [SerializeField, Min(0.05f)] private float stepSeconds = 0.34f;
        [SerializeField, Min(0f)] private float hopHeight = 0.25f;
        [SerializeField, Min(0f)] private float holdBeforeStart = 0.3f;
        [SerializeField, Min(0.05f)] private float fadeSeconds = 0.6f;

        [Tooltip("How long the torch beam takes to reach out to its full length once switched on.")]
        [SerializeField, Min(0.05f)] private float beamReachSeconds = 0.45f;

        private readonly Vector4[] lights = new Vector4[LightCount];
        private readonly Vector4[] shapes = new Vector4[LightCount];
        private readonly Vector4[] beams = new Vector4[LightCount];
        private Material material;
        private Sequence preview;
        private Tween fade;
        private float shadeAmount;
        private float rainAmount;
        private bool isGuideLit;
        private float beamPower;
        private float beamExtent;

        public bool IsPreviewing => preview != null && preview.IsActive();

        /// <summary>The planes' torches have just been switched on.</summary>
        public event Action OnTorchesOn;

        private void Awake()
        {
            material = shade.material;

            if (weather != null)
            {
                weather.Initialize(this, board);
            }

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
                shapes[i] = new Vector4(planePool * pitch, beamReach * pitch * beamExtent, Mathf.Tan(beamSpread * Mathf.Deg2Rad), 1f);
                beams[i] = new Vector4(beamPower, 0f, 0f, 0f);
            }

            Vector3 guidePosition = guide.transform.position;
            lights[GuideLight] = new Vector4(guidePosition.x, guidePosition.y, 0f, 1f);
            shapes[GuideLight] = isGuideLit ? new Vector4(guidePool * pitch, 0f, 0f, 1f) : Vector4.zero;

            material.SetFloat(DarknessId, shadeAmount);
            material.SetFloat(WeatherScaleId, pitch);
            material.SetVectorArray(LightId, lights);
            material.SetVectorArray(ShapeId, shapes);
            material.SetVectorArray(BeamId, beams);
        }

        /// <summary>Clears any night left from the previous level.</summary>
        public void Prepare()
        {
            KillTweens();
            HideAll();

            if (weather != null)
            {
                weather.Clear(0f);
            }
        }

        /// <summary>
        /// Lets night fall, with the weather of level <paramref name="levelNumber"/>, then flies
        /// the light along the route through the dark. <paramref name="onDone"/> runs once the
        /// light has gone and the torches are on, which is when the player may start.
        /// </summary>
        public void PlayPreview(IReadOnlyList<int> route, int levelNumber, Action onDone)
        {
            KillTweens();

            if (weather != null)
            {
                weather.Begin(levelNumber);
            }

            SetBeam(0f);
            beamExtent = 0f;

            if (route.Count == 0)
            {
                FadeShade(darkness);
                SetBeam(1f);
                beamExtent = 1f;
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
            preview.AppendCallback(() =>
            {
                guide.enabled = false;
                isGuideLit = false;
            });
            AppendSwitchOn(preview);
            preview.OnComplete(() =>
            {
                preview = null;
                onDone?.Invoke();
            });
        }

        /// <summary>
        /// The torches switching on: the beams flicker as they catch, then hold steady while
        /// they reach out from the planes' noses to their full length.
        /// </summary>
        private void AppendSwitchOn(Sequence sequence)
        {
            float at = sequence.Duration(false);
            sequence.InsertCallback(at, RaiseTorchesOn);
            sequence.Insert(at, DOTween.To(() => beamExtent, v => beamExtent = v, 1f, beamReachSeconds).SetEase(Ease.OutCubic));

            for (int i = 0; i < SwitchOnSteps.Length; i++)
            {
                sequence.Insert(at, DOTween.To(() => beamPower, SetBeam, SwitchOnSteps[i], SwitchOnSeconds[i]));
                at += SwitchOnSeconds[i];
            }
        }

        private void RaiseTorchesOn()
        {
            OnTorchesOn?.Invoke();
        }

        private void SetBeam(float power)
        {
            beamPower = power;
        }

        /// <summary>Morning: the dark lifts, for the win, though any rain keeps falling.</summary>
        public void Dawn()
        {
            KillTweens();
            guide.enabled = false;
            isGuideLit = false;
            FadeShade(0f);

            if (weather != null)
            {
                weather.Brighten(fadeSeconds);
            }
        }

        public void SetFog(float amount)
        {
            material.SetFloat(FogId, amount);
        }

        public void SetRain(float amount)
        {
            rainAmount = amount;
            material.SetFloat(RainId, amount);
            ShowShadeIfNeeded();
        }

        public void SetFlash(float amount)
        {
            material.SetFloat(FlashId, amount);
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
            material.SetFloat(DarknessId, amount);
            ShowShadeIfNeeded();
        }

        /// <summary>The sheet draws the rain too, so it stays up while it rains, even once the dark has gone.</summary>
        private void ShowShadeIfNeeded()
        {
            shade.enabled = shadeAmount > 0f || rainAmount > 0f;
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

        public void EditorLinkWeather(NightWeatherView linkedWeather)
        {
            weather = linkedWeather;
        }
#endif
    }
}
