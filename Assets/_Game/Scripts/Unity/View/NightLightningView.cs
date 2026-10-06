using System.Collections;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Lightning on a stormy night flight. Every few seconds the sky flashes and, for a
    /// moment, the whole board shows through the dark: a glimpse to check the route against.
    /// Most strikes come with a jagged bolt down the sky; the rest are a far-off flash alone.
    /// </summary>
    public sealed class NightLightningView : MonoBehaviour
    {
        // A strike's flash over time: brightness at each moment, and the moment in seconds.
        private static readonly float[] FlashLevels = { 0f, 0.85f, 0.15f, 0.9f, 0.35f, 0.12f, 0f };
        private static readonly float[] FlashTimes = { 0f, 0.04f, 0.1f, 0.15f, 0.26f, 0.4f, 0.75f };

        // The bolt shows from the first peak until the flash starts dying.
        private const float BoltOff = 0.26f;

        [SerializeField] private LineRenderer bolt;

        [Range(0f, 1f)]
        [Tooltip("How much a strike without a bolt flashes, against a full strike.")]
        [SerializeField] private float farFlash = 0.4f;
        [SerializeField, Range(0f, 1f)] private float boltChance = 0.7f;

        [Header("Bolt, in board squares")]
        [SerializeField, Min(1f)] private float boltTop = 10f;
        [SerializeField, Min(0f)] private float boltJitter = 0.45f;
        [SerializeField, Min(0.01f)] private float boltWidth = 0.1f;

        private INightShade shade;
        private BoardView board;
        private Vector3[] points;
        private Coroutine storm;

        /// <summary>A strike has just begun; true when it brings a bolt, false for a far-off flash.</summary>
        public event System.Action<bool> OnStrike;

        public void Initialize(INightShade nightShade, BoardView boardView)
        {
            shade = nightShade;
            board = boardView;
            points = new Vector3[bolt.positionCount];
            bolt.enabled = false;
        }

        private void OnDisable()
        {
            Stop();
        }

        public void Begin(float averageSeconds)
        {
            Stop();
            storm = StartCoroutine(Storm(averageSeconds));
        }

        public void Stop()
        {
            if (storm != null)
            {
                StopCoroutine(storm);
                storm = null;
            }

            if (shade != null)
            {
                shade.SetFlash(0f);
            }

            bolt.enabled = false;
        }

        private IEnumerator Storm(float averageSeconds)
        {
            float wait = Mathf.Max(2.5f, averageSeconds * 0.5f);

            while (true)
            {
                for (float t = 0f; t < wait; t += Time.deltaTime)
                {
                    yield return null;
                }

                bool hasBolt = Random.value < boltChance;
                float strength = hasBolt ? 1f : farFlash;

                if (hasBolt)
                {
                    DrawBolt();
                }

                OnStrike?.Invoke(hasBolt);

                float end = FlashTimes[FlashTimes.Length - 1];

                for (float t = 0f; t < end; t += Time.deltaTime)
                {
                    shade.SetFlash(FlashAt(t) * strength);
                    bolt.enabled = hasBolt && t >= FlashTimes[1] && t < BoltOff;
                    yield return null;
                }

                shade.SetFlash(0f);
                bolt.enabled = false;
                wait = averageSeconds * Random.Range(0.6f, 1.4f);
            }
        }

        private static float FlashAt(float time)
        {
            for (int i = 1; i < FlashTimes.Length; i++)
            {
                if (time < FlashTimes[i])
                {
                    float t = Mathf.InverseLerp(FlashTimes[i - 1], FlashTimes[i], time);
                    return Mathf.Lerp(FlashLevels[i - 1], FlashLevels[i], t);
                }
            }

            return 0f;
        }

        /// <summary>A jagged line from high in the sky down towards the board, somewhere new each time.</summary>
        private void DrawBolt()
        {
            float pitch = board.CellPitch;
            Vector3 centre = board.transform.position;
            Vector3 top = centre + new Vector3(Random.Range(-3f, 3f), boltTop, 0f) * pitch;
            Vector3 bottom = centre + new Vector3(Random.Range(-2.5f, 2.5f), Random.Range(0f, 3f), 0f) * pitch;
            int last = points.Length - 1;

            for (int i = 0; i <= last; i++)
            {
                Vector3 point = Vector3.Lerp(top, bottom, (float)i / last);

                if (i > 0 && i < last)
                {
                    point.x += Random.Range(-boltJitter, boltJitter) * pitch;
                }

                points[i] = point;
            }

            bolt.SetPositions(points);
            bolt.widthMultiplier = boltWidth * pitch;
        }

#if UNITY_EDITOR
        public void EditorLink(LineRenderer linkedBolt)
        {
            bolt = linkedBolt;
        }
#endif
    }
}
