using System.Collections;
using UnityEngine;

namespace Coffee.UIExtensions
{
    /// <summary>
    /// Compatibility component for the legacy ShinyEffectForUGUI API used by
    /// the SDK prefabs. The original third-party package is not present in
    /// this project, so this keeps those prefabs and scripts loadable.
    /// </summary>
    public class ShinyEffectForUGUI : MonoBehaviour
    {
        public float m_Location = 0.5f;
        public float m_Width = 0.25f;
        public float m_Softness = 1f;
        public float m_Brightness = 1f;
        public float m_Rotation = 120f;
        public float m_Highlight = 1f;
        public Material m_EffectMaterial;

        Coroutine playRoutine;

        public void Play(float duration)
        {
            if (playRoutine != null)
                StopCoroutine(playRoutine);

            playRoutine = StartCoroutine(PlayRoutine(duration));
        }

        IEnumerator PlayRoutine(float duration)
        {
            float start = m_Location;
            float elapsed = 0f;
            duration = Mathf.Max(0.01f, duration);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                m_Location = Mathf.Lerp(start, 1f, elapsed / duration);
                yield return null;
            }

            m_Location = 0f;
            playRoutine = null;
        }
    }
}
